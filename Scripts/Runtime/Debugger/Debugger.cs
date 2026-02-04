using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 2024年10月12日
    /// 增加了全屏模式，在全屏下显示大窗口模式，这个更适合在移动设备上使用
    /// 2024年2月2日
    /// 增加了激活时禁用EventSystem的功能
    /// 2022年8月2日：
    /// CFramework中的IMGUI调试器，此为最新的Debugger调试器。用partial关键字分模块编写
    /// </summary>
    [DefaultExecutionOrder(-100)]
    // [ExecuteAlways]
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
                    Debugger[] debugger = FindObjectsByType<Debugger>(FindObjectsSortMode.None);
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

        #region OnScreenUI

        // 继承此类，以成为客户端
        public interface IDebuggerOnScreenUI
        {
            public void OnScreenUIDraw(float globalGUIScale); // 绘制GUI
            public int OnScreenUIOrder => 0;// 绘制顺序
        }
        // 客户端列表
        public List<IDebuggerOnScreenUI> clientList = new List<IDebuggerOnScreenUI>();

        public void RefreshOnScreenUIClientList()
        {
            // Debug.Log("[IMGUIManager] RefreshClientList");
            clientList.Clear();
            MonoBehaviour[] monoInScene = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // Debug.Log(s.Length);
            foreach (MonoBehaviour client in monoInScene)
            {
                if (client is IDebuggerOnScreenUI imGUI)
                    clientList.Add(imGUI);
            }
            // Debug.Log(clientList.Count);
            clientList.Sort((a, b) => a.OnScreenUIOrder.CompareTo(b.OnScreenUIOrder));
        }

        private void OnValidate()
        {
            RefreshOnScreenUIClientList();
        }

        #endregion

        public enum DebuggerResolutionType
        {
            MEDIUM_640x480,
            LARGE_800x600,
            LARGE_1067x600,// 16:9
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
        [Tooltip("存储最多x条日志信息.考虑在移动平台使用较低的该值")]
        public int consoleMaxLine = 300;
        [Tooltip("是否使用R键重新加载场景")]
        public bool useRKeyReloadScene = false;
        [Tooltip("采用全屏模式显示大窗口")]
        public bool fullScreenMode = true;
        [Tooltip("全屏模式下是否采用全屏宽度")]
        public bool fullScreenWidth = false;
        [Tooltip("使用全屏模式时顶部留白,在刘海屏手机上可能需要设置")]
        public float fullScreenModeTopSpace = 0;

        [Header("OnScreenUI")]
        [Tooltip("是否使用屏幕上的调式UI，通过实现IDebuggerOnScreenUI接口来添加该UI")]
        public bool useOnScreenUI = true;
        [Tooltip("缩放因子")]
        public float onScreenUIGUIScale = 2.5f;
        [Tooltip("OnScreenUI的颜色")]
        public Color onScreenUIColor = Color.red;

        [Space(10)]

        private bool _isShowDebugger = false; //dont call this
        private bool _showFullWindow = false; //dont call this
        private Rect _minWindowRect;
        private Rect _fullWindowRect;
        private float _debuggerWindowGUIScale = 1;
        private int _selectIndex = 0; //选择的tab索引
        private readonly FpsCounter _fps = new FpsCounter(0.5f);
        private bool _showCreditsPage = false; //是否显示关于页面

        //window
        private ConsoleWindow _consoleWindow = new ConsoleWindow(); //控制台窗口

        private InfoWindow _infoWindow = new InfoWindow(); //信息窗口
        private CustomWindow _customWindow = new CustomWindow(); //自定义窗口，交给应用程序自定义
        private UtilitiesWindow _utilitiesWindow = new UtilitiesWindow(); //工具窗口

        //保存已注册的DebuggerWindow <path,window>
        private Dictionary<string, IDebuggerWindow> registeredWindowDic = new Dictionary<string, IDebuggerWindow>();
        private string[] topstTabNames; //顶部选项卡tab文本



        // 返回经过缩放后的屏幕大小
        private Vector2 realScreenSize => new Vector2(Screen.width / _debuggerWindowGUIScale, Screen.height / _debuggerWindowGUIScale);


        private void Start()
        {
            //设置默认值
            isShowFullWindow = defaultShowFullWindow;
            _selectIndex = defaultTab;
            _minWindowRect = new Rect(0, 0, MIN_WINDOW_SIZE.x, MIN_WINDOW_SIZE.y);
            NORMAL_WINDOW_SIZE = GetResolution(resolutionType);
            _fullWindowRect = new Rect(0, 0, NORMAL_WINDOW_SIZE.x, NORMAL_WINDOW_SIZE.y);
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
            RefreshOnScreenUIClientList();
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
                        _debuggerWindowGUIScale = Screen.height / (_fullWindowRect.height + 20);
                    else //根据宽
                        _debuggerWindowGUIScale = Screen.width / (_fullWindowRect.width + 20);
                    //刷新客户端
                    _customWindow.RefreshDebuggerClientRoutine();
                    RefreshOnScreenUIClientList();
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
            EventSystem eventSystem = FindObjectsByType<EventSystem>(FindObjectsSortMode.None).FirstOrDefault();
            if (eventSystem)
            {
                if (isShowFullWindow && isShowDebugger)
                    eventSystem.enabled = false;
                else
                    eventSystem.enabled = true;
            }
        }

        //将会同时设置mini窗口和正常窗口左上角坐标到指定位置
        public void SetWindowPosition(int x, int y)
        {
            _minWindowRect.position = new Vector2(x, y);
            _fullWindowRect.position = new Vector2(x, y);
        }

        void Update()
        {
            _fps.Update();
            _consoleWindow.RefreshCount();
            topstTabNames[0] = $"<color={GetTextColor()}>Console</color>";

            //快捷键
            //显示/隐藏调试器  1键左边那个键
            if (isGestureDone() || InputEx.GetKeyDown(KeyCode.BackQuote))
            {
                isShowDebugger = !isShowDebugger;
            }

            //切换小窗口/正常窗口
            if (isShowDebugger && InputEx.GetKeyDown(KeyCode.Tab))
            {
                isShowFullWindow = !isShowFullWindow;
            }

            //重新加载场景
            if (useRKeyReloadScene && InputEx.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        }


        private void DrawCreditsPage()
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("Back to Debugger"))
                    _showCreditsPage = false;

                GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 50;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Credits", titleStyle);

                GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontSize = 20;

                GUILayout.Label("致用户：", headerStyle);
                GUILayout.Label("使用`键（键盘左上角ESC键下面）显示/隐藏调试器，移动设备也可以用旋转手势呼出，只需手指按下在屏幕中画圈即可。");
                GUILayout.Label("Debugger有两种窗口模式，仅显示FPS信息的小窗口和全功能大窗口。按Tab键切换小窗口/正常窗口，也可以点击FPS文本和右上角的最小化按钮来切换。");
                GUILayout.Label("通常，您只需要关注 Console 标签页和 Custom 标签页。");
                GUILayout.Label("当互动软件发生错误和功能异常时，请携带Console标签页中的信息联系开发者。");
                GUILayout.Label("在Custom标签页中，您可以看到互动软件的自定义界面，这些功能是由开发者为您定制的，每款互动软件各不相同，您可以在这里查看互动软件的状态与管理互动软件的运行。");
                GUILayout.Space(20);
                GUILayout.Label("致开发者：", headerStyle);

                GUILayout.Label("您正在使用的 GUI 覆盖层 Debugger 是开源项目 PluginHub 的一部分。");
                GUILayout.Label("开发者可以在 Custom 页面为每个互动软件添加自定义的 GUI 界面，这在制作系统后台时非常有用。");
                GUILayout.Label("新 PluginHub 现在是一份加速 Unity3D 项目开发的代码模板。包含 Runtime(运行时) 和 Editor(编辑器) 功能");
                GUILayout.Label("目前 PluginHub 由我一人维护，您可以进入 PluginHub 开源主页点击右上角的 Star 按钮来支持我的开发。");

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("开源页面：https://github.com/cloudinnng/PluginHub");
                    if (GUILayout.Button("带我去 PluginHub 开源主页", GUILayout.Width(200)))
                        Application.OpenURL("https://github.com/cloudinnng/PluginHub");
                }
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Back to Debugger", GUILayout.Height(50)))
                    _showCreditsPage = false;

            }
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        private void DrawBigWindow(int windowId)
        {
            if (fullScreenMode)
                GUILayout.Space(fullScreenModeTopSpace);
            else
                GUI.DragWindow(new Rect(0, 0, float.MaxValue, 25));
            GUILayout.Space(5);

            if (_showCreditsPage)
            {
                DrawCreditsPage();
            }
            else
            {
                //顶部按钮
                GUILayout.BeginHorizontal();
                {
                    //绘制顶层Tab标签按钮
                    _selectIndex = GUILayout.Toolbar(_selectIndex, topstTabNames, GUILayout.Height(25f));

                    //最小化按钮
                    if (GUILayout.Button("?", GUILayout.Height(25f), GUILayout.Width(30f)))
                        _showCreditsPage = !_showCreditsPage;

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
        }

        private void OnGUI()
        {
            if (useOnScreenUI)
            {
                Matrix4x4 tmp1 = GUI.matrix;
                GUI.color = onScreenUIColor;
                {
                    GUI.matrix = Matrix4x4.Scale(new Vector3(onScreenUIGUIScale, onScreenUIGUIScale, 1));
                    {
                        foreach (IDebuggerOnScreenUI client in clientList)
                        {
                            client.OnScreenUIDraw(onScreenUIGUIScale);
                        }
                    }
                    GUI.matrix = tmp1;
                }
                GUI.color = Color.white;
            }


            if (!isShowDebugger) return;

            //整体缩放绘制
            Matrix4x4 tmp = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_debuggerWindowGUIScale, _debuggerWindowGUIScale, 1));
            {
                if (isShowFullWindow)
                {
                    if (fullScreenMode)
                    {
                        GUILayout.BeginArea(new Rect(0, 0, realScreenSize.x, realScreenSize.y));
                        {
                            if (fullScreenWidth)
                                GUILayout.BeginVertical("Box", GUILayout.Height(realScreenSize.y), GUILayout.Width(realScreenSize.x));
                            else
                                GUILayout.BeginVertical("Box", GUILayout.Height(realScreenSize.y), GUILayout.Width(_fullWindowRect.width));
                            {
                                DrawBigWindow(1);
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndArea();
                    }
                    else
                    {
                        _fullWindowRect = GUILayout.Window(0, _fullWindowRect, DrawBigWindow, "<b>Debugger</b>");
                    }
                }
                else
                {
                    _minWindowRect = GUILayout.Window(0, _minWindowRect, DrawMinWindow, "<b>Debugger</b>");
                }
            }
            GUI.matrix = tmp; //记得设置回来 不然会有bug
        }
    }
}