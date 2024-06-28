using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 2024年2月2日
    /// 增加了激活时禁用EventSystem的功能
    /// 2022年8月2日：
    /// CFramework中的IMGUI调试器，此为最新的Debugger调试器。用partial关键字分模块编写
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public partial class Debugger : MonoBehaviour
    {
        #region singleton

        private static Debugger _instance;

        public static Debugger Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debugger[] debugger = FindObjectsOfType<Debugger>();
                    if (debugger != null)
                    {
                        if (debugger.Length == 1)
                            _instance = debugger[0];
                        else
                            Debug.LogError("There is more than one Debugger in the scene.");
                    }
                }

                return _instance;
            }
        }

        #endregion

        public enum DebuggerResolutionType
        {
            MEDIUM_640x480,
            LARGE_800x600,
            LARGE_1067x600,
            XLARGE_1024x768,
        }
        private static Vector2Int GetResolution(DebuggerResolutionType type)
        {
            switch (type)
            {
                case DebuggerResolutionType.MEDIUM_640x480:
                    return new Vector2Int(640, 480);
                case DebuggerResolutionType.LARGE_800x600:
                    return new Vector2Int(800, 600);
                case DebuggerResolutionType.LARGE_1067x600:
                    return new Vector2Int(1067, 600);
                case DebuggerResolutionType.XLARGE_1024x768:
                    return new Vector2Int(1024, 768);
                default:
                    return new Vector2Int(800, 600);
            }
        }


        [Header("默认设置")]
        public bool defaultOpen = true; //默认情况下是否打开调试器
        public bool defaultShowFullWindow = false; //debugger有两种窗口，一种是仅显示FPS的小窗口，一种是有更多功能的大窗口
        public int defaultTab = 0; //默认打开的tab页
        public Vector2 defaultWindowPosition = new Vector2(10, 10);

        [Header("窗口分辨率（移动设备推荐使用较小的分辨率）")]
        public DebuggerResolutionType resolutionType = DebuggerResolutionType.LARGE_800x600;
        private Vector2Int MIN_WINDOW_SIZE = new Vector2Int(60, 60);
        private Vector2Int NORMAL_WINDOW_SIZE;

        [Header("配置")]
        [Tooltip("是否在显示调试器大窗口的时候禁用 EventSystem 以避免响应下方的UGUI事件")]
        public bool deactiveEventSystem = false;


        private bool _isShowDebugger = false; //dont call this
        private bool _showFullWindow = false; //dont call this
        private Rect _minWindowRect;
        private Rect _normalWindowRect;
        private float _guiScale = 1;
        private int _selectIndex = 0; //选择的tab索引
        private readonly FpsCounter _fps = new FpsCounter(0.5f);


        //window
        private ConsoleWindow _consoleWindow = new ConsoleWindow(); //控制台窗口

        private InfoWindow _infoWindow = new InfoWindow(); //信息窗口
        private CustomWindow _customWindow = new CustomWindow(); //自定义窗口，交给应用程序自定义
        private UtilitiesWindow _utilitiesWindow = new UtilitiesWindow(); //工具窗口

        //保存已注册的DebuggerWindow <path,window>
        private Dictionary<string, IDebuggerWindow> registeredWindowDic = new Dictionary<string, IDebuggerWindow>();
        private string[] topstTabNames; //顶部选项卡tab文本

        private void Start()
        {
            //设置默认值
            isShowFullWindow = defaultShowFullWindow;
            _selectIndex = defaultTab;
            _minWindowRect = new Rect(0, 0, MIN_WINDOW_SIZE.x, MIN_WINDOW_SIZE.y);
            NORMAL_WINDOW_SIZE = GetResolution(resolutionType);
            _normalWindowRect = new Rect(0, 0, NORMAL_WINDOW_SIZE.x, NORMAL_WINDOW_SIZE.y);
            SetWindowPosition((int)defaultWindowPosition.x, (int)defaultWindowPosition.y);
            isShowDebugger = defaultOpen;


            //注册Debugger窗口
            RegisterDebuggerWindow("Console", _consoleWindow);
            RegisterDebuggerWindow("Info", _infoWindow);
            RegisterDebuggerWindow("Custom", _customWindow);
            RegisterDebuggerWindow("Utilities", _utilitiesWindow);
            _consoleWindow.OnStart();
            _infoWindow.OnStart();
            _customWindow.OnStart();
            _utilitiesWindow.OnStart();

            //初始化顶部选项卡名字  
            topstTabNames = registeredWindowDic.Select(kvp => kvp.Key).ToArray();
        }

        private void RegisterDebuggerWindow(string path, IDebuggerWindow window)
        {
            registeredWindowDic.Add(path, window);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.logMessageReceived += _consoleWindow.OnConsoleReceivedLog;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.logMessageReceived -= _consoleWindow.OnConsoleReceivedLog;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            _customWindow.RefreshDebuggerClientRoutine();
        }

        public bool isShowDebugger // call this
        {
            get { return _isShowDebugger; }
            set
            {
                _isShowDebugger = value;
                if (_isShowDebugger) //显示的时候自动调整GUI缩放
                {
                    if (Screen.width > Screen.height) //根据高
                        _guiScale = Screen.height / (_normalWindowRect.height + 20);
                    else //根据宽
                        _guiScale = Screen.width / (_normalWindowRect.width + 20);
                    //刷新客户端
                    _customWindow.RefreshDebuggerClientRoutine();
                }
                SetEventSystem();
            }
        }
        public bool isShowFullWindow // call this
        {
            get { return _showFullWindow; }
            set
            {
                _showFullWindow = value;
                SetEventSystem();
            }
        }

        private void SetEventSystem()
        {
            if (!deactiveEventSystem)
                return;
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem && isShowFullWindow && isShowDebugger)
                eventSystem.enabled = false;
            else
                eventSystem.enabled = true;
        }

        //将会同时设置mini窗口和正常窗口左上角坐标到指定位置
        public void SetWindowPosition(int x, int y)
        {
            _minWindowRect.position = new Vector2(x, y);
            _normalWindowRect.position = new Vector2(x, y);
        }

        void Update()
        {
            _fps.Update();
            _consoleWindow.RefreshCount();
            topstTabNames[0] = $"<color={GetTextColor()}>Console</color>";

            //快捷键
            //显示/隐藏调试器  1键左边那个键
            if (isGestureDone() || Input.GetKeyDown(KeyCode.BackQuote))
            {
                isShowDebugger = !isShowDebugger;
            }

            //切换小窗口/正常窗口
            if (isShowDebugger && Input.GetKeyDown(KeyCode.Tab))
            {
                isShowFullWindow = !isShowFullWindow;
            }
        }


        private void DrawMinWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, float.MaxValue, 25));
            GUILayout.Space(5);

            //画FPS
            if (GUILayout.Button($"<b><color={GetTextColor()}>FPS:{_fps.CurrentFps:F1}</color></b>",
                    GUILayout.Width(100f),
                    GUILayout.Height(40)))
            {
                isShowFullWindow = true;
            }
        }

        //有错误  FPS文字变红
        private string GetTextColor()
        {
            return _consoleWindow.ErrorCount <= 0 ? "white" : "red";
            ;
        }

        private void DrawBigWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, float.MaxValue, 25));
            GUILayout.Space(5);

            //顶部按钮
            GUILayout.BeginHorizontal();
            {
                //绘制顶层Tab标签按钮
                _selectIndex = GUILayout.Toolbar(_selectIndex, topstTabNames, GUILayout.Height(25f));

                //最小化按钮
                if (GUILayout.Button("_", GUILayout.Height(25f), GUILayout.Width(30f)))
                    isShowFullWindow = false;

                //关闭调试器按钮
                if (GUILayout.Button("X", GUILayout.Height(25f), GUILayout.Width(30f)))
                    isShowDebugger = false;
            }
            GUILayout.EndHorizontal();


            //画出选择的窗口
            registeredWindowDic.Values.ToArray()[_selectIndex].OnDraw();
        }

        private void OnGUI()
        {
            if (!isShowDebugger) return;

            //整体缩放绘制
            Matrix4x4 tmp = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_guiScale, _guiScale, 1));
            {
                if (isShowFullWindow)
                    _normalWindowRect = GUILayout.Window(0, _normalWindowRect, DrawBigWindow, "<b>Debugger</b>");
                else
                    _minWindowRect = GUILayout.Window(0, _minWindowRect, DrawMinWindow, "<b>Debugger</b>");
            }
            GUI.matrix = tmp; //记得设置回来 不然会有bug
        }
    }
}