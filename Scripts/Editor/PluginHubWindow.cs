﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PluginHub.Editor;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR;
using Debug = UnityEngine.Debug;
using Lightmapping = UnityEditor.Lightmapping;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace PluginHub.Editor
{
    //插件中心 主窗口
    public class PluginHubWindow : EditorWindow
    {
        /// <summary>
        /// PluginHubWindow单例，不要直接使用这个变量，使用Window属性
        /// </summary>
        private static PluginHubWindow _window;

        public static PluginHubWindow Window
        {
            get
            {
                if (_window == null)
                {
                    // Debug.Log("创建 PluginHub 主窗口");//调用Close()之后会释放_window实例。
                    _window = GetWindow<PluginHubWindow>("Plugin Hub Window");
                    _window.minSize = new Vector2(325, 200);
                }
                return _window;
            }
        }

        //菜单栏
        [UnityEditor.MenuItem("Window/Plugin Hub Window %&R", false, -10000)]
        public static void SwitchWindow()
        {
            if (Window.docked)//当窗口被停靠时，显示窗口
            {
                Debug.Log("显示 PluginHub 主窗口");
                InitModule();
                Window.Show();//显示窗口
            }
            else//当窗口未被停靠时，切换窗口的显示和隐藏，因此可以使用快捷键切换窗口的显示和隐藏
            {
                Debug.Log("切换 PluginHub 主窗口");
                if (Window.position.x == 0)//在非dock下，可以用Window.position.x == 0判断窗口是否显示
                {
                    InitModule();
                    Window.Show();
                }
                else
                    Window.Close();//调用Close()之后，会释放PluginHubWindow实例。其中的值会消失，所以记得重要的变量使用EditorPrefs保存
            }
        }

        public static void RestartWindow()
        {
            Window.Close();
            InitModule();
            Window.Show();
        }

        private new void Close()
        {
            OnDestroy();
            base.Close();
        }

        //ScriptableObject配置
        private static ModuleConfigSO _moduleConfigSO;
        public static ModuleConfigSO moduleConfigSO
        {
            get
            {
                if (_moduleConfigSO == null)
                    _moduleConfigSO = AssetDatabase.LoadAssetAtPath<ModuleConfigSO>(PluginHubConfig.MODULECONFIT_ASSET_PATH);
                if (_moduleConfigSO == null)
                {
                    // 项目Assets下没有模块配置文件，创建一个
                    _moduleConfigSO = ScriptableObject.CreateInstance<ModuleConfigSO>();
                    _moduleConfigSO.MakeAllModuleConfig();
                    if (!System.IO.Directory.Exists(PluginHubConfig.ASSET_DIR))
                        System.IO.Directory.CreateDirectory(PluginHubConfig.ASSET_DIR);
                    AssetDatabase.CreateAsset(_moduleConfigSO, PluginHubConfig.MODULECONFIT_ASSET_PATH);
                }
                return _moduleConfigSO;
            }
        }

        //退出播放模式时是否显示PluginHubWindow窗口
        public static bool showPluginHubOnExitPlayMode
        {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_showPluginHubOnExitPlayMode", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_showPluginHubOnExitPlayMode", value); }
        }

        //是否显示顶部设置面板
        public static bool showSettingPanel {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_showSettingPanel", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_showSettingPanel", value); }
        }

        //是否启用全局debug模式，在ui上显示一些调试信息，开发目的
        public static bool globalDebugMode
        {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_globalDebugMode", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_globalDebugMode", value); }
        }

        //是否总是刷新gui，会让鼠标指针不处于窗口内也刷新UI,这在某些耗时模块的gui绘制中会产生一定编辑器性能消耗，但是可以让某些模块功能更新更加及时
        public static bool alwaysRepaintGUI {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_alwaysRepaintGUI", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_alwaysRepaintGUI", value); }
        }

        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch(); //秒表 用于计算代码执行时间
        private float lastTime;
        //保存模块的实例列表
        private static List<PluginHubModuleBase> moduleList = new List<PluginHubModuleBase>();

        //用于计算按钮宽度的值
        private float CommonPadding
        {
            get { return EditorPrefs.GetFloat($"{PluginHubFunc.ProjectUniquePrefix}_CommonPadding", 43); }
            set { EditorPrefs.SetFloat($"{PluginHubFunc.ProjectUniquePrefix}_CommonPadding", value); }
        }

        //按钮之间的间距
        private float ButtonPadding
        {
            get { return EditorPrefs.GetFloat($"{PluginHubFunc.ProjectUniquePrefix}_ButtonPadding", 3); }
            set { EditorPrefs.SetFloat($"{PluginHubFunc.ProjectUniquePrefix}_ButtonPadding", value); }
        }

        // private void OnValidate()
        // {
        //     //这里也初始化一下 避免修改脚本后丢失引用
        //     InitModule();
        // }

        // 如果不加这个，在用户修改代码重新编译后，所有模块会从内存中消失。
        [InitializeOnLoadMethod]
        // 释放所有模块，重新加载模块
        private static void InitModule()
        {
            // 先释放之前的模块
            foreach (var module in moduleList)
            {
                module.OnDisable();
                module.OnDestroy();
            }
            moduleList.Clear();

            List<ModuleTabConfig> tabConfigs = moduleConfigSO.tabConfigs;
            for (int i = 0; i < tabConfigs.Count; i++)
            {
                ModuleTabConfig tabConfig = tabConfigs[i];

                for (int j = 0; j < tabConfig.moduleList.Count; j++)
                {
                    MonoScript monoScript = tabConfig.moduleList[j];
                    if (monoScript != null)
                    {
                        //调用模块的构造函数
                        try
                        {
                            // 使用反射调用模块的构造函数
                            ConstructorInfo constructor = monoScript.GetClass().GetConstructor(new Type[] { });
                            PluginHubModuleBase module = (PluginHubModuleBase)constructor.Invoke(null);
                            // Debug.Log($"加载模块：{monoScript.name}");
                            if (module != null)
                            {
                                module.Init(i);
                                // module.OnEnable();
                                moduleList.Add(module);
                            }
                        }
                        catch(InvalidCastException e)
                        {
                            Debug.LogError($"PluginHub 无法将 {monoScript.name} 作为模块加载，仅支持加载继承自 PluginHubModuleBase 的模块类。选择模块配置文件，去除非模块脚本。或者使用预制模块配置，并重启 PluginHub。\n{e}");
                        }catch(Exception e)
                        {
                            Debug.LogError($"加载模块{monoScript.name}时发生错误：{e}");
                        }
                    }
                }
            }
        }

        // 2024年4月7日 该模块生命周期方法暂时被禁用了,有bug,无法很好的使用
        //Allow an editor class method to be initialized when Unity loads without action from the user.
        // 允许编辑器类方法在Unity加载时被初始化，而不需要用户的操作。
        // 该方法会在Unity编辑器打开时调用
        // [InitializeOnLoadMethod]
        // private static void InitializeOnLoadMethod()
        // {
        //     Debug.Log("PH_InitializeOnLoadMethod");
        //     // PluginHubWindow.Window.InitModule();
        //     if (moduleList != null && moduleList.Count > 0)
        //     {
        //         foreach (var module in moduleList)
        //             module.OnInitOnload();
        //     }
        // }

        //当前选择的tab索引
        private int currSelectTabIndex
        {
            set { EditorPrefs.SetInt($"{PluginHubFunc.ProjectUniquePrefix}_currSelectTabIndex", value); }
            get
            {
                int storedValue = EditorPrefs.GetInt($"{PluginHubFunc.ProjectUniquePrefix}_currSelectTabIndex", 0);
                if(storedValue >= moduleConfigSO.tabConfigs.Count)
                    storedValue = 0;
                return storedValue;
            }
        }
        private Vector2 scrollPos;
        private bool isExpandAll = true;

        private void OnGUI()
        {
            DrawTopBar();

            DrawSettingPanel();

            if (globalDebugMode)
            {
                stopwatch.Reset();
                stopwatch.Start(); //  开始监视代码运行时间
                DrawGlobalDebugUI();
            }

            GUILayout.BeginHorizontal("Box");
            {
                string[] selectionTexts = moduleConfigSO.tabConfigs.Select(module => module.tabName).ToArray();
                //绘制选项卡
                currSelectTabIndex = GUILayout.SelectionGrid(currSelectTabIndex, selectionTexts, selectionTexts.Length,GUILayout.MaxWidth(Window.position.width));
                //绘制展开/折叠所有按钮
                string collapseAllText = isExpandAll ? "▼" : "▶";
                if (GUILayout.Button(collapseAllText, GUILayout.ExpandWidth(false)))
                {
                    isExpandAll = !isExpandAll;
                    moduleList.ForEach(module => module.expand = isExpandAll);
                }
            }
            GUILayout.EndHorizontal();

            //绘制滚动条
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(false));
                {
                    for (int i = 0; i < moduleList.Count; i++)
                    {
                        var module = moduleList[i];
                        //绘制属于当前选项卡的模块
                        if (module.tabIndex == currSelectTabIndex)
                            module.DrawModule();
                    }
                }
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            if (globalDebugMode)
            {
                stopwatch.Stop(); //  停止监视
                lastTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal();
            {
                //绘制脚本和配置文件，用于快速定位
                GUI.enabled = false;
                EditorGUILayout.ObjectField(MonoScript.FromScriptableObject(this), typeof(PluginHubWindow), false);
                // EditorGUILayout.ObjectField(moduleConfigSO, typeof(ModuleConfigSO), false);
                GUI.enabled = true;

                //选中模块配置文件按钮
                if (GUILayout.Button(PluginHubFunc.IconContent("d_ScriptableObject Icon", "", "模块配置文件"),PluginHubFunc.IconBtnLayoutOptions))
                {
                    Selection.activeObject = moduleConfigSO;
                }
                //绘制PluginHub开源主页按钮
                if (GUILayout.Button(PluginHubFunc.IconContent("UnityEditor.VersionControl", "", "前往PluginHub开源主页"),PluginHubFunc.IconBtnLayoutOptions))
                {
                    Application.OpenURL("https://github.com/cloudinnng/PluginHub");
                }
                //设置按钮
                Color oldColor = GUI.color;
                if (showSettingPanel)
                    GUI.color = PluginHubFunc.SelectedColor;
                if (GUILayout.Button(PluginHubFunc.IconContent("SettingsIcon@2x", "", ""),PluginHubFunc.IconBtnLayoutOptions))
                    showSettingPanel = !showSettingPanel;
                GUI.color = oldColor;

                //全局调试按钮
                oldColor = GUI.color;
                if (globalDebugMode)
                    GUI.color = PluginHubFunc.SelectedColor;
                string iconName = "DebuggerDisabled";
                if (GUILayout.Button(PluginHubFunc.IconContent(iconName, "", "Global Debug Mode"),PluginHubFunc.IconBtnLayoutOptions))
                {
                    globalDebugMode = !globalDebugMode;
                }
                GUI.color = oldColor;

                //总是刷新按钮
                oldColor = GUI.color;
                if (alwaysRepaintGUI)
                    GUI.color = PluginHubFunc.SelectedColor;
                if (GUILayout.Button(PluginHubFunc.IconContent("Refresh", "", "Always refresh the PluginHubWindow GUI, which makes certain modules that need real-time updates more instantly updated"),PluginHubFunc.IconBtnLayoutOptions))
                {
                    alwaysRepaintGUI = !alwaysRepaintGUI;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSettingPanel()
        {
            //绘制设置面板在顶端
            if (!showSettingPanel)return;

            GUILayout.BeginVertical(PluginHubFunc.GetCustomStyle("SettingPanel"));
            {
                GUILayout.BeginHorizontal();
                {
                    showPluginHubOnExitPlayMode = GUILayout.Toggle(showPluginHubOnExitPlayMode, "退出播放模式时显示PluginHubWindow窗口");
                    if (GUILayout.Button("打开配置文件所在文件夹"))
                        EditorUtility.RevealInFinder(PluginHubConfig.configPath);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        //绘制全局调试界面
        void DrawGlobalDebugUI()
        {
            //红色的背景
            GUILayout.BeginVertical(PluginHubFunc.GetCustomStyle("DebugPanel"));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"窗口GUI时间：{lastTime}ms");
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("打开 IMGUI Debugger"))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Analysis/IMGUI Debugger");
                    }

                    if (GUILayout.Button("GCCollect"))
                    {
                        PluginHubFunc.GC();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Label($"scrollPos: {scrollPos}");

                GUILayout.Label($"position:{this.position}");
                CommonPadding = EditorGUILayout.Slider("CommonPadding", CommonPadding, 0, 100);
                ButtonPadding = EditorGUILayout.Slider("ButtonPadding", ButtonPadding, 0, 100);
            }
            GUILayout.EndVertical();
        }

        //按钮均等宽度横放时计算按钮的宽度，参数为一行放几个按钮
        public float CaculateButtonWidth(int numberPerLines)
        {
            return (position.width - CommonPadding - (numberPerLines - 1) * ButtonPadding) / numberPerLines;
        }

        #region EditorWindow 生命周期函数

        private void OnEnable()//PluginHubWindow显示时调用
        {
            foreach (var module in moduleList)
            {
                module.OnEnable();
            }

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            foreach (var module in moduleList)
            {
                module.OnDisable();
            }

            SceneView.duringSceneGui -= OnSceneGUI;
        }

        //
        private void OnSceneGUI(SceneView sceneView)
        {
            foreach (var module in moduleList)
            {
                module.isDrawingSceneGUI = module.m_OnSceneGUI(sceneView);
            }
        }

        private void OnFocus()//PluginHubWindow获得焦点时调用
        {
            foreach (var module in moduleList)
            {
                module.OnFocus();
            }
        }

        private void OnDestroy()//PluginHubWindow关闭时调用
        {
            foreach (var module in moduleList)
            {
                module.OnDestroy();
            }
        }

        //这段代码使得鼠标不在窗口内的情况下UI也得以更新
        private void OnInspectorUpdate()
        {
            if (alwaysRepaintGUI)
                Repaint();
        }

        private void Update()//即时焦点不在PluginHubWindow上，也会每帧调用。除非关掉PluginHubWindow
        {
            foreach (var module in moduleList)
            {
                if (module.expand)
                {
                    module.OnUpdate();
                    if (module.isDataDirty)
                        module.RefreshData();
                }
            }
        }

        #endregion
    }
}