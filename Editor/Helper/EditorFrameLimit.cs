using UnityEngine;
using System.Collections;
using System;
using Cloudinnng.CFramework.Editor;
using UnityEditor;

namespace PluginHub.Editor.Helper
{

//仅用于限制编辑器模式下的帧率，不干涉运行时的帧率
//避免还没运行呢显卡就风扇狂转，也有省电的作用
//再次进去编辑模式可能需要重新打开

// 目前不清楚是否需要下面两行代码
// Application.targetFrameRate = -1;
// QualitySettings.vSyncCount = 0;
    public class EditorFrameLimit
    {

        private const string MENU_NAME = "PluginHub/限制编辑模式帧率";
        private static int desiredFPS = 25;

        private static bool enable
        {
            get { return EditorPrefs.GetBool($"PH_{Application.productName}_EditorFrameLimit", false); }
            set { EditorPrefs.SetBool($"PH_{Application.productName}_EditorFrameLimit", value); }
        }

        [MenuItem(MENU_NAME, false, 101)]
        private static void ToggleAction()
        {
            enable = !enable;

            if (enable)
            {
                EditorApplication.update -= EditorUpdate;
                EditorApplication.update += EditorUpdate;
            }
            else
            {
                EditorApplication.update -= EditorUpdate;
            }
        }

        [MenuItem(MENU_NAME, true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked(MENU_NAME, enable);
            return true;
        }


        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (enable)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.update -= EditorUpdate;
                    EditorApplication.update += EditorUpdate;
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                if (enable)
                {
                    EditorApplication.update -= EditorUpdate;
                    EditorApplication.update += EditorUpdate;
                }
            }
        }

        private static void EditorUpdate()
        {
            if (!enable) //未开启
                return;

            long lastTicks = DateTime.Now.Ticks;
            long currentTicks = lastTicks;
            float delay = 1f / desiredFPS;
            float elapsedTime;

            if (desiredFPS <= 0)
                return;

            while (true)
            {
                currentTicks = DateTime.Now.Ticks;
                elapsedTime = (float)TimeSpan.FromTicks(currentTicks - lastTicks).TotalSeconds;
                if (elapsedTime >= delay)
                {
                    break;
                }
            }
        }
    }
}