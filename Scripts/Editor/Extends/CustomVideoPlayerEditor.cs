using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace PluginHub.Editor
{
    // [CustomEditor(typeof(VideoPlayer))]
    // public class CustomVideoPlayerEditor : UnityEditor.Editor
    // {
    //     private UnityEditor.Editor instance;
    //     private VideoPlayer targetVideoPlayer;

    //     private void OnEnable()
    //     {
    //         // Try to get the internal 'VideoPlayerEditor' type
    //         var editorAssembly = typeof(UnityEditor.Editor).Assembly;
    //         Debug.Log(editorAssembly.FullName);
    //         var editorType = editorAssembly.GetType("UnityEditor.VideoPlayerEditor");
    //         if (editorType != null)
    //         {
    //             instance = CreateEditor(targets, editorType);
    //         }
    //         else
    //         {
    //             Debug.LogError("Could not find VideoPlayerEditor type.");
    //         }
    //         targetVideoPlayer = target as VideoPlayer;
    //         if (targetVideoPlayer == null)
    //         {
    //             Debug.LogError("Target is not a VideoPlayer.");
    //         }
    //     }

    //     private void OnDisable()
    //     {
    //         if (instance != null)
    //             DestroyImmediate(instance);
    //     }

    //     public override void OnInspectorGUI()
    //     {
    //         // 绘制原有Inspector
    //         if (instance != null)
    //             instance.OnInspectorGUI();

    //         // 绘制新的Inspector
    //         GUILayout.Space(10);
    //         if (GUILayout.Button("Play"))
    //         {
    //             targetVideoPlayer.Play();
    //         }
    //         if (GUILayout.Button("Pause"))
    //         {
    //             targetVideoPlayer.Pause();
    //         }
    //         if (GUILayout.Button("Stop"))
    //         {
    //             targetVideoPlayer.Stop();
    //         }
    //     }
    // }
}