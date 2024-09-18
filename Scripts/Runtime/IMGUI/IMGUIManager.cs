using System;
using System.Collections;
using System.Collections.Generic;
using PluginHub.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = System.Object;


// 【IMGUI基础组件（必须在场景中存在）】
//
// 提供一个IMGUI集中管理器，用于管理各个脚本的IMGUI绘制
// 其他脚本可以注册到IMGUIManager中，实现自己的GUI绘制，IMGUIManager会在OnGUI中调用他们的绘制方法
// 注册到IMGUIManager的脚本称为其客户端
// 注册方式是继承IMGUIManager.IIMGUI接口
//
// IIMGUI 使用[抽象类]存在的问题：
// 1、无法同时继承多个抽象类，以实现多个界面绘制功能
// 2、占用了继承位，客户端无法继承其他类
//
// IIMGUI 使用[接口]存在的问题：
// 1、接口无法包含字段，无法直接在实现的客户端检视面板中添加localGUIScale滑动条以便用户调整
// 2、带不上Mono的引用

namespace PluginHub.Runtime
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(IMGUIManager))]
    public class IMGUIManagerEditor : Editor
    {
        private bool foldout = false;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            IMGUIManager imGUIManager = target as IMGUIManager;

            foldout = EditorGUILayout.Foldout(foldout, $"客户端数量：{imGUIManager.clientList.Count}");
            if (foldout)
            {
                // 显示出当前的客户端列表
                for (int i = 0; i < imGUIManager.clientList.Count; i++)
                {
                    IMGUIManager.IIMGUI imGUI = imGUIManager.clientList[i];
                    GUILayout.Label($"[{i}] {imGUI.GetType().ToString()} ({imGUI.IMGUIOrder})");
                }
            }

            if (GUILayout.Button("刷新客户端列表"))
                imGUIManager.RefreshClientList();

        }
    }

    #endif

    [ExecuteAlways]
    public class IMGUIManager : SceneSingleton<IMGUIManager>
    {
        // 继承此类，以成为客户端
        public interface IIMGUI
        {
            public bool IMGUIEnable => true;// 是否绘制GUI
            public void IMGUIDraw(); // 绘制GUI
            public int IMGUIOrder => 0;// 绘制顺序
            public float IMGUILocalGUIScale => 1;// 本地UI缩放因子
        }

        // 全局GUI开关
        public bool showGUI = true;
        // 用户设置的UI全局缩放因子
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
            Debug.Log("[IMGUIManager] RefreshClientList");

            clientList.Clear();
            MonoBehaviour[] monoInScene = FindObjectsOfType<MonoBehaviour>();
            // Debug.Log(s.Length);
            foreach (MonoBehaviour client in monoInScene)
            {
                if (client is IIMGUI imGUI)
                {
                    clientList.Add(imGUI);
                }
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

            Matrix4x4 originalMatrix = GUI.matrix;

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(globalGUIScale,globalGUIScale, 1));
            {
                // 普通GUI
                for (int i = 0; i < clientList.Count; i++)
                {
                    IIMGUI imGUI = clientList[i];

                    if (imGUI.IMGUIEnable == false)
                        continue;

                    float scale = imGUI.IMGUILocalGUIScale * globalGUIScale;
                    if(scale<0.1f)
                        Debug.LogWarning($"[IMGUIManager] {imGUI.GetType().ToString()} 的 IMGUILocalGUIScale 设置过小，可能导致GUI无法显示");
                    scale = Mathf.Clamp(scale, 0.1f, 2.0f);

                    GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
                    imGUI.IMGUIDraw();
                }
            }
            GUI.matrix = originalMatrix;
        }
    }
}
