using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Sdk;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Components;
using VRC.SDKBase.Editor.Api;

namespace gay.lilyy.discordrpc.providers
{

    [InitializeOnLoad]
    public class LillithDiscordRPCWorldsProvider

    {
        private static string lastAvatarId = "";
        static LillithDiscordRPCWorldsProvider()
        {
            Debug.Log("LillithDiscordRPCWorldsProvider initialized");
            DiscordRPC.SetProvider(SetPresence);
        }

        private static VRCSceneDescriptor GetPriorityAvatar()
        {
            var selectedObjects = Selection.gameObjects;
            foreach (var obj in selectedObjects)
            {
                var avatar = obj.GetComponentInParent<VRCSceneDescriptor>();
                if (avatar != null)
                    return avatar;
            }
            return UnityEngine.Object.FindObjectOfType<VRCSceneDescriptor>();
        }
        static async Task<bool> SetPresence(Activity activity, bool hasSentRPC)
        {
            try
            {
                VRCSceneDescriptor avatarDescriptor = GetPriorityAvatar();

                if (avatarDescriptor != null)
                {
                    string bundleId = avatarDescriptor.gameObject.GetComponent<PipelineManager>().blueprintId;

                    if (bundleId != lastAvatarId || !hasSentRPC)
                    {
                        lastAvatarId = bundleId;
                        var world = await VRCApi.GetWorld(bundleId);
                        if (Application.isPlaying)
                        {
                            activity.SetDetails("Testing " + world.Name);
                        }
                        else
                        {
                            activity.SetDetails("Working on " + world.Name);
                        }
                        var assets = new ActivityAssets();
                        assets.SetLargeImage(world.ImageUrl);
                        assets.SetSmallImage("unity");
                        assets.SetSmallText("Unity");
                        activity.SetAssets(assets);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return true;
            }
            return false;
        }
    }
}