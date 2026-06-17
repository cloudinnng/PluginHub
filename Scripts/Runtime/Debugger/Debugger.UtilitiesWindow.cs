using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace PluginHub.Runtime
{
    public partial class Debugger
    {
        public class UtilitiesWindow : ScrollableDebuggerWindowBase
        {
            private string[] buildSceneNames;
            private bool _intentSetQualityLevel = false; //是否准备设置质量等级


            private string currentPath;

            private string tempText = "";
            // 存储飞书云文件的<文件token,文件名>
            private Dictionary<string, string> feiShuCloudFiles = new Dictionary<string, string>();
            // private float resolutionScale = 1;

            private bool exchangeWidthHeight
            {
                get => PlayerPrefs.GetInt("PH_portrait", 0) == 1;
                set => PlayerPrefs.SetInt("PH_portrait", value ? 1 : 0);
            }


            private int _selectedTab = 0;
            private readonly string[] tabNames = { "Common", "Scene", "PersistentDataPath", "Hierarchy" };

            // ========== Runtime Hierarchy 模块相关字段 ==========
            // 按 Transform.GetInstanceID() 记录每个节点的展开状态；销毁后残留 key 开销极小，不做清理
            private readonly Dictionary<int, bool> _hierarchyExpanded = new Dictionary<int, bool>();
            // 每层缩进像素，行布局靠它实现树形结构的视觉
            private const float HIERARCHY_INDENT = 14f;
            // 折叠按钮与 Toggle 的固定宽度，无子级时用同宽 Space 占位保持列对齐
            private const float HIERARCHY_FOLDOUT_WIDTH = 20f;
            // 用于探测 DontDestroyOnLoad 场景的占位对象（在该场景中创建任何 GO 即可获得 scene 引用）
            // 设为 HideAndDontSave，并在绘制 DDOL 根对象时跳过该实例本身
            private GameObject _ddolProbe;
            // 当前选中的层级节点，用于在面板下方显示 Transform 详细信息
            // 引用类型，对象被销毁时 Unity 重载的 == null 会返回 true，绘制前需做空检查
            private Transform _selectedTransform;

            public override void OnStart()
            {
                base.OnStart();
                currentPath = Application.persistentDataPath;
            }

            protected override void OnDrawToolbar()
            {
                _selectedTab = GUILayout.Toolbar(_selectedTab, tabNames);
            }

            protected override void OnDrawScrollableWindow()
            {
                GUILayout.BeginVertical("Box");
                {
                    switch (_selectedTab)
                    {
                        case 0:
                            DrawResolutionModule();
                            DrawDebugModule();
                            DrawFrameRateModule();
                            break;
                        case 1:
                            DrawSceneModule();
                            break;
                        case 2:
                            DrawPersistentDataModule();
                            break;
                        case 3:
                            DrawRuntimeHierarchyModule();
                            break;
                    }
                }
                GUILayout.EndVertical();
            }

            Vector2Int[] commonResolution = new Vector2Int[]
            {
                new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3840, 2160),
            };


            private void DrawResolutionModule()
            {
                //移动平台上不显示
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
                    return;

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Resolution / Screen");
                    GUILayout.FlexibleSpace();
                    if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer
                        || Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
                    {
                        //最小化应用程序按钮（Windows 独立构建：调用 user32 ShowWindow 最小化 Player 窗口）
                        if (GUILayout.Button("_", GUILayout.Height(25f), GUILayout.Width(30f)))
                            WindowDisplayHelper.MinimizeApplicationWindow();

                        // 最大化应用程序按钮
                        bool currentFullScreen = Screen.fullScreen;
                        GUI.color = currentFullScreen ? Color.cyan : Color.white;
                        if (GUILayout.Button(currentFullScreen ? "⧉" : "囗", GUILayout.Height(25f), GUILayout.Width(30f)))
                        {
                            if (currentFullScreen)
                            {
                                Screen.fullScreen = false;
                                ScreenSetting.Instance?.ApplyAutoWindowSize();
                            }
                            else
                            {
                                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                            }
                            Instance.StartCoroutine(PluginHubRuntime.DelayAction(0.1f, Instance.AdjustDebuggerWindowGUIScale));
                        }
                        GUI.color = Color.white;
                        // 关闭应用程序按钮
                        if (GUILayout.Button("X", GUILayout.Height(25f), GUILayout.Width(30f)))
                            Application.Quit();
                    }
                }

                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("FullScreenMode :");
                        Screen.fullScreenMode = (FullScreenMode)GUILayout.SelectionGrid((int)Screen.fullScreenMode, Enum.GetNames(typeof(FullScreenMode)), 4);
                    }
                    GUILayout.EndHorizontal();

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"Resolution Quick Selection ({Screen.width}x{Screen.height}):");
                        GUILayout.FlexibleSpace();
                        GUI.color = Screen.fullScreen ? Color.cyan : Color.white;
                        Vector2Int designResolution = ScreenSetting.Instance.designResolution;
                        if (GUILayout.Button($"Design Resolution ({designResolution.x}x{designResolution.y})"))
                        {
                            Screen.SetResolution(designResolution.x, designResolution.y, Screen.fullScreen);
                        }
                        if (GUILayout.Button("FullScreen"))
                        {
                            Screen.fullScreen = !Screen.fullScreen;
                        }
                        GUI.color = exchangeWidthHeight ? Color.cyan : Color.white;
                        if (GUILayout.Button("Exchange Width/Height"))
                        {
                            exchangeWidthHeight = !exchangeWidthHeight;
                        }
                        GUI.color = Color.white;
                    }

                    GUILayout.BeginHorizontal();
                    {
                        // 显示所有分辨率
                        string[] names = commonResolution.Select(v => exchangeWidthHeight ? $"{v.y} x {v.x}" : $"{v.x} x {v.y}").ToArray();
                        int selected = GUILayout.SelectionGrid(-1, names, 5);
                        if (selected != -1)
                        {
                            if (exchangeWidthHeight)
                                Screen.SetResolution(commonResolution[selected].y, commonResolution[selected].x, false);
                            else
                                Screen.SetResolution(commonResolution[selected].x, commonResolution[selected].y, false);
                            Instance.StartCoroutine(PluginHubRuntime.DelayAction(0.1f, Instance.AdjustDebuggerWindowGUIScale));
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button($"15 Pro max"))
                            TryResolutionWindowed(1290, 2796, exchangeWidthHeight);
                        if (GUILayout.Button($"iPad Air 5"))
                            TryResolutionWindowed(1536, 2048, exchangeWidthHeight);
                        if (GUILayout.Button($"Lenovo Xiaoxin Pad Pro 2025 edition"))
                            TryResolutionWindowed(1840, 2944, exchangeWidthHeight);

                    }
                    GUILayout.EndHorizontal();

                }
                GUILayout.EndVertical();
            }

            // 将游戏分辨率设置成以给定分辨率尽可能大的分辨率保持宽高比的窗口模式。（给任务栏留出空间）
            // 这在PC端快速预览移动设备的屏幕比例时非常有用
            // 使用 exchangeWidthHeight 可以很容易的切换横屏和竖屏
            private void TryResolutionWindowed(int tryWidth, int tryHeight, bool mExchangeWidthHeight = false)
            {
                if (mExchangeWidthHeight)
                {
                    (tryWidth, tryHeight) = (tryHeight, tryWidth);
                }

                float factorForTaskBar = 0.88f;// 一个比例用于给任务栏留出空间
                float windowedRatio = (float)tryWidth / tryHeight;
                bool windowIsHorizontal = tryWidth > tryHeight;

                int monitorWidth = Screen.currentResolution.width;
                int monitorHeight = Screen.currentResolution.height;
                float screenRatio = (float)monitorWidth / monitorHeight;
                if (windowedRatio > screenRatio)
                {
                    int useWidth = (int)(monitorWidth * factorForTaskBar);
                    int useHeight = (int)(useWidth / windowedRatio);
                    Screen.SetResolution(useWidth, useHeight, false);
                }
                else
                {
                    int useHeight = (int)(monitorHeight * factorForTaskBar);
                    int useWidth = (int)(useHeight * windowedRatio);
                    Screen.SetResolution(useWidth, useHeight, false);
                }
                Instance.StartCoroutine(PluginHubRuntime.DelayAction(0.1f, Instance.AdjustDebuggerWindowGUIScale));
            }

            private void DrawDebugModule()
            {
                GUILayout.Label("Debug:");
                GUILayout.BeginVertical("Box");
                {
                    if (_intentSetQualityLevel)
                    {
                        GUILayout.Label("选择一个画面质量:");
                        int count = QualitySettings.names.Length;
                        for (int i = 0; i < count; i++)
                        {
                            if (GUILayout.Button(QualitySettings.names[i]))
                            {
                                QualitySettings.SetQualityLevel(i);
                                _intentSetQualityLevel = false;
                                break;
                            }
                        }

                        if (GUILayout.Button("取消"))
                        {
                            _intentSetQualityLevel = false;
                        }
                    }

                    if (!_intentSetQualityLevel)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label("System:");
                                if (GUILayout.Button("重载当前场景"))
                                {
                                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                                }

                                //设置与显示画面质量
                                if (GUILayout.Button(
                                        $"切换质量: {QualitySettings.names[QualitySettings.GetQualityLevel()]}"))
                                {
                                    _intentSetQualityLevel = true;
                                }

                                //只有windows平台才有打开屏幕键盘这个功能
                                if (Application.platform == RuntimePlatform.WindowsPlayer ||
                                    Application.platform == RuntimePlatform.WindowsEditor)
                                {
                                    if (GUILayout.Button("系统屏幕键盘(osk)"))
                                    {
                                        System.Diagnostics.Process.Start("osk");
                                    }
                                }

                                if (GUILayout.Button("尝试释放内存"))
                                {
                                    Resources.UnloadUnusedAssets();
                                    GC.Collect();
                                }

                                if (GUILayout.Button("关闭应用程序"))
                                {
                                    Application.Quit();
                                }
                                if (Application.platform == RuntimePlatform.WindowsPlayer && GUILayout.Button("重启应用程序"))
                                {
                                    RestartApplication();
                                }
                            }
                            GUILayout.EndVertical();


                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label("Clear:");
                                if (GUILayout.Button("PlayerPrefs"))
                                {
                                    PlayerPrefs.DeleteAll();
                                }

                                if (GUILayout.Button("Persistent Path"))
                                {
                                    string path = Application.persistentDataPath;
                                    DirectoryInfo directory = new DirectoryInfo(path);
                                    if (directory.Exists)
                                    {
                                        //删除文件夹下的所有文件
                                        FileInfo[] files = directory.GetFiles();
                                        foreach (FileInfo file in files)
                                            file.Delete();
                                        //删除文件夹下的所有子文件夹
                                        DirectoryInfo[] directories = directory.GetDirectories();
                                        foreach (DirectoryInfo dir in directories)
                                            dir.Delete(true);
                                    }
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }
                    Instance.useOnScreenUI = GUILayout.Toggle(Instance.useOnScreenUI, "使用OnScreenUI");
                }
                GUILayout.EndVertical();
            }

            public static void RestartApplication()
            {
                string exePath = Application.dataPath.Replace("_Data", ".exe");
                System.Diagnostics.Process.Start(exePath);
                Application.Quit();
            }

            private void DrawFrameRateModule()
            {
                // https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html
                GUILayout.Label($"TargetFrameRate:{Application.targetFrameRate}");
                GUILayout.BeginHorizontal("Box");
                {
                    if (GUILayout.Button("-1"))
                        Application.targetFrameRate = -1;
                    if (GUILayout.Button("15"))
                        Application.targetFrameRate = 15;
                    if (GUILayout.Button("30"))
                        Application.targetFrameRate = 30;
                    if (GUILayout.Button("50"))
                        Application.targetFrameRate = 50;
                    if (GUILayout.Button("60"))
                        Application.targetFrameRate = 70;
                    if (GUILayout.Button("9999"))
                        Application.targetFrameRate = 9999;
                }
                GUILayout.EndHorizontal();
            }

            private void DrawSceneModule()
            {
                GUILayout.Label($"当前场景：{SceneManager.GetActiveScene().name}");
                if (GUILayout.Button("重载当前场景"))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                if (buildSceneNames == null)
                {
                    buildSceneNames = new string[SceneManager.sceneCountInBuildSettings];
                    for (int i = 0; i < buildSceneNames.Length; i++)
                    {
                        //string sceneName = SceneManager.GetSceneByBuildIndex(i).path;
                        string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                        if (string.IsNullOrWhiteSpace(scenePath)) scenePath = "NoName";
                        buildSceneNames[i] = $"{i} {scenePath}";
                    }
                }
                GUILayout.Label($"启动场景: {ScreenSetting.Instance.startUpScene}");
                GUILayout.Label($"或者选择一个场景:{buildSceneNames.Length}");
                for (int i = 0; i < buildSceneNames.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(buildSceneNames[i]);
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button($"{i}、{name}"))
                        {
                            SceneManager.LoadScene(i);
                        }
                        if (GUILayout.Button("设置为启动场景", GUILayout.ExpandWidth(false)))
                        {
                            ScreenSetting.Instance.autoSwitchSceneIndexRuntimeSpecify = i;
                        }
                    }
                }
            }

            //绘制PersistentDataPath管理的UI
            private void DrawPersistentDataModule()
            {
                // if (GUILayout.Button("压缩测试"))
                // {
                //     //解决中文乱码
                //     ICSharpCode.SharpZipLib.Zip.ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                //     new FastZip().CreateZip(@"C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\1.zip", @"C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\新建文件夹\", true, null);
                // }
                // if (GUILayout.Button("解压测试"))
                // {
                //     new FastZip().ExtractZip("C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\1.zip", "C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\解压", null);
                // }
                // if (GUILayout.Button("Download test"))
                // {
                //     Debugger.Instance.StartCoroutine(FeiShuFileBakcup.DownloadFile("SS4zbPEvvodjDWx7bKEck1r3nYf", "1.txt"));
                // }

                GUILayout.Label("PersistentDataPath管理:");
                float buttonWidth = 80;
                GUILayout.BeginVertical("Box");
                {
                    GUILayout.Label("简易文件管理器：");

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("上一级", GUILayout.Width(buttonWidth)))
                            currentPath = Path.GetDirectoryName(currentPath);
                        if (GUILayout.Button("Home", GUILayout.Width(buttonWidth)))
                            currentPath = Application.persistentDataPath;
                        if (GUILayout.Button("使用Explorer打开目录"))
                            System.Diagnostics.Process.Start(currentPath);

                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    //显示当前路径
                    GUILayout.BeginVertical("Box");
                    {
                        GUILayout.Label(currentPath);
                    }
                    GUILayout.EndVertical();

                    // 列出所有目录
                    string[] dirs = Directory.GetDirectories(currentPath);
                    foreach (string dir in dirs)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("目录", GUILayout.Width(buttonWidth)))
                                currentPath = dir;

                            GUILayout.Label(dir.Replace(currentPath, ""));

                            if (GUILayout.Button("压缩", GUILayout.Width(buttonWidth)))
                            {
                                //解决中文乱码
                                // ICSharpCode.SharpZipLib.Zip.ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                                new FastZip().CreateZip(dir + ".zip", dir, true, null);
                            }

                            if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                                Directory.Delete(dir, true);
                        }
                        GUILayout.EndHorizontal();
                    }

                    //列出所有文件
                    {
                        string[] files = Directory.GetFiles(currentPath);
                        foreach (string file in files)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("", GUILayout.Width(buttonWidth));
                                GUILayout.Label(file.Replace(currentPath, ""));
                                bool isZipFile = file.ToLower().EndsWith(".zip");

                                if (!isZipFile && GUILayout.Button("OpenAsText", GUILayout.Width(buttonWidth)))
                                    tempText = File.ReadAllText(file);


                                if (GUILayout.Button("上传", GUILayout.Width(buttonWidth)))
                                {
                                    Debugger.Instance.StartCoroutine(FeiShuFileBackup.UploadFile(file, FeiShuFileBackup.rootFolderToken));
                                }

                                if (isZipFile)
                                {
                                    if (GUILayout.Button("解压到此处", GUILayout.Width(buttonWidth)))
                                    {
                                        //解决中文乱码
                                        // ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                                        new FastZip().ExtractZip(file, currentPath, null);
                                    }
                                    if (GUILayout.Button("解压到目录", GUILayout.Width(buttonWidth)))
                                    {
                                        //解决中文乱码
                                        // ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                                        string zipNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                                        string targetPath = Path.Combine(currentPath, zipNameWithoutExtension);
                                        new FastZip().ExtractZip(file, targetPath, null);
                                    }
                                }
                                else
                                {
                                    GUILayout.Label("", GUILayout.Width(buttonWidth));
                                }

                                if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                                    File.Delete(file);
                            }
                            GUILayout.EndHorizontal();
                        }

                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("显示云端内容"))
                        {
                            Debugger.Instance.StartCoroutine(
                                FeiShuFileBackup.ListFiles(FeiShuFileBackup.rootFolderToken, feiShuCloudFiles));
                        }
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    //显示云端文件
                    foreach (var feiShuCloudFile in feiShuCloudFiles)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            string fileName = feiShuCloudFile.Value;
                            string fileToken = feiShuCloudFile.Key;

                            GUILayout.Label(fileName);

                            if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                                Debugger.Instance.StartCoroutine(FeiShuFileBackup.DeleteFile(fileToken));

                            if (GUILayout.Button("下载", GUILayout.Width(buttonWidth)))
                            {
                                string savePath = Path.Combine(currentPath, fileName);
                                Debugger.Instance.StartCoroutine(FeiShuFileBackup.DownloadFile(fileToken, savePath));
                            }
                        }
                        GUILayout.EndHorizontal();
                    }


                    //显示以文本方式打开的文件内容
                    if (!string.IsNullOrWhiteSpace(tempText))
                    {
                        if (GUILayout.Button("Close"))
                            tempText = "";
                        GUILayout.Label(tempText.Trim());
                    }
                }
                GUILayout.EndVertical();
            }

            #region Runtime Hierarchy
            // ========== Runtime Hierarchy 模块 ==========
            // 简单的运行时层级视图：树状显示所有加载场景的根对象与其子级，提供激活/反激活 Toggle
            private void DrawRuntimeHierarchyModule()
            {
                GUILayout.Label("Runtime Hierarchy:");

                // 顶栏：全部折叠 / 展开所有根节点（仅一层，避免一键展开几千个对象导致 IMGUI 卡顿）
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("全部折叠", GUILayout.Width(80)))
                    {
                        _hierarchyExpanded.Clear();
                        Debug.Log("[Debugger.Hierarchy] 清除所有展开状态");
                    }

                    if (GUILayout.Button("展开根节点", GUILayout.Width(100)))
                    {
                        ExpandAllRoots();
                    }

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("Box");
                {
                    // 1) 遍历所有已加载场景
                    int sceneCount = SceneManager.sceneCount;
                    for (int i = 0; i < sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (!scene.isLoaded) continue;

                        GUILayout.Label($"Scene: {scene.name} (roots: {scene.rootCount})");

                        GameObject[] roots = scene.GetRootGameObjects();
                        for (int r = 0; r < roots.Length; r++)
                        {
                            DrawGameObjectNode(roots[r].transform, 0);
                        }
                    }

                    // 2) DontDestroyOnLoad 场景：通过 probe GameObject 拿到其 Scene 引用
                    Scene ddolScene = GetOrCreateDdolScene();
                    if (ddolScene.IsValid())
                    {
                        // probe 自己也算 1 个根，这里显示时减掉
                        int ddolRootCount = Mathf.Max(0, ddolScene.rootCount - 1);
                        GUILayout.Label($"Scene: {ddolScene.name} (roots: {ddolRootCount})");

                        GameObject[] ddolRoots = ddolScene.GetRootGameObjects();
                        for (int r = 0; r < ddolRoots.Length; r++)
                        {
                            // 跳过探测对象自身
                            if (ReferenceEquals(ddolRoots[r], _ddolProbe)) continue;
                            DrawGameObjectNode(ddolRoots[r].transform, 0);
                        }
                    }
                }
                GUILayout.EndVertical();

                // 选中节点的详情面板（在层级树下方独立绘制，便于在树过长时仍能看到）
                DrawSelectedTransformInfo();
            }

            // 绘制当前选中 Transform 的详细信息：路径、世界/本地 PRS、parent、childCount 等
            // 仅显示信息，不做编辑；如需调试时改值，再后续拓展可编辑字段
            private void DrawSelectedTransformInfo()
            {
                // 选中对象被销毁后 Unity 的 == 重载会判定为 null，这里直接清空引用以避免脏数据
                if (_selectedTransform == null)
                {
                    _selectedTransform = null;
                    return;
                }

                GUILayout.Space(6);
                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"已选中: {_selectedTransform.gameObject.name}");
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("取消选择", GUILayout.Width(80)))
                        {
                            Debug.Log($"[Debugger.Hierarchy] 取消选择: {GetHierarchyPath(_selectedTransform)}");
                            _selectedTransform = null;
                        }
                    }
                    GUILayout.EndHorizontal();

                    // 再次校验：上面的"取消选择"按钮可能已清空引用
                    if (_selectedTransform == null)
                    {
                        GUILayout.EndVertical();
                        return;
                    }

                    GameObject go = _selectedTransform.gameObject;

                    GUILayout.Label($"Path: {GetHierarchyPath(_selectedTransform)}");
                    GUILayout.Label(
                        $"InstanceID: {_selectedTransform.GetInstanceID()}   " +
                        $"Layer: {LayerMask.LayerToName(go.layer)}({go.layer})   " +
                        $"Tag: {go.tag}   " +
                        $"Active(self/inHierarchy): {go.activeSelf}/{go.activeInHierarchy}");

                    GUILayout.Label("World:");
                    GUILayout.Label($"  Position  : {FormatVec3(_selectedTransform.position)}");
                    GUILayout.Label($"  EulerAngles: {FormatVec3(_selectedTransform.eulerAngles)}");
                    GUILayout.Label($"  LossyScale : {FormatVec3(_selectedTransform.lossyScale)}");

                    GUILayout.Label("Local:");
                    GUILayout.Label($"  Position  : {FormatVec3(_selectedTransform.localPosition)}");
                    GUILayout.Label($"  EulerAngles: {FormatVec3(_selectedTransform.localEulerAngles)}");
                    GUILayout.Label($"  Scale     : {FormatVec3(_selectedTransform.localScale)}");

                    string parentName = _selectedTransform.parent == null
                        ? "<none>"
                        : _selectedTransform.parent.name;
                    GUILayout.Label($"Parent: {parentName}    ChildCount: {_selectedTransform.childCount}");
                }
                GUILayout.EndVertical();
            }

            // Vector3 格式化辅助：固定 3 位小数，便于阅读
            private static string FormatVec3(Vector3 v)
            {
                return $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
            }

            // 绘制单个 GameObject 节点：缩进 + 折叠按钮 + 激活Toggle + 名称
            // 展开时递归绘制子级；通过缩进列对齐保持层级可读性
            private void DrawGameObjectNode(Transform t, int depth)
            {
                if (t == null) return;

                int instanceId = t.GetInstanceID();
                int childCount = t.childCount;
                // 默认折叠：字典中没有键时返回 false
                bool expanded = _hierarchyExpanded.TryGetValue(instanceId, out bool v) && v;

                GUILayout.BeginHorizontal();
                {
                    // 缩进
                    if (depth > 0)
                        GUILayout.Space(depth * HIERARCHY_INDENT);

                    // 折叠按钮：有子级才显示，否则用同宽 Space 占位以保持列对齐
                    if (childCount > 0)
                    {
                        // 用 > 与 v 代替三角符号，避免某些字体缺字形导致显示为方块
                        string foldoutLabel = expanded ? "v" : ">";
                        if (GUILayout.Button(foldoutLabel, GUILayout.Width(HIERARCHY_FOLDOUT_WIDTH)))
                        {
                            _hierarchyExpanded[instanceId] = !expanded;
                            expanded = !expanded;
                        }
                    }
                    else
                    {
                        GUILayout.Space(24);
                    }

                    // 激活 Toggle：直接绑定 gameObject.activeSelf
                    // 注意：父级被禁用时子级在 Editor 中显示为灰色，这里 Toggle 仍可点击，行为符合 Editor Hierarchy
                    bool curActive = t.gameObject.activeSelf;
                    bool curActiveInHierarchy = t.gameObject.activeInHierarchy;
                    bool newActive = GUILayout.Toggle(curActive, GUIContent.none, GUILayout.Width(18));
                    if (newActive != curActive)
                    {
                        t.gameObject.SetActive(newActive);
                        Debug.Log($"[Debugger.Hierarchy] SetActive: {GetHierarchyPath(t)} -> {newActive}");
                    }

                    // 名称：点击可选中；选中时用青色高亮，未激活时灰色，二者叠加时优先显示选中
                    // 这里用 label 样式的 Button 模拟"可点击的 Label"，保留视觉上紧凑的层级外观
                    bool isSelected = ReferenceEquals(_selectedTransform, t);
                    if (isSelected)
                        GUI.color = Color.cyan;
                    else
                        GUI.color = curActiveInHierarchy ? Color.white : Color.gray;

                    if (GUILayout.Button(t.gameObject.name, GUI.skin.label))
                    {
                        // 再次点击同一行可视作"取消选择"，便于隐藏下方详情面板
                        if (isSelected)
                        {
                            _selectedTransform = null;
                            Debug.Log($"[Debugger.Hierarchy] 取消选择: {GetHierarchyPath(t)}");
                        }
                        else
                        {
                            _selectedTransform = t;
                            Debug.Log($"[Debugger.Hierarchy] 选中: {GetHierarchyPath(t)}");
                        }
                    }
                    GUI.color = Color.white;

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                // 展开时递归子级
                if (expanded && childCount > 0)
                {
                    for (int i = 0; i < childCount; i++)
                    {
                        DrawGameObjectNode(t.GetChild(i), depth + 1);
                    }
                }
            }

            // 展开所有场景的根节点一层（不递归子级，避免一次性渲染过多）
            private void ExpandAllRoots()
            {
                int total = 0;
                int sceneCount = SceneManager.sceneCount;
                for (int i = 0; i < sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    GameObject[] roots = scene.GetRootGameObjects();
                    for (int r = 0; r < roots.Length; r++)
                    {
                        _hierarchyExpanded[roots[r].transform.GetInstanceID()] = true;
                        total++;
                    }
                }

                Scene ddolScene = GetOrCreateDdolScene();
                if (ddolScene.IsValid())
                {
                    GameObject[] ddolRoots = ddolScene.GetRootGameObjects();
                    for (int r = 0; r < ddolRoots.Length; r++)
                    {
                        if (ReferenceEquals(ddolRoots[r], _ddolProbe)) continue;
                        _hierarchyExpanded[ddolRoots[r].transform.GetInstanceID()] = true;
                        total++;
                    }
                }

                Debug.Log($"[Debugger.Hierarchy] 展开根节点完成，共 {total} 个");
            }

            // 创建/复用 DontDestroyOnLoad 探测对象，用以拿到该场景引用
            // Unity 没有直接 API 拿 DDOL 场景，必须先有一个对象在该场景里
            private Scene GetOrCreateDdolScene()
            {
                if (_ddolProbe == null)
                {
                    _ddolProbe = new GameObject("[Debugger.DDOL.Probe]");
                    _ddolProbe.hideFlags = HideFlags.HideAndDontSave;
                    GameObject.DontDestroyOnLoad(_ddolProbe);
                }
                return _ddolProbe.scene;
            }

            // 仅用于日志输出：生成形如 "Root/Child/Leaf" 的路径，便于排查 SetActive 是谁触发的
            private static string GetHierarchyPath(Transform t)
            {
                if (t == null) return string.Empty;
                if (t.parent == null) return t.name;
                return GetHierarchyPath(t.parent) + "/" + t.name;
            }
            #endregion
        }
    }
}