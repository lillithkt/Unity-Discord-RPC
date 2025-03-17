using Discord.Sdk;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using System;

namespace gay.lilyy.discordrpc
{

    [InitializeOnLoad]
    public class DiscordRPC
    {
        [SerializeField]
        private static ulong clientId = 1217541552847716393;

        private static Client client;
        private static string codeVerifier;
        private static string lastScene = "";

        private static bool hasSentRPC = false;

        private static Func<Activity, bool, Task<bool>> VRCSDK_PROVIDER;

        public static void SetProvider(Func<Activity, bool, Task<bool>> provider)
        {
            VRCSDK_PROVIDER = provider;
        }

        static DiscordRPC()
        {
            Initialize();
            EditorSceneManager.activeSceneChangedInEditMode += (_, next) => OnSceneChanged(next);
            EditorSceneManager.sceneOpened += (next, _) => OnSceneChanged(next);
            EditorApplication.update += CheckForNewAvatar;
        }
        private static double lastCheckTime = 0;

        private static void CheckForNewAvatar()
        {
            if (EditorApplication.timeSinceStartup - lastCheckTime < 1.0)
                return;

            lastCheckTime = EditorApplication.timeSinceStartup;
            UpdateRichPresence();
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            Debug.Log("DiscordRPC initialized");
            client = new Client();
            client.SetStatusChangedCallback(OnStatusChanged);
            if (System.IO.File.Exists("discord_token.txt"))
            {
                Debug.Log("Token file found");
                string token = System.IO.File.ReadAllText("discord_token.txt");
                OnReceivedToken(token);
            }
            else
            {
                Debug.Log("Token file not found, starting OAuth flow");
                StartOAuthFlow();
            }
        }

        private static void OnSceneChanged(Scene next)
        {
            if (next.name != lastScene)
            {
                Debug.Log("Scene changed: " + next.name);
                lastScene = next.name;
                UpdateRichPresence();
            }
        }


        private static async void UpdateRichPresence()
        {
            if (client == null || client.GetStatus() != Client.Status.Ready)
                return;

            // Debug.Log("Updating rich presence");

            Activity activity = new Activity();
            activity.SetName("Unity Editor");
            activity.SetType(ActivityTypes.Playing);
            activity.SetDetails($"Editing Scene: {SceneManager.GetActiveScene().name}");
            ActivityAssets assets = new ActivityAssets();
            assets.SetLargeImage("unity");
            assets.SetLargeText("Unity");
            activity.SetAssets(assets);

            if (VRCSDK_PROVIDER != null)
            {
                if (!await VRCSDK_PROVIDER(activity, hasSentRPC)) return;
            }
            else
            {
                Debug.Log("No provider set");
            }


            client.UpdateRichPresence(activity, (ClientResult result) =>
            {
                if (result.Successful())
                {
                    Debug.Log("Rich presence updated!");
                    hasSentRPC = true;
                }
                else
                {
                    Debug.LogError("Failed to update rich presence");
                }
            });
        }


        private static void OnStatusChanged(Client.Status status, Client.Error error, int errorCode)
        {
            Debug.Log($"Status changed: {status}");
            if (error != Client.Error.None)
            {
                Debug.LogError($"Error: {error}, code: {errorCode}");
            }
            if (status == Client.Status.Ready)
            {
                UpdateRichPresence();
            }
        }

        private static void StartOAuthFlow()
        {
            var authorizationVerifier = client.CreateAuthorizationCodeVerifier();
            codeVerifier = authorizationVerifier.Verifier();

            var args = new AuthorizationArgs();
            args.SetClientId(clientId);
            args.SetScopes(Client.GetDefaultPresenceScopes());
            args.SetCodeChallenge(authorizationVerifier.Challenge());
            client.Authorize(args, OnAuthorizeResult);
        }

        private static void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
        {
            Debug.Log($"Authorization result: [{result.Error()}] [{code}] [{redirectUri}]");
            if (!result.Successful())
            {
                return;
            }
            GetTokenFromCode(code, redirectUri);
        }

        private static void GetTokenFromCode(string code, string redirectUri)
        {
            client.GetToken(clientId,
                            code,
                            codeVerifier,
                            redirectUri,
                            (result, token, refreshToken, tokenType, expiresIn, scope) =>
                            {
                                if (!string.IsNullOrEmpty(token))
                                {
                                    OnReceivedToken(token);
                                }
                                else
                                {
                                    OnRetrieveTokenFailed();
                                }
                            });
        }

        private static void OnReceivedToken(string token)
        {
            Debug.Log("Token received: " + token);
            client.UpdateToken(AuthorizationTokenType.Bearer, token, (ClientResult result) => { client.Connect(); });
            System.IO.File.WriteAllText("discord_token.txt", token);
        }

        private static void OnRetrieveTokenFailed()
        {
            Debug.LogError("Failed to retrieve token");
        }
    }
}
