using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Sdk;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.Api;

namespace gay.lilyy.discordrpc.providers
{

    [InitializeOnLoad]
    public class LillithDiscordRPCAvatarProvider

    {
        private static string lastAvatarId = "";
        static LillithDiscordRPCAvatarProvider()
        {
            Debug.Log("LillithDiscordRPCAvatarProvider initialized");
            DiscordRPC.SetProvider(SetPresence);
        }

        private static VRCAvatarDescriptor GetPriorityAvatar()
        {
            var selectedObjects = Selection.gameObjects;
            foreach (var obj in selectedObjects)
            {
                var avatar = obj.GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null)
                    return avatar;
            }
            return UnityEngine.Object.FindObjectOfType<VRCAvatarDescriptor>();
        }
        static async Task<bool> SetPresence(Activity activity, bool hasSentRPC)
        {
            try
            {
                VRCAvatarDescriptor avatarDescriptor = GetPriorityAvatar();

                if (avatarDescriptor != null)
                {
                    string bundleId = avatarDescriptor.gameObject.GetComponent<PipelineManager>().blueprintId;
                    if (bundleId != lastAvatarId || !hasSentRPC)
                    {
                        lastAvatarId = bundleId;
                        var avatar = await VRCApi.GetAvatar(bundleId);
                        Debug.Log("Avatar Name: " + avatar.Name);
                        if (Application.isPlaying)
                        {
                            activity.SetDetails("Testing " + avatar.Name);
                        }
                        else
                        {
                            activity.SetDetails("Working on " + avatar.Name);
                        }
                        var assets = new ActivityAssets();
                        assets.SetLargeImage(avatar.ImageUrl);
                        assets.SetSmallImage("unity");
                        assets.SetSmallText("Unity");
                        activity.SetAssets(assets);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                return true;
            }
            return false;
        }
    }
}