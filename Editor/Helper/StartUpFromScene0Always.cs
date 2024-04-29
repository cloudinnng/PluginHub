using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor.Helper
{
    public class StartUpFromScene0Always
    {
        private const string MENU_NAME = "PluginHub/总是从0号场景启动(编辑器中有效)";

        private static bool enable
        {
            get { return EditorPrefs.GetBool($"PH_{Application.productName}_StartUpFrom0Enable", false); }
            set { EditorPrefs.SetBool($"PH_{Application.productName}_StartUpFrom0Enable", value); }
        }

        [MenuItem(MENU_NAME, false, 100)]
        private static void ToggleAction()
        {
            enable = !enable;
        }

        [MenuItem(MENU_NAME, true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked(MENU_NAME, enable);
            return true;
        }

        //这个方法会在程序运行后最先调用一次
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            // Debug.Log("Initialize");
            if (!enable) return;

            int currSceneIndex = SceneManager.GetActiveScene().buildIndex;
            if (currSceneIndex == 0)
                return;
            
            int editorStartUpSceneIndex = currSceneIndex;

            Debug.LogWarning("强制从0号场景开始运行");
            SceneManager.LoadScene(0);
            
            //切换到编辑器启动之前所在的场景
            Debug.Log($"跳转到编辑器启动之前所在的场景{editorStartUpSceneIndex}");
            SceneManager.LoadScene(editorStartUpSceneIndex);
        }
    }
}
