using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PluginHub;
using PluginHub.Data;
using PluginHub.Helper;
using PluginHub.Module;
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

namespace PluginHub
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
                    _window = GetWindow<PluginHubWindow>("Plugin Hub Window");
                    _window.minSize = new Vector2(325, 200);
                    _window.InitModule();
                }
                return _window;
            }
        }

        //菜单栏
        [UnityEditor.MenuItem("Window/Plugin Hub Window %&R", false, -10000)]
        public static void ShowWindow()
        {
            Debug.Log("显示 PluginHub 主窗口");
            Window.Show();//显示窗口
        }

        public static void RestartWindow()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
            ShowWindow();
        }

        //ScriptableObject配置
        private ModuleConfigSO _moduleConfigSO;
        public ModuleConfigSO moduleConfigSO
        {
            get
            {
                if (_moduleConfigSO == null)
                    _moduleConfigSO = Resources.Load<ModuleConfigSO>("PH_ModuleConfigSO");
                return _moduleConfigSO;
            }
        }

        //退出播放模式时是否显示PluginHubWindow窗口
        public static bool showPluginHubOnExitPlayMode
        {
            get { return EditorPrefs.GetBool("showPluginHubOnExitPlayMode", true); }
            set { EditorPrefs.SetBool("showPluginHubOnExitPlayMode", value); }
        }
        
        //是否显示顶部设置面板
        public static bool showSettingPanel = false;
        
        //是否启用全局debug模式，在ui上显示一些调试信息，开发目的
        public static bool globalDebugMode = false;

        //是否总是刷新gui，会让鼠标指针不处于窗口内也刷新UI,这在某些耗时模块的gui绘制中会产生一定编辑器性能消耗，但是可以让某些模块功能更新更加及时
        public static bool alwaysRefreshGUI = false;

        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch(); //秒表 用于计算代码执行时间
        private float lastTime;
        //保存模块的实例列表
        private static List<PluginHubModuleBase> moduleList = new List<PluginHubModuleBase>();

        //用于计算按钮宽度的值
        private float CommonPadding
        {
            get { return EditorPrefs.GetFloat("CommonPadding", 40); }
            set { EditorPrefs.SetFloat("CommonPadding", value); }
        }

        //按钮之间的间距
        private float ButtonPadding
        {
            get { return EditorPrefs.GetFloat("ButtonPadding", 3); }
            set { EditorPrefs.SetFloat("ButtonPadding", value); }
        }

        private void OnValidate()
        {
            //这里也初始化一下 避免修改脚本后丢失引用
            InitModule();
        }

        public void InitModule()
        {
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
                        PluginHubModuleBase module = (PluginHubModuleBase)monoScript.GetClass().GetConstructor(new Type[]{}).Invoke(null);
                        if (module != null)
                        {
                            module.Init(i);
                            moduleList.Add(module);
                        }
                    }
                }
            }
        }

        private int currSelectTabIndex; //当前选择的tab索引
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
                currSelectTabIndex = GUILayout.SelectionGrid(currSelectTabIndex, selectionTexts, selectionTexts.Length);
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
                EditorGUILayout.ObjectField(moduleConfigSO, typeof(ModuleConfigSO), false);
                GUI.enabled = true;


                //绘制PluginHub开源主页按钮
                if (GUILayout.Button(PluginHubFunc.Icon("UnityEditor.VersionControl", "", "前往PluginHub开源主页"),PluginHubFunc.IconBtnLayoutOptions))
                {
                    Application.OpenURL("https://github.com/cloudinnng/PluginHub");
                }
                
                //设置按钮
                Color oldColor = GUI.color;
                if (showSettingPanel)
                    GUI.color = Color.red;
                if (GUILayout.Button(PluginHubFunc.Icon("SettingsIcon@2x", "", ""),PluginHubFunc.IconBtnLayoutOptions))
                {
                    showSettingPanel = !showSettingPanel;
                }
                GUI.color = oldColor;

                //全局调试按钮
                oldColor = GUI.color;
                if (globalDebugMode)
                    GUI.color = Color.red;
                string iconName = "DebuggerDisabled";
                if (GUILayout.Button(PluginHubFunc.Icon(iconName, "", "Global Debug Mode"),PluginHubFunc.IconBtnLayoutOptions))
                {
                    globalDebugMode = !globalDebugMode;
                }
                GUI.color = oldColor;

                //总是刷新按钮
                oldColor = GUI.color;
                if (alwaysRefreshGUI)
                    GUI.color = Color.red;
                if (GUILayout.Button(PluginHubFunc.Icon("Refresh", "", "Always refresh the GUI, which makes certain modules that need real-time updates more instantly updated"),PluginHubFunc.IconBtnLayoutOptions))
                {
                    alwaysRefreshGUI = !alwaysRefreshGUI;
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
                showPluginHubOnExitPlayMode = GUILayout.Toggle(showPluginHubOnExitPlayMode, "退出编辑模式时显示PluginHubWindow窗口");
            }
            GUILayout.EndVertical();
        }


        //绘制全局调试界面
        void DrawGlobalDebugUI()
        {
            //红色的背景
            GUILayout.BeginVertical(PluginHubFunc.GetCustomStyle("DebugBox"));
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

        #region EditorWindowFunction

        private void OnEnable()
        {
            foreach (var module in moduleList)
            {
                module.OnEnable();
            }
        }

        private void OnFocus()
        {
            foreach (var module in moduleList)
            {
                module.OnFocus();
            }
        }

        private void OnDestroy()
        {
            foreach (var module in moduleList)
            {
                module.OnDestroy();
            }
            // if (thisWindow != null)
            // {
            //     GameObject.DestroyImmediate(thisWindow);
            // }
        }

        //这段代码使得鼠标不在窗口内的情况下UI也得以更新
        private void OnInspectorUpdate()
        {
            if (alwaysRefreshGUI)
                Repaint();
        }

        private void Update()
        {
            foreach (var module in moduleList)
            {
                module.OnUpdate();
            }
        }

        #endregion
    }
}