using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    // 这个类适用于当你有一些常驻场景的游戏对象(DontDestroyOnLoad(gameObject))的时候，你希望在所有场景中进行测试时，都会将其带入到当前运行环境。
    // 将常驻对象放入到0号场景，然后在编辑器中运行其他场景时，会自动切换到0号场景，然后再进入原场景（点击play按钮前的场景）。
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
            {
                Debug.LogWarning("您现在运行的就是0号场景，如果不是有意行为,请确定将所需场景添加进构建设置中");
                return;
            }

            // 编辑器启动时的场景索引
            int editorStartUpSceneIndex = currSceneIndex;

            Debug.LogWarning("强制从0号场景开始运行");
            SceneManager.LoadScene(0);
            
            //切换到编辑器启动之前所在的场景
            Debug.Log($"普通载入编辑器启动之前所在的场景{editorStartUpSceneIndex}");
            SceneManager.LoadScene(editorStartUpSceneIndex);
        }
    }
}
