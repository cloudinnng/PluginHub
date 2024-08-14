using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Runtime
{

    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(IMGUISideScrollableView))]
    public class IMGUISideScrollableViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            IMGUISideScrollableView imGUIManager = target as IMGUISideScrollableView;
            if (GUILayout.Button("刷新客户端列表"))
                imGUIManager.RefreshClientList();
        }
    }
    #endif

    [ExecuteAlways]
    // 提供侧边栏滚动视图
    public class IMGUISideScrollableView : IMGUIManager.IIMGUI, Debugger.CustomWindow.ICustomWindowGUI
    {
        public abstract class IIMGUI : MonoBehaviour
        {
            [HideInInspector]
            public bool hideGUIContent = false; // 是否隐藏GUI内容,只显示标题,使得可以折叠,让界面更简洁
            // 绘制左侧边栏内容GUI
            public abstract void IMGUILeftSideDraw();
            public virtual string IMGUITitle => GetType().ToString();
            public virtual int IMGUIOrder => 0;
        }

        public bool showSidebarGUI = true;
        public bool showClientTitle = true;

        // 侧边栏
        public bool fullScreenWidth = false; // 是否占满屏幕宽度
        public float leftSideScrollWidth = 400;
        private Vector2 leftScrollPos;

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

        public void OnDrawDebuggerGUI()
        {
            GUILayout.BeginHorizontal();
            {
                showSidebarGUI = GUILayout.Toggle(showSidebarGUI, "显示GUI");
                showClientTitle = GUILayout.Toggle(showClientTitle, "显示客户端标题");
                fullScreenWidth = GUILayout.Toggle(fullScreenWidth, "占满屏幕宽度");
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"GUIScale: {localGUIScale:F2}");
            localGUIScale = GUILayout.HorizontalSlider(localGUIScale, 0.1f, 2.0f);


            GUILayout.Label($"侧边栏宽度: {leftSideScrollWidth:F0}");

            if (!fullScreenWidth)
                leftSideScrollWidth = GUILayout.HorizontalSlider(leftSideScrollWidth, 100, 2000);

            if (GUILayout.Button("刷新客户端列表"))
                RefreshClientList();
        }

        public override void IMGUIDraw()
        {
            if (!showSidebarGUI)
                return;


            Vector2 screenSize = IMGUIManager.Instance.ScreenSize(localGUIScale);
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
                        // 根据是否激活和是否启用来绘制
                        if (imGUI.gameObject.activeInHierarchy == false || imGUI.enabled == false)
                            continue;

                        // 画title
                        if (showClientTitle)
                        {
                            // 折叠中的客户端标题颜色变灰
                            GUI.color = imGUI.hideGUIContent ? Color.gray : Color.white;
                            imGUI.hideGUIContent = GUILayout.Toggle(imGUI.hideGUIContent,
                                $"{imGUI.IMGUITitle} ({imGUI.IMGUIOrder})", "Box");
                            GUI.color = Color.white;
                        }

                        // 画内容
                        if (!imGUI.hideGUIContent)
                            imGUI.IMGUILeftSideDraw();
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        public override int IMGUIOrder => 0;
    }
}