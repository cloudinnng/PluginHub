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
            public void IMGUIDraw(float globalGUIScale); // 绘制GUI
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

        public GUISkin guiskin;
        public bool showFPS = true;
        private FpsCounter fpsCounter = new FpsCounter(0.5f);

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
            // Debug.Log("[IMGUIManager] RefreshClientList");

            clientList.Clear();
            MonoBehaviour[] monoInScene = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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
            if(showFPS)
                fpsCounter.Update();

            // 更新真实屏幕尺寸
            if(_realScreenSize.x != Screen.width || _realScreenSize.y != Screen.height)
                _realScreenSize = new Vector2(Screen.width, Screen.height);
        }

        private void OnGUI()
        {
            if (!showGUI)
                return;
            if (guiskin != null)
                GUI.skin = guiskin;

            Matrix4x4 originalMatrix = GUI.matrix;

            // FPS
            if (showFPS)
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(globalGUIScale/2,globalGUIScale/2, 1));
                GUI.color = Color.green;
                string text = $"FPS: {fpsCounter.CurrentFps:F1}";
                Vector2 size = GUI.skin.label.CalcSize(GUIContentEx.Temp(text));
                Rect rect = new Rect(0, 0, size.x + 4,size.y);
                GUI.Box(rect,"");
                GUI.Label(rect, text);
                GUI.color = Color.white;
            }
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(globalGUIScale,globalGUIScale, 1));
            {
                // 普通GUI
                for (int i = 0; i < clientList.Count; i++)
                {
                    IIMGUI imGUI = clientList[i];

                    if (imGUI.IMGUIEnable == false)
                        continue;

                    float scale = imGUI.IMGUILocalGUIScale * globalGUIScale;
                    scale = Mathf.Clamp(scale, 0.1f, 10.0f);

                    GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
                    imGUI.IMGUIDraw(globalGUIScale);
                }
            }
            GUI.matrix = originalMatrix;
        }
    }
}
