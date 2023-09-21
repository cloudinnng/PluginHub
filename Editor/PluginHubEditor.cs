using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginHub
{
    //编辑器相关的代码
    [InitializeOnLoad]
    public class PluginHubEditor
    {
        static PluginHubEditor()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            //结束播放模式
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (PluginHubWindow.showPluginHubOnExitPlayMode)
                {
                    //将PluginHubWindow窗口和gameview放在一起，可以在结束运行时切换到PluginHubWindow窗口，从而隐藏gameview
                    // EditorWindow.GetWindow(typeof(PluginHubWindow)).Show();
                    PluginHubWindow.ShowWindow();
                    Debug.Log("退出播放模式，显示PluginHubWindow窗口");
                }
            }
        }
    }
}

