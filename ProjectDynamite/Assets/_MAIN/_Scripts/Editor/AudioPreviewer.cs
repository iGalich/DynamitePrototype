using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class AudioPreviewer
{
    private static int? _lastPlayedAudioClipId = null;

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line)
    {
        UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceId);

        if (obj is AudioClip audioClip)
        {
            if (IsPreviewClipPlaying())
            {
                StopAllPreviewClips();

                if (_lastPlayedAudioClipId.HasValue && _lastPlayedAudioClipId.Value != audioClip.GetInstanceID())
                {
                    PlayPreviewClip(audioClip);
                }
            }
            else
            {
                PlayPreviewClip(audioClip);
            }

            _lastPlayedAudioClipId = audioClip.GetInstanceID();

            return true;
        }

        return false;
    }

    public static void PlayPreviewClip(AudioClip audioClip)
    {
        Assembly assembly = typeof(AudioImporter).Assembly;
        Type audioUtil = assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo methodInfo = audioUtil.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new System.Type[] { typeof(AudioClip), typeof(Int32), typeof(Boolean) },
            null);

        methodInfo.Invoke(null, new object[] { audioClip, 0, false });
    }

    public static bool IsPreviewClipPlaying()
    {
        Assembly assembly = typeof(AudioImporter).Assembly;
        Type audioUtil = assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo methodInfo = audioUtil.GetMethod(
            "IsPreviewClipPlaying",
            BindingFlags.Static | BindingFlags.Public);

        return (bool)methodInfo.Invoke(null, null);
    }

    public static void StopAllPreviewClips()
    {
        Assembly assembly = typeof(AudioImporter).Assembly;
        Type audioUtil = assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo methodInfo = audioUtil.GetMethod(
            "StopAllPreviewClips",
            BindingFlags.Static | BindingFlags.Public);

        methodInfo.Invoke(null, null);
    }
}