using System;
using System.Collections;
using System.Collections.Generic;
using PluginHub.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;


// 提供左侧边栏绘制和普通GUI绘制（全屏区域）
// 用户脚本可以通过实现IIMGUI接口，将自己的GUI绘制到屏幕上 (脚本需继承自MonoBehaviour)
// 思路来自Debugger.CustomWindow.ICustomWindowGUI
// 与其不同的是，他不依附于Debugger.CustomWindow，而是直接显示在屏幕上
[ExecuteAlways]
public class IMGUIManager : SceneSingleton<IMGUIManager>, Debugger.CustomWindow.ICustomWindowGUI
{
    public interface IIMGUI
    {
        public bool offerLeftSideDraw => true;// 是否提供左侧边栏绘制
        public void IMGUILeftSideDraw();// 绘制左侧边栏内容GUI
        public void IMGUIDraw();// 绘制普通GUI
        public int IMGUIOrder => 0;
        public string IMGUITitle => GetType().ToString();
    }

    // 继承这个就简单一点
    public abstract class IMGUIBase : MonoBehaviour, IIMGUI
    {
        public bool offerLeftSideDraw => false;
        public virtual void IMGUILeftSideDraw() { }
        public virtual void IMGUIDraw() { }
    }

    public bool showGUI = true;
    [Tooltip("用户设置的缩放因子")]
    public float guiScale = 1.0f;
    public bool showClientTitle = true;

    // 侧边栏
    public bool fullScreenWidth = false;// 是否占满屏幕宽度
    public float leftSideScrollWidth = 400;
    private Vector2 leftScrollPos;


    // 真实屏幕尺寸
    private static Vector2 _realScreenSize;
    // GUI程序开发依赖的屏幕尺寸
    public Vector2 screenSize => new Vector2(_realScreenSize.x / guiScale, _realScreenSize.y / guiScale);

    // 客户端列表
    public List<IIMGUI> clientList = new List<IIMGUI>();


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
        clientList.Clear();
        foreach (var client in FindObjectsOfType<MonoBehaviour>())
        {
            if (client is IIMGUI imGUI)
                clientList.Add(imGUI);
        }
        clientList.Sort((a, b) => a.IMGUIOrder.CompareTo(b.IMGUIOrder));
    }

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

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale,guiScale, 1));
        {
            if(fullScreenWidth)
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
                        if (!imGUI.offerLeftSideDraw)// 不提供左侧边栏绘制, 跳过
                            continue;
                        // 画title
                        if (showClientTitle)
                            GUILayout.Box($"{imGUI.IMGUITitle} ({imGUI.IMGUIOrder})");
                        // 画内容
                        imGUI.IMGUILeftSideDraw();
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            // 普通GUI
            for (int i = 0; i < clientList.Count; i++)
                clientList[i].IMGUIDraw();

        }
        GUI.matrix = originalMatrix;
    }


    public void OnDrawDebuggerGUI()
    {
        GUILayout.BeginHorizontal();
        {
            showGUI = GUILayout.Toggle(showGUI, "显示GUI");
            showClientTitle = GUILayout.Toggle(showClientTitle, "显示客户端标题");
            fullScreenWidth = GUILayout.Toggle(fullScreenWidth, "占满屏幕宽度");
        }
        GUILayout.EndHorizontal();

        GUILayout.Label($"GUIScale: {guiScale:F2}");
        guiScale = GUILayout.HorizontalSlider(guiScale, 0.1f, 2.0f);


        GUILayout.Label($"侧边栏宽度: {leftSideScrollWidth:F0}");

        if(!fullScreenWidth)
            leftSideScrollWidth = GUILayout.HorizontalSlider(leftSideScrollWidth, 100, 2000);

        if (GUILayout.Button("刷新客户端列表"))
            RefreshClientList();
    }
}
