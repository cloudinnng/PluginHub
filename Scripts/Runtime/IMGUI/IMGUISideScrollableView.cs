using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace PluginHub.Runtime
{

    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(IMGUISideScrollableView))]
    public class IMGUISideScrollableViewEditor : Editor
    {
        private bool foldout = false;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            IMGUISideScrollableView imGUIManager = target as IMGUISideScrollableView;

            foldout = EditorGUILayout.Foldout(foldout, $"客户端数量：{imGUIManager.clientList.Count}");
            if (foldout)
            {
                // 显示出当前的客户端列表
                for (int i = 0; i < imGUIManager.clientList.Count; i++)
                {
                    IMGUISideScrollableView.IIMGUI imGUI = imGUIManager.clientList[i];
                    GUILayout.Label($"[{i}] {imGUI.GetType().ToString()} ({imGUI.IMGUIOrder})");
                }
            }

            if (GUILayout.Button("刷新客户端列表"))
                imGUIManager.RefreshClientList();
        }
    }
    #endif

    [ExecuteAlways]
    // 提供侧边栏滚动视图
    public class IMGUISideScrollableView : SceneSingleton<IMGUISideScrollableView>, IMGUIManager.IIMGUI, Debugger.CustomWindow.ICustomWindowGUI
    {
        public interface IIMGUI
        {
            public bool enableGUI => true; // 是否绘制GUI
            // 绘制左侧边栏内容GUI
            public void IMGUILeftSideDraw();
            public string IMGUITitle => GetType().ToString();
            public int IMGUIOrder => 0;
        }

        public float localGUIScale = 1;
        public float IMGUILocalGUIScale => localGUIScale;

        public bool showSidebarGUI = true;
        public bool showClientTitle = true;

        // 侧边栏
        public bool fullScreenWidth = false; // 是否占满屏幕宽度
        public float leftSideScrollWidth = 400;
        private Vector2 leftScrollPos;

        // 客户端列表
        public List<IIMGUI> clientList = new List<IIMGUI>();
        public List<bool> isFoldoutList = new List<bool>();

        public bool showFPS = true;
        private FpsCounter fpsCounter = new FpsCounter(0.2f);


        private void Update()
        {
            if(showFPS)
                fpsCounter.Update();

            if (Input.GetKeyDown(KeyCode.F4))
                showSidebarGUI = !showSidebarGUI;
        }

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
            Debug.Log("[IMGUISideScrollableView] RefreshClientList");
            clientList.Clear();
            isFoldoutList.Clear();
            MonoBehaviour[] monoInScene = FindObjectsOfType<MonoBehaviour>();
            // Debug.Log(s.Length);
            foreach (var client in monoInScene)
            {
                if (client is IIMGUI imGUI)
                {
                    clientList.Add(imGUI);
                    isFoldoutList.Add(false);
                }
            }
            // Debug.Log(clientList.Count);
            clientList.Sort((a, b) => a.IMGUIOrder.CompareTo(b.IMGUIOrder));
        }
        #endregion

        public void OnDrawDebuggerGUI()
        {
            GUILayout.BeginHorizontal();
            {
                showSidebarGUI = GUILayout.Toggle(showSidebarGUI, "显示GUI");
                showFPS = GUILayout.Toggle(showFPS, "显示FPS");
                showClientTitle = GUILayout.Toggle(showClientTitle, "显示客户端标题");
                fullScreenWidth = GUILayout.Toggle(fullScreenWidth, "占满屏幕宽度");
            }
            GUILayout.EndHorizontal();

            // GUILayout.Label($"GUIScale: {localGUIScale:F2}");
            // localGUIScale = GUILayout.HorizontalSlider(localGUIScale, 0.1f, 2.0f);

            GUILayout.Label($"侧边栏宽度: {leftSideScrollWidth:F0}");

            if (!fullScreenWidth)
                leftSideScrollWidth = GUILayout.HorizontalSlider(leftSideScrollWidth, 100, 2000);

            if (GUILayout.Button($"刷新客户端列表 {clientList.Count}"))
                RefreshClientList();

            GUILayout.Label($"F4 切换GUI显示");
        }

        public void IMGUIDraw()
        {
            if (showSidebarGUI)
            {
                Vector2 screenSize = IMGUIManager.Instance.ScreenSize(IMGUILocalGUIScale);
                if (fullScreenWidth)
                    leftSideScrollWidth = screenSize.x;

                // 侧边栏
                GUILayout.BeginVertical("Box", GUILayout.Height(screenSize.y), GUILayout.Width(leftSideScrollWidth));
                {
                    // 滚动视图
                    // 8是 Box Style 的 padding left + padding right
                    int boxPadding = 8;
                    leftScrollPos = GUILayout.BeginScrollView(leftScrollPos, GUILayout.Width(leftSideScrollWidth - boxPadding));
                    {
                        // 绘制侧边栏内容GUI
                        for (int i = 0; i < clientList.Count; i++)
                        {
                            IIMGUI imGUI = clientList[i];
                            bool isFoldout = isFoldoutList[i];

                            // 根据是否激活和是否启用来绘制
                            // if (imGUI.enableGUI == false || mono.gameObject.activeInHierarchy == false || mono.enabled == false)
                                // continue;

                            // 画title
                            if (showClientTitle)
                            {
                                // 折叠中的客户端标题颜色变灰
                                GUI.color = isFoldout ? Color.gray : Color.white;
                                isFoldoutList[i] = GUILayout.Toggle(isFoldout,
                                    $"{imGUI.IMGUITitle} ({imGUI.IMGUIOrder})", "Box");
                                GUI.color = Color.white;
                            }

                            // 画内容
                            if (!isFoldout)
                                imGUI.IMGUILeftSideDraw();
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            // FPS
            if (showFPS)
            {
                GUI.color = Color.green;
                string text = $"FPS: {fpsCounter.CurrentFps:F1}";
                Vector2 size = GUI.skin.label.CalcSize(GUIContentEx.Temp(text));
                Rect rect = new Rect(0, 0, size.x, size.y);
                GUI.Label(rect, text);
                GUI.color = Color.white;
            }
        }

    }
}