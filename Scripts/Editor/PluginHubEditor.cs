using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
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
                    //将PluginHub窗口和其他窗口放在一起，可以在结束运行时切换到PluginHubWindow窗口
                    //和game窗口放在一起的时候如果进入播放模式前game窗口是显示状态则退出播放模式时好像切换不过去
                    PluginHubWindow.Window.Show();
                    Debug.Log("退出播放模式，显示PluginHubWindow窗口");
                }
            }

        }
    }
}

