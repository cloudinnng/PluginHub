using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PluginHub.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace PluginHub.Editor
{
    public partial class BuildModule : PluginHubModuleBase, IPreprocessBuildWithReport
    {
        #region 模块信息

        public override ModuleType moduleType => ModuleType.Shortcut;
        public override string moduleName => "构建";
        public override string moduleDescription => "一键Build,便于快速迭代";

        private static float titleWidth = 70;

        #endregion

        #region 属性配置

        //项目文件夹名称（项目最顶层文件夹）
        //eg: D:/UnityProject/TopWellCustomPattern/Assets -> TopWellCustomPattern
        private static string projectFolderName
        {
            get
            {
                string path = Application.dataPath.Replace("/Assets", "");
                return Path.GetFileName(path);
            }
        }

        //是否是开发构建
        private static bool devBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_devBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_devBuild", value); }
        }

        //是否构建前删除旧的构建目录
        private static bool deleteOldBuildBeforeBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_deleteOldBuildBeforeBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_deleteOldBuildBeforeBuild", value); }
        }

        //是否构建前清空编辑器中的StreamingAssets文件夹
        private static bool clearStreamingAssetsBeforeBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_clearStreamingAssetsBeforeBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_clearStreamingAssetsBeforeBuild", value); }
        }

        //PC平台场景构建时,构建项目时用于exe执行文件的名称和构建目录名，如果为空，则使用项目文件夹名称
        // eg: 溪洛渡水电站机电设备三维可视化平台
        // eg: XLDHydropowerStation
        private static string projectBuildName
        {
            get
            {
                string value = PluginHubConfig.ReadConfig("BuildModule", "projectBuildName", "");
                if (string.IsNullOrWhiteSpace(value))
                    return projectFolderName;
                else
                    return value;
            }
            set
            {
                PluginHubConfig.WriteConfig("BuildModule", "projectBuildName", value);
            }
        }

        //实际的构建名
        private static string realProjectBuildName => devBuild ? $"{projectBuildName}_Dev" : projectBuildName;

        //PC平台场景构建时,用于exe执行文件的名称和构建目录名，如果为空，则使用场景名称
        // eg: 00.MainScene
        private static string sceneBuildName
        {
            get
            {
                string sceneName = EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrWhiteSpace(sceneName))
                    return "";

                string value = PluginHubConfig.ReadConfig($"BuildModule_{sceneName}", "sceneBuildName", "");
                if (string.IsNullOrWhiteSpace(value))
                    return sceneName;
                else
                    return value;
            }
            set
            {
                string sceneName = EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrWhiteSpace(sceneName))
                    return;

                PluginHubConfig.WriteConfig($"BuildModule_{sceneName}", "sceneBuildName", value);
            }
        }

        //使用短小的构建路径
        private static bool iosUseShortBuildPath
        {
            get
            {
                return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_iosUseShortBuildPath",
                    false);
            }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_iosUseShortBuildPath", value); }
        }

        //更新信息
        private string updateInfo
        {
            get { return EditorPrefs.GetString($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_updateInfo", ""); }
            set { EditorPrefs.SetString($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_updateInfo", value); }
        }

        //构建备注
        private static string buildNote
        {
            get => EditorPrefs.GetString($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_BuildNote", "");
            set => EditorPrefs.SetString($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_BuildNote", value);
        }

        #endregion

        #region 构建预处理

        public int callbackOrder => -999999;

        public static string BuildInfoFilePath => Path.Combine(Application.streamingAssetsPath, "BuildInfo.txt");

        //构建预处理
        public void OnPreprocessBuild(BuildReport report)
        {
            CreateStreamingAssetsIfNotExists();
            WriteBuildInfo();
            ClearStreamingAssetsIfNeeded();
        }

        private void CreateStreamingAssetsIfNotExists()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        private void WriteBuildInfo()
        {
            Debug.Log($"[BuildModule] Write build info to {BuildInfoFilePath}");
            Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoFilePath));
            INIParser iniParser = new INIParser();
            iniParser.Open(BuildInfoFilePath);
            iniParser.WriteValue("BuildInfo", "UpdateInfo", updateInfo.Replace("\n", "\\n").Trim());
            iniParser.WriteValue("BuildInfo", "BuildTime", CurrentDateTimeString());
            iniParser.Close();
        }

        private void ClearStreamingAssetsIfNeeded()
        {
            if (clearStreamingAssetsBeforeBuild)
            {
                if (EditorUtility.DisplayDialog("清空StreamingAssets", "您选择了 clearStreamingAssetsBeforeBuild 是否删除 StreamingAssets 文件夹下的所有文件？", "是", "否"))
                {
                    string path = Application.streamingAssetsPath;
                    string[] files = System.IO.Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        System.IO.File.Delete(file);
                    }
                }
            }
        }
        #endregion

        #region 构建后处理

        [PostProcessBuild]
        public static void PostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                IncrementIOSBuildNumber();
            }
            else if (buildTarget == BuildTarget.StandaloneWindows64)
            {
                IncrementPCVersionNumber();// 增加版本号
                CopyDaemonRunBatToWindowsBuildDirectory(pathToBuiltProject);// 复制 daemon-run.bat 到构建目录
                ExecutePostCopyFolders(buildTarget, pathToBuiltProject);// 执行构建后复制文件夹到构建目录
            }
        }

        /// <summary>
        /// Windows 构建完成后，将 daemon-run.bat 复制到构建目录（exe 同级目录）。
        /// 优先通过 Package Manager API 解析包真实路径，再回退到多个候选路径，
        /// 兼容插件放在 Packages/Assets/工程根目录等不同场景。
        /// </summary>
        private static void CopyDaemonRunBatToWindowsBuildDirectory(string pathToBuiltProject)
        {
            if (string.IsNullOrWhiteSpace(pathToBuiltProject))
            {
                Debug.LogWarning("[BuildModule] 跳过复制 daemon-run.bat：pathToBuiltProject 为空。");
                return;
            }

            string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
            if (string.IsNullOrWhiteSpace(buildDirectory) || !Directory.Exists(buildDirectory))
            {
                Debug.LogWarning($"[BuildModule] 跳过复制 daemon-run.bat：构建目录无效，pathToBuiltProject={pathToBuiltProject}");
                return;
            }

            string scriptPath = PluginHubRuntime.ResolveRelativePath("Plugins/daemon-run.bat");
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                Debug.LogWarning("[BuildModule] 跳过复制 daemon-run.bat：未找到源文件。已检查路径：" + scriptPath);
                return;
            }
            string targetFilePath = Path.Combine(buildDirectory, "daemon-run.bat");
            File.Copy(scriptPath, targetFilePath, true);
            Debug.Log($"[BuildModule] 已复制 daemon-run.bat 到构建目录：{targetFilePath}");
        }

        private static void IncrementIOSBuildNumber()
        {
            string oldBuildNumber = PlayerSettings.iOS.buildNumber;
            PlayerSettings.iOS.buildNumber = (int.Parse(PlayerSettings.iOS.buildNumber) + 1).ToString();
            Debug.Log($"Build ID从{oldBuildNumber}自增到{PlayerSettings.iOS.buildNumber}");
        }

        private static void IncrementPCVersionNumber()
        {
            string oldVersion = PlayerSettings.bundleVersion;
            int lastIndex = oldVersion.LastIndexOf('.');
            string majorVersion = oldVersion.Substring(0, lastIndex);
            string minorVersion = oldVersion.Substring(lastIndex + 1);
            minorVersion = (int.Parse(minorVersion) + 1).ToString();
            PlayerSettings.bundleVersion = $"{majorVersion}.{minorVersion}";
            Debug.Log($"版本号从{oldVersion}自增到{PlayerSettings.bundleVersion}");
        }

        #endregion

        #region 界面绘制

        protected override void DrawGuiContent()
        {
            DrawBuildInfoSection();
            DrawBuildOptionsSection();
            DrawQuickBuildSection();
            DrawBuildLibrarySection();
            DrawBuildNoteSection();
        }

        private void DrawBuildInfoSection()
        {
            DrawSplitLine("构建信息");

            DrawItem("公司名称:", PlayerSettings.companyName);
            DrawItem("产品名称:", PlayerSettings.productName);
            DrawItem("版本:", PlayerSettings.bundleVersion);
            DrawVersionControl();
            DrawItem("默认屏幕:", PlayerSettings.defaultScreenWidth + " x " + PlayerSettings.defaultScreenHeight);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("更新内容:", GUILayout.Width(titleWidth));
                updateInfo = GUILayout.TextArea(updateInfo);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawBuildOptionsSection()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("更多选项:", GUILayout.Width(titleWidth));
                GUILayout.BeginVertical();
                {
                    devBuild = GUILayout.Toggle(devBuild, new GUIContent("开发构建"));
                    deleteOldBuildBeforeBuild = GUILayout.Toggle(deleteOldBuildBeforeBuild, new GUIContent("构建前删除旧的构建", "虽然构建会将之前的覆盖,但有时动态生成的多余文件可能仍会被保留.使用此选项在构建前先删除旧构建文件夹以确保干净。"));
                    clearStreamingAssetsBeforeBuild = GUILayout.Toggle(clearStreamingAssetsBeforeBuild, new GUIContent("构建前清空StreamingAssets"));
                    enablePostCopy = GUILayout.Toggle(enablePostCopy, new GUIContent("构建后复制文件夹到构建目录"));
                    if (enablePostCopy)
                    {
                        DrawPostCopyFolderPathsUI();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawQuickBuildSection()
        {
            DrawSplitLine("快捷构建");

            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    DrawPCBuildButtons();
                    break;
                case BuildTarget.iOS:
                    DrawIOSBuildButtons();
                    break;
                case BuildTarget.Android:
                    DrawAndroidBuildButtons();
                    break;
                case BuildTarget.WebGL:
                    DrawWebGLBuildButtons();
                    break;
                case BuildTarget.StandaloneOSX:
                    DrawMacOSBuildButtons();
                    break;
            }
        }

        private void DrawBuildLibrarySection()
        {
            DrawSplitLine("构建库");
            DrawBuildLibrary();
        }

        private void DrawBuildNoteSection()
        {
            DrawSplitLine("构建备注:");
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (DrawIconBtn("d_TreeEditor.Duplicate", "复制备注"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        GUIUtility.systemCopyBuffer = buildNote;
                        Debug.Log("构建备注已复制到剪贴板");
                    };
                }
            }
            GUILayout.EndHorizontal();
            buildNote = GUILayout.TextArea(buildNote);
        }

        #region 平台构建按钮

        private void DrawPCBuildButtons()
        {
            //PC平台快捷构建按钮
            using(new GUILayout.VerticalScope("Box"))
            {
                DrawPlatformHeader("BuildSettings.Standalone On", BuildTarget.StandaloneWindows64.ToString());
                DrawPCProjectBuildSection();
                DrawPCSceneBuildSection();
            }
        }

        private void DrawPlatformHeader(string icon, string platformName)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(PluginHubEditor.IconContent(icon));
                GUILayout.Label($"平台 : {platformName}");
            }
            GUILayout.EndHorizontal();
        }

        private void DrawPCProjectBuildSection()
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("项目构建:");
                projectBuildName = EditorGUILayout.TextField("项目构建名称:", projectBuildName);
                GUILayout.BeginHorizontal();
                {
                    string path = CurrProjectBuildFullPath();
                    if (GUILayout.Button(PluginHubEditor.GuiContent("构建项目", $"将构建到{path}")))
                    {
                        EditorApplication.delayCall += BuildStandaloneProject;
                    }

                    if (GUILayout.Button("构建并运行", GUILayout.ExpandWidth(false)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            BuildStandaloneProject();
                            ExecuteExe(path);
                        };
                    }
                    DrawIconBtnOpenFolder(path);
                    DrawRunButton(path);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawPCSceneBuildSection()
        {
            using(new GUILayout.VerticalScope("Box"))
            {
                GUILayout.Label("场景构建:");
                sceneBuildName = EditorGUILayout.TextField("场景构建名称:", sceneBuildName);

                using(new GUILayout.HorizontalScope())
                {
                    string path = CurrSceneBuildFullPath();
                    if (GUILayout.Button(PluginHubEditor.GuiContent("构建当前场景", $"将会直接构建到{path}。")))
                    {
                        EditorApplication.delayCall += () => BuildStandaloneCurrScene(false);
                    }

                    if (GUILayout.Button(
                            PluginHubEditor.GuiContent("仅构建当前场景", $"程序将先在构建设置中取消激活其它已添加的场景\n然后构建到{path}。"),
                            GUILayout.ExpandWidth(false)))
                    {
                        EditorApplication.delayCall += () => BuildStandaloneCurrScene(true);
                    }

                    DrawIconBtnOpenFolder(path);
                    DrawRunButton(path);
                }
            }
        }

        private void DrawRunButton(string exePath)
        {
            GUI.enabled = File.Exists(exePath);
            if (GUILayout.Button("运行", GUILayout.ExpandWidth(false)))
            {
                ExecuteExe(exePath);
            }
            GUI.enabled = true;
        }

        public void DrawIOSBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                DrawPlatformHeader("BuildSettings.iPhone On", BuildTarget.iOS.ToString());
                DrawIOSBuildIDSection();
                DrawIOSPackageIDSection();
                DrawIOSBuildButton();
            }
            GUILayout.EndVertical();
        }

        private void DrawIOSBuildIDSection()
        {
            GUILayout.BeginHorizontal();
            {
                DrawItem("Build ID:", PlayerSettings.iOS.buildNumber);
                if (GUILayout.Button("归零构建ID", GUILayout.ExpandWidth(false)))
                {
                    PlayerSettings.iOS.buildNumber = "0";
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawIOSPackageIDSection()
        {
            GUILayout.BeginHorizontal();
            {
                DrawItem("Package ID:", PlayerSettings.applicationIdentifier);
                GUILayout.FlexibleSpace();

                GUI.enabled = Application.platform == RuntimePlatform.WindowsEditor;
                iosUseShortBuildPath = GUILayout.Toggle(iosUseShortBuildPath,
                    new GUIContent("使用短小的构建路径", "当由于路径过长导致构建失败时，可以尝试勾选此选项。"));
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawIOSBuildButton()
        {
            GUILayout.BeginHorizontal();
            {
                string fullPath = Application.dataPath;
                fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
                fullPath = Path.Combine(fullPath, $"Build/IOS/{PlayerSettings.applicationIdentifier}_xcode");
                fullPath = fullPath.Replace('/', '\\');
                string path = fullPath;
                if (iosUseShortBuildPath)
                    path = $@"D:\Build_IOS\{Application.productName}\";
                // if macos
                if (Application.platform == RuntimePlatform.OSXEditor)
                    path = $"Build/IOS/{PlayerSettings.applicationIdentifier}_xcode";

                if (GUILayout.Button(PluginHubEditor.GuiContent("构建 IOS 项目", $"将构建到{path}"), GUILayout.Height(PluginHubEditor.NormalBtnHeight)))
                {
                    EditorApplication.delayCall += () => BuildIOS(path);
                }

                // 快捷打开xCode项目的icon按钮
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    string xCodePath = Path.Combine(fullPath, $"Unity-iPhone.xcodeproj");
                    xCodePath = xCodePath.Replace('\\', '/');
                    bool exist = Directory.Exists(xCodePath);
                    GUI.enabled = exist;
                    if (DrawIconBtn("BuildSettings.iPhone On@2x", $"打开xCode项目{xCodePath}"))
                    {
                        EditorApplication.delayCall += () => OpenFile(xCodePath);
                    }
                    GUI.enabled = true;
                }

                DrawIconBtnOpenFolder(path);
                DrawIconBtnCopy(path);
            }
            GUILayout.EndHorizontal();
        }

        public void DrawAndroidBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                DrawPlatformHeader("BuildSettings.Android On", BuildTarget.Android.ToString());
                DrawAndroidBuildButton();
            }
            GUILayout.EndVertical();
        }

        private void DrawAndroidBuildButton()
        {
            GUILayout.BeginHorizontal();
            {
                string fullPath = Application.dataPath;
                fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
                fullPath = Path.Combine(fullPath, $"Build/Android/");
                fullPath = fullPath.Replace('/', '\\');
                string path = fullPath;
                if (GUILayout.Button(PluginHubEditor.GuiContent("构建 Android 项目", $"将构建到{path}")))
                {
                    EditorApplication.delayCall += () => BuildAndroid($"Build/Android/{PlayerSettings.applicationIdentifier}.apk");
                }

                DrawIconBtnOpenFolder(path);
            }
            GUILayout.EndHorizontal();
        }

        public void DrawWebGLBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                DrawPlatformHeader("BuildSettings.WebGL On", BuildTarget.WebGL.ToString());
                DrawWebGLBuildButton();
            }
            GUILayout.EndVertical();
        }

        private void DrawWebGLBuildButton()
        {
            GUILayout.BeginHorizontal();
            {
                string fullPath = Application.dataPath;
                fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
                fullPath = Path.Combine(fullPath, $"Build/WebGL/");
                fullPath = fullPath.Replace('/', '\\');
                string path = fullPath;
                if (GUILayout.Button(PluginHubEditor.GuiContent("构建 WebGL 项目", $"将构建到{path}")))
                {
                    EditorApplication.delayCall += () => BuildWebGL($@"Build/WebGL/");
                }

                DrawIconBtnOpenFolder(path);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawMacOSBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                DrawPlatformHeader("BuildSettings.Standalone On", BuildTarget.StandaloneOSX.ToString());
                DrawMacOSBuildButton();
            }
            GUILayout.EndVertical();
        }

        private void DrawMacOSBuildButton()
        {
            GUILayout.BeginHorizontal();
            {
                string fullPath = Application.dataPath;
                fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
                fullPath = Path.Combine(fullPath, $"Build/MacOS/{PlayerSettings.productName}.app");
                string path = fullPath;
                if (GUILayout.Button(PluginHubEditor.GuiContent("构建 MacOS 项目", $"将构建到{path}")))
                {
                    EditorApplication.delayCall += () => BuildMacOS($@"Build/MacOS/{PlayerSettings.productName}.app");
                }
                DrawIconBtnOpenFolder(path);
            }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region 构建库

        private void DrawBuildLibrary()
        {
            //构建库
            GUILayout.BeginVertical("Box");
            {
                DrawBuildLibraryHeader();
                DrawBaiduPCSSection();
                DrawBuildsSection();
                DrawZipFilesSection();
            }
            GUILayout.EndVertical();
        }

        private void DrawBuildsSection()
        {
            string buildPath = Path.Combine(Application.dataPath, "../Build");
            if (!Directory.Exists(buildPath))
                return;

            GUILayout.Label("构建：");
            string[] directories = Directory.GetDirectories(buildPath);

            // 按构建时间排序，找出最新
            var directoriesWithTime = directories
                .Select(d => new { Path = d, Time = GetBuildTime(d) })
                .OrderBy(x => x.Time)
                .ToList();

            DateTime latestBuildTime = directoriesWithTime.LastOrDefault()?.Time ?? DateTime.MinValue;

            for (int i = 0; i < directories.Length; i++)
            {
                DateTime buildTime = GetBuildTime(directories[i]);
                bool isLatest = buildTime == latestBuildTime && latestBuildTime != DateTime.MinValue;
                DrawBuildItem(directories[i], i, isLatest);
            }
        }

        private void DrawBuildLibraryHeader()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(PluginHubEditor.IconContent("VerticalLayoutGroup Icon"));
                GUILayout.FlexibleSpace();
                DrawIconBtnOpenFolder(Path.Combine(Application.dataPath, "../Build/"));
            }
            GUILayout.EndHorizontal();
        }


        private void DrawBuildItem(string directory, int index, bool isLatest)
        {
            string folderName = Path.GetFileName(directory);
            string executeFullpath = Path.Combine(directory, $"{folderName}.exe");

            GUILayout.BeginHorizontal();
            {
                DateTime buildTime = GetBuildTime(directory);
                string spendTimeStr = GetTimeSpanFromDateTime(buildTime);
                string labelText = $"{index}. {folderName} ({spendTimeStr})";

                if (isLatest)
                {
                    Color oldColor = GUI.color;
                    GUI.color = new Color(0.3f, 1f, 0.3f);
                    GUILayout.Label(labelText);
                    GUI.color = oldColor;
                }
                else
                {
                    GUILayout.Label(labelText);
                }

                //打开StreamingAssets文件夹按钮
                string streamingAssetsPath = Path.Combine(directory, $"{folderName}_Data/StreamingAssets/");
                DrawIconBtnOpenFolder(streamingAssetsPath, "SA");

                //打开文件夹按钮
                DrawIconBtnOpenFolder(directory);

                //压缩这个构建到当前目录
                if (GUILayout.Button("zip", GUILayout.ExpandWidth(false)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        string zipFilePath = ZipBuildDirectory(directory);
                        WinClipboard.CopyFiles(new[] { zipFilePath });
                        Debug.Log($"已复制: {zipFilePath}");
                    };
                }

                //运行按钮
                GUI.enabled = File.Exists(executeFullpath);
                if (GUILayout.Button("运行", GUILayout.ExpandWidth(false)))
                {
                    ExecuteExe(executeFullpath);
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private DateTime GetBuildTime(string buildDirectory)
        {
            string folderName = Path.GetFileName(buildDirectory);
            string streamingAssetsPath = Path.Combine(buildDirectory, $"{folderName}_Data/StreamingAssets/");
            string buildInfoFilePath = Path.Combine(streamingAssetsPath, "BuildInfo.txt");
            if (!File.Exists(buildInfoFilePath))
                return DateTime.MinValue;
            INIParser iniParser = new INIParser();
            iniParser.Open(buildInfoFilePath);
            string buildTime = iniParser.ReadValue("BuildInfo", "BuildTime", DateTime.MinValue.ToString("yyyyMMddHHmmss"));
            iniParser.Close();
            return GetDateTimeFromFileName(Path.GetFileName(buildTime));
        }

        private string ZipBuildDirectory(string directory)
        {
            string timeStr = CurrentDateTimeString();
            string destZipPath = Path.Combine(directory + $"_{timeStr}.zip");
            CreateZip(directory, destZipPath);
            return destZipPath;
        }

        private void DrawZipFilesSection()
        {
            string buildPath = Path.Combine(Application.dataPath, "../Build");
            if (!Directory.Exists(buildPath))
                return;
            string[] zipFiles = Directory.GetFiles(buildPath, "*.zip");

            if (zipFiles.Length == 0)
                return;

            GUILayout.Label("压缩文件：");

            // 按文件名前缀的时间戳排序，找出最新
            var zipFilesWithTime = zipFiles
                .Select(f => new { Path = f, Time = GetDateTimeFromFileName(Path.GetFileName(f)) })
                .OrderBy(x => x.Time)
                .ToList();

            DateTime latestZipTime = zipFilesWithTime.LastOrDefault()?.Time ?? DateTime.MinValue;

            for (int i = 0; i < zipFiles.Length; i++)
            {
                DateTime zipTime = GetDateTimeFromFileName(Path.GetFileName(zipFiles[i]));
                bool isLatest = zipTime == latestZipTime && latestZipTime != DateTime.MinValue;
                bool isProjectZipFile = zipFiles[i].Contains(PlayerSettings.productName);
                DrawZipFileItem(zipFiles[i], i, isLatest, isProjectZipFile);
            }
        }

        private void DrawZipFileItem(string zipFile, int index, bool isLatest, bool isProjectZipFile)
        {
            zipFile = zipFile.Replace("/", "\\");
            zipFile = zipFile.Replace(@"Assets\..\", "");
            string zipFileName = Path.GetFileName(zipFile);

            GUILayout.BeginHorizontal();
            {
                string timeSpanStr = GetTimeSpanFromDateTime(GetDateTimeFromFileName(zipFileName));
                string title = $"{index}. {zipFileName} ({timeSpanStr})";

                if (isLatest)
                {
                    Color oldColor = GUI.color;
                    GUI.color = new Color(0.3f, 1f, 0.3f);
                    GUILayout.Label(PluginHubEditor.GuiContent(title));
                    GUI.color = oldColor;
                }
                else if (isProjectZipFile)
                {
                    Color oldColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 0.3f);
                    GUILayout.Label(PluginHubEditor.GuiContent(title));
                    GUI.color = oldColor;
                }
                else
                {
                    GUILayout.Label(PluginHubEditor.GuiContent(title));
                }

                // 删除按钮
                if (DrawIconBtn("P4_DeletedLocal", $"删除文件"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorUtility.DisplayDialog("提示", $"是否删除文件: {zipFile}", "是", "否"))
                        {
                            if (File.Exists(zipFile))
                            {
                                File.Delete(zipFile);
                            }
                        }
                    };
                }

                // 上传到百度网盘
                if (DrawIconBtn("Update-Available@2x", "上传到百度网盘,需要先配置BaiduPCS-Go,登录"))
                {
                    Debug.Log("上传到百度网盘");
                    RunCmd(baiduPCSGoFolderFullPath, $"BaiduPCS-Go upload {zipFile} /apps/BaiduPCS-Go/{PlayerSettings.productName}");
                }

                DrawIconBtnOpenFolder(zipFile);

                // 复制文件到剪贴板
                if (DrawIconBtn("d_TreeEditor.Duplicate", $"复制文件到剪贴板，方便粘贴到微信等其他软件"))
                {
                    WinClipboard.CopyFiles(new[] { zipFile });
                    Debug.Log($"已复制: {zipFile}");
                }
            }
            GUILayout.EndHorizontal();
        }

        #endregion

        #endregion

        #region 辅助绘制

        private static void DrawItem(string title, string content)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(titleWidth));
                GUILayout.Label(content, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawVersionControl()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("版本设置:", GUILayout.Width(titleWidth));

                // 解析当前版本号
                string currentVersion = PlayerSettings.bundleVersion;
                string[] versionParts = currentVersion.Split('.');
                int major = versionParts.Length > 0 ? int.Parse(versionParts[0]) : 0;
                int minor = versionParts.Length > 1 ? int.Parse(versionParts[1]) : 0;
                int revision = versionParts.Length > 2 ? int.Parse(versionParts[2]) : 0;

                GUILayout.Label("主要:", GUILayout.ExpandWidth(false));
                if (GUILayout.Button("▲", GUILayout.Width(25)))
                {
                    major++;
                    UpdateVersion(major, minor, revision);
                }
                GUILayout.Label(major.ToString(), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("▼", GUILayout.Width(25)))
                {
                    major = Mathf.Max(0, major - 1);
                    UpdateVersion(major, minor, revision);
                }

                GUILayout.Label("次要:", GUILayout.ExpandWidth(false));
                if (GUILayout.Button("▲", GUILayout.Width(25)))
                {
                    minor++;
                    UpdateVersion(major, minor, revision);
                }
                GUILayout.Label(minor.ToString(), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("▼", GUILayout.Width(25)))
                {
                    minor = Mathf.Max(0, minor - 1);
                    UpdateVersion(major, minor, revision);
                }

                GUILayout.Label("修订:", GUILayout.ExpandWidth(false));
                if (GUILayout.Button("▲", GUILayout.Width(25)))
                {
                    revision++;
                    UpdateVersion(major, minor, revision);
                }
                GUILayout.Label(revision.ToString(), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("▼", GUILayout.Width(25)))
                {
                    revision = Mathf.Max(0, revision - 1);
                    UpdateVersion(major, minor, revision);
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void UpdateVersion(int major, int minor, int revision)
        {
            PlayerSettings.bundleVersion = $"{major}.{minor}.{revision}";
            Debug.Log($"版本号已更新为: {PlayerSettings.bundleVersion}");
        }

        #endregion

        #region 命令执行

        private static void ExecuteExe(string exeFullPath, bool admin = false)
        {
            //Process.Start只支持windows的路径格式（左斜线分隔）
            exeFullPath = exeFullPath.Replace('/', '\\');
            if (!exeFullPath.EndsWith(".exe"))
            {
                Debug.LogError("ExecuteExe only support .exe");
                return;
            }

            if (!File.Exists(exeFullPath))
            {
                Debug.LogError($"{exeFullPath} do not exist");
                return;
            }

            string executeFileName = Path.GetFileName(exeFullPath); //文件名  xxx.exe
            string workingDirectory = Path.GetDirectoryName(exeFullPath); //文件所在目录  D:\xxx\

            Process proc = new Process();
            proc.StartInfo.WorkingDirectory = workingDirectory;
            proc.StartInfo.FileName = executeFileName;
            if (admin)
                proc.StartInfo.Verb = "runas"; //使用管理员运行
            proc.Start();
            Debug.Log($"Run exe {exeFullPath}");
        }

        //macos 可用
        public static void OpenFile(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = filePath,
                    UseShellExecute = false
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"打开文件失败: {e.Message}");
            }
        }

        #endregion

        #region PC构建辅助

        //E:\ProjectFolder\ProjectName\Build\folderName\exeName.exe
        private static string GetBuildFullPath(string folderName, string exeName)
        {
            string fullPath = Application.dataPath;
            fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
            fullPath = Path.Combine(fullPath, $"Build/{folderName}/{exeName}.exe");
            fullPath = fullPath.Replace('/', '\\');
            return fullPath;
        }

        private static string CurrSceneBuildFullPath()
        {
            string buildName = sceneBuildName;
            return GetBuildFullPath(buildName, buildName);
        }

        private static string CurrProjectBuildFullPath()
        {
            string currProjectName = realProjectBuildName;
            return GetBuildFullPath(currProjectName, currProjectName);
        }

        private static void BuildStandaloneProject()
        {
            string buildName = realProjectBuildName;
            AddCurrSceneToBuildSetting();
            SetBuildSceneEnable(false);
            BuildStandalone(buildName, buildName);
        }

        private static void BuildStandaloneCurrScene(bool uncheckOtherScene)
        {
            AddCurrSceneToBuildSetting();
            if (uncheckOtherScene)
                SetBuildSceneEnable(true);
            BuildStandalone(sceneBuildName, sceneBuildName);
        }

        #endregion

        #region 通用构建

        private static BuildOptions GetBuildOptions()
        {
            BuildOptions options = BuildOptions.None;
            if (devBuild)
                options |= BuildOptions.Development;
            return options;
        }

        //删除旧构建的确认
        private static bool DeleteOldBuildConfirm(string folder)
        {
            bool continueBuild = true;
            if (!deleteOldBuildBeforeBuild)
                return true;

            if (Directory.Exists(folder))
            {
                if (EditorUtility.DisplayDialog("删除旧构建", $"是否在构建前删除旧构建目录{folder} ?", "是,继续构建", "否,取消构建"))
                {
                    Directory.Delete(folder, true);
                    continueBuild = true;
                }
                else
                {
                    continueBuild = false;
                }
            }
            return continueBuild;
        }

        private static void LogBuildResult(BuildSummary summary)
        {
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"✅Build succeeded");
                    break;
                case BuildResult.Failed:
                    Debug.LogError("❌Build failed");
                    break;
            }
        }

        private static void BuildStandalone(string folderName, string exeName)
        {
            string folder = Path.GetDirectoryName(GetBuildFullPath(folderName, exeName));
            if (!DeleteOldBuildConfirm(folder))
                return;

            PlayerSettings.productName = exeName;// 这样会将打包后程序的窗口标题设置为exeName
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.locationPathName = $"Build/{folderName}/{exeName}.exe";
            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = GetBuildOptions();
            //开始构建
            //当没有项目名称文件夹的时候如（TimePuzzle），Unity 2021.3.16f1可能会在这里有个bug。需要在Build目录建立TimePuzzle文件夹后才能构建到时间拼图文件夹里。
            //构建的时候如果这里报一个警告，说用户脚本引用了WinForm。
            //可以尝试将Other Settings里的Scripting Define Symbols里的PH_WINFORMS去掉。
            //或者删除代码中的System.Windows.Forms
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            LogBuildResult(summary);
        }

        private static void BuildIOS(string locationPathName)
        {
            if (!DeleteOldBuildConfirm(locationPathName))
                return;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = BuildTarget.iOS;
            buildPlayerOptions.options = GetBuildOptions();
            //开始构建
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            LogBuildResult(summary);
        }

        private static void BuildAndroid(string locationPathName)
        {
            Debug.Log($"BuildAndroid: {locationPathName}");
            if (!DeleteOldBuildConfirm(locationPathName))
                return;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, $"com.{Application.companyName}.{Application.productName}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = GetBuildOptions();
            //开始构建
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            LogBuildResult(summary);
        }

        private static void BuildWebGL(string locationPathName)
        {
            if (!DeleteOldBuildConfirm(locationPathName))
                return;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = GetBuildOptions();
            //开始构建
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            LogBuildResult(summary);
        }

        private static void BuildMacOS(string locationPathName)
        {
            if (!DeleteOldBuildConfirm(locationPathName))
                return;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = BuildTarget.StandaloneOSX;
            buildPlayerOptions.options = GetBuildOptions();
            //开始构建
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            LogBuildResult(summary);
        }

        #endregion

        #region 场景管理

        private static void AddCurrSceneToBuildSetting()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            string currScenePath = SceneManager.GetActiveScene().path;
            int count = scenes.Where(scene => scene.path == currScenePath).Count();
            if (count == 0) //如果构建设置中没有，添加当前场景到构建设置中
            {
                EditorBuildSettingsScene scene = new EditorBuildSettingsScene();
                scene.path = currScenePath;
                List<EditorBuildSettingsScene> scenesList = scenes.ToList();
                scenesList.Add(scene);
                scenes = scenesList.ToArray();
            }

            EditorBuildSettings.scenes = scenes;
        }

        // 设置构建设置中各场景的启用状态
        // 如果 onlyCurrentScene 为 true，则只启用当前活动场景，其余场景禁用
        // 如果 onlyCurrentScene 为 false，则启用所有场景
        private static void SetBuildSceneEnable(bool onlyCurrentScene)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            foreach (var scene in scenes)
            {
                if (onlyCurrentScene)
                    scene.enabled = scene.path == SceneManager.GetActiveScene().path;
                else
                    scene.enabled = true;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        #endregion

        #region 压缩

        // https://www.cnblogs.com/rainbow70626/p/4559691.html
        private void CreateZip(string sourceFolderPath, string destinationZipFilePath)
        {
            //解决中文乱码
            Encoding gbk = Encoding.GetEncoding("gbk");
            ICSharpCode.SharpZipLib.Zip.ZipStrings.CodePage = gbk.CodePage;
            var fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fastZip.CreateZip(destinationZipFilePath, sourceFolderPath, true, null);
        }
        #endregion

        #region 日期时间处理
        private string CurrentDateTimeString()
        {
            return DateTime.Now.ToString("yyyyMMddHHmm");
        }

        private DateTime GetDateTimeFromFileName(string fileName)
        {
            string regex = @"\d{12}";// yyyyMMddHHmm
            Match match = Regex.Match(fileName, regex);
            if (match.Success)
            {
                string dateTimeString = match.Value;
                return DateTime.ParseExact(dateTimeString, "yyyyMMddHHmm", null);
            }
            return DateTime.MinValue;
        }

        private string GetTimeSpanFromDateTime(DateTime dateTime)
        {
            TimeSpan timeSpan = DateTime.Now - dateTime;
            if (timeSpan.TotalDays >= 36500)
            {
                return "x天前";
            }
            else if (timeSpan.TotalDays >= 1)
            {
                int days = (int)timeSpan.TotalDays;
                return $"{days}天前";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                int hours = (int)timeSpan.TotalHours;
                return $"{hours}小时前";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                int minutes = (int)timeSpan.TotalMinutes;
                return $"{minutes}分钟前";
            }
            else
            {
                int seconds = (int)timeSpan.TotalSeconds;
                return $"{seconds}秒前";
            }
        }
        #endregion


    }
}