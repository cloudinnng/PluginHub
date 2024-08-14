using System;
using System.Collections;
using System.Collections.Generic;
using PluginHub.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = System.Object;


namespace PluginHub.Runtime
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(IMGUIManager))]
    public class IMGUIManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            IMGUIManager imGUIManager = target as IMGUIManager;
            if (GUILayout.Button("刷新客户端列表"))
                imGUIManager.RefreshClientList();
        }
    }
    #endif

    // 【IMGUI基础组件】
    //
    // 提供左侧边栏绘制和普通GUI绘制（全屏区域）
    // 用户脚本可以通过继承IIMGUI，将自己的GUI绘制到屏幕上 (脚本需继承自MonoBehaviour)
    // 思路来自Debugger.CustomWindow.ICustomWindowGUI
    // 与其不同的是，他不依附于Debugger.CustomWindow，而是直接显示在屏幕上
    [ExecuteAlways]
    public class IMGUIManager : SceneSingleton<IMGUIManager>
    {
        // 继承此类，以成为客户端
        public abstract class IIMGUI : MonoBehaviour
        {
            public float localGUIScale = 1.0f;// 本地GUI缩放因子，让不同的GUI客户端在全局GUI缩放下可以有不同的本地缩放比例
            public abstract void IMGUIDraw(); // 绘制GUI
            public virtual int IMGUIOrder => 0;
        }

        public bool showGUI = true;
        [Tooltip("用户设置的UI全局缩放因子")]
        public float globalGUIScale = 1.0f;


        // 真实屏幕尺寸
        private static Vector2 _realScreenSize;
        // [重要方法] GUI程序开发依赖的屏幕尺寸
        public Vector2 ScreenSize(float localGUIScale) => new Vector2(_realScreenSize.x / globalGUIScale / localGUIScale, _realScreenSize.y / globalGUIScale / localGUIScale);

        // 客户端列表
        public List<IIMGUI> clientList = new List<IIMGUI>();

        #region 刷新客户端操作

        private void OnValidate()
        {
            RefreshClientList();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            RefreshClientList();
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1);
            RefreshClientList();
        }

        public void RefreshClientList()
        {
            Debug.Log("RefreshClientList");
            clientList.Clear();
            MonoBehaviour[] monoInScene = FindObjectsOfType<MonoBehaviour>();
            // Debug.Log(s.Length);
            foreach (var client in monoInScene)
            {
                if (client is IIMGUI imGUI)
                    clientList.Add(imGUI);
            }
            // Debug.Log(clientList.Count);
            clientList.Sort((a, b) => a.IMGUIOrder.CompareTo(b.IMGUIOrder));
        }

        #endregion

        private void Update()
        {
            // 更新真实屏幕尺寸
            if(_realScreenSize.x != Screen.width || _realScreenSize.y != Screen.height)
                _realScreenSize = new Vector2(Screen.width, Screen.height);
        }

        private void OnGUI()
        {
            if (!showGUI)
                return;

            // 检查无效客户端,并自动刷新
            for (int i = 0; i < clientList.Count; i++)
            {
                if (clientList[i] == null)
                {
                    RefreshClientList();
                    break;
                }
            }

            Matrix4x4 originalMatrix = GUI.matrix;

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(globalGUIScale,globalGUIScale, 1));
            {
                // 普通GUI
                for (int i = 0; i < clientList.Count; i++)
                {
                    IIMGUI imGUI = clientList[i];
                    // 根据是否激活和是否启用来绘制
                    if (imGUI.gameObject.activeInHierarchy == false || imGUI.enabled == false)
                        continue;

                    float localScale = imGUI.localGUIScale * globalGUIScale;
                    GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(localScale, localScale, 1));
                    imGUI.IMGUIDraw();
                }
            }
            GUI.matrix = originalMatrix;
        }


    }
}
