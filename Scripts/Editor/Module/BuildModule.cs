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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace PluginHub.Editor
{
    public partial class BuildModule : PluginHubModuleBase
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

        //是否构建完成后自动运行（替代原先按住 Ctrl 点击构建按钮的行为）
        private static bool buildAndRun
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_buildAndRun", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_buildAndRun", value); }
        }

        //是否构建完成后自动压缩构建目录（与构建库「zip」按钮逻辑一致）
        private static bool autoZipAfterBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_autoZipAfterBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_autoZipAfterBuild", value); }
        }

        //是否在打包前将当前激活场景名称写入 PlayerSettings.productName
        private static bool useSceneNameAsProductName
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_useSceneNameAsProductName", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_useSceneNameAsProductName", value); }
        }

        //PC平台场景构建时,构建项目时用于exe执行文件的名称和构建目录名，如果为空，则使用项目文件夹名称
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

        //是否标记为不应使用项目构建
        private static bool isMarkShouldNotUseProjectBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_isMarkShouldNotUseProjectBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_isMarkShouldNotUseProjectBuild", value); }
        }

        //是否标记为不应使用场景构建
        private static bool isMarkShouldNotUseSceneBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_isMarkShouldNotUseSceneBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_isMarkShouldNotUseSceneBuild", value); }
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
                // 生成中禁用文本框旁的 AI 按钮，避免重复触发
                EditorGUI.BeginDisabledGroup(isGeneratingUpdateInfo);
                updateInfo = GUILayout.TextArea(updateInfo);
                string aiBtnLabel = isGeneratingUpdateInfo ? "生成中…" : "AI";
                if (GUILayout.Button(PluginHubEditor.GuiContent(aiBtnLabel, "根据手选 git 区间的整仓 diff，用本机 Cursor Agent 润色成更新说明"), GUILayout.Width(56), GUILayout.ExpandHeight(true)))
                {
                    Debug.Log("[BuildModule.UpdateInfoAI] 打开 commit 区间选择窗口");
                    UpdateInfoCommitRangeWindow.Open(OnUpdateInfoCommitRangeConfirmed);
                }
                EditorGUI.EndDisabledGroup();
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
                    if (GUILayout.Button("打开场景列表"))
                    {
                        // 打开 Unity 自带的场景构建选择界面（Build Settings / Build Profiles）
                        Debug.Log("[BuildModule] 打开 Unity 场景构建选择界面");
                        BuildPlayerWindow.ShowBuildPlayerWindow();
                    }
                    devBuild = GUILayout.Toggle(devBuild, PluginHubEditor.GuiContent($"开发构建{(devBuild?"🛠️":"")}"));
                    deleteOldBuildBeforeBuild = GUILayout.Toggle(deleteOldBuildBeforeBuild, PluginHubEditor.GuiContent($"构建前删除旧的构建{(deleteOldBuildBeforeBuild?"❌":"")}", "虽然构建会将之前的覆盖,但有时动态生成的多余文件可能仍会被保留.使用此选项在构建前先删除旧构建文件夹以确保干净。"));
                    clearStreamingAssetsBeforeBuild = GUILayout.Toggle(clearStreamingAssetsBeforeBuild, PluginHubEditor.GuiContent($"构建前清空StreamingAssets{(clearStreamingAssetsBeforeBuild?"🧹":"")}"));
                    buildAndRun = GUILayout.Toggle(buildAndRun, PluginHubEditor.GuiContent($"构建后运行{(buildAndRun?"▶️":"")}", "勾选后，点击构建按钮将在构建完成后自动运行。"));
                    autoZipAfterBuild = GUILayout.Toggle(autoZipAfterBuild, PluginHubEditor.GuiContent($"构建后自动压缩{(autoZipAfterBuild?"📦":"")}", "勾选后，Windows 构建成功后将自动压缩构建目录（时间命名）并复制 zip 到剪贴板。"));
                    useSceneNameAsProductName = GUILayout.Toggle(useSceneNameAsProductName, PluginHubEditor.GuiContent($"使用场景名作为产品名称{(useSceneNameAsProductName?"🏞️":"")}", "勾选后，打包前将当前激活场景名称临时写入 Product Name，构建结束后自动还原。"));
                    enablePostCopy = GUILayout.Toggle(enablePostCopy, PluginHubEditor.GuiContent($"构建后复制文件夹到构建目录{(enablePostCopy?"📋":"")}"));
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
            using (new GUILayout.VerticalScope("Box"))
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
            // 标记为不应使用时，整块区域背景染红，提醒优先用另一构建方式
            Color oldBg = GUI.backgroundColor;
            if (isMarkShouldNotUseProjectBuild)
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f, 1f);

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("项目构建:");
                    GUILayout.FlexibleSpace();
                    isMarkShouldNotUseProjectBuild = GUILayout.Toggle(
                        isMarkShouldNotUseProjectBuild,
                        PluginHubEditor.GuiContent("不应使用", "勾选后此区域标红，提醒本项目不应使用「项目构建」。"));
                }
                GUILayout.EndHorizontal();
                projectBuildName = EditorGUILayout.TextField("项目构建名称:", projectBuildName);
                GUILayout.BeginHorizontal();
                {
                    string exePath = $"Build/{projectBuildName}/{projectBuildName}.exe";
                    DrawBuildButton(
                        "构建项目",
                        $"将构建到{exePath}",
                        () =>
                        {
                            SceneManage_AddCurrSceneToBuildSetting();
                            SceneManage_SetBuildSceneEnable(false);
                            ExecuteBuild(BuildTarget.StandaloneWindows64, exePath);
                        });
                    DrawIconBtnOpenFolder(exePath);
                    DrawRunButton(exePath);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUI.backgroundColor = oldBg;
        }

        private void DrawPCSceneBuildSection()
        {
            // 标记为不应使用时，整块区域背景染红，提醒优先用另一构建方式
            Color oldBg = GUI.backgroundColor;
            if (isMarkShouldNotUseSceneBuild)
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f, 1f);

            using (new GUILayout.VerticalScope("Box"))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("场景构建:");
                    GUILayout.FlexibleSpace();
                    isMarkShouldNotUseSceneBuild = GUILayout.Toggle(
                        isMarkShouldNotUseSceneBuild,
                        PluginHubEditor.GuiContent("不应使用", "勾选后此区域标红，提醒本项目不应使用「场景构建」。"));
                }
                sceneBuildName = EditorGUILayout.TextField("场景构建名称:", sceneBuildName);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    string exePath = $"Build/{sceneBuildName}/{sceneBuildName}.exe";
                    DrawBuildButton(
                        "仅构建当前场景",
                        $"将构建到{exePath}",
                        () =>
                        {
                            SceneManage_AddCurrSceneToBuildSetting();
                            SceneManage_SetBuildSceneEnable(true);
                            ExecuteBuild(BuildTarget.StandaloneWindows64, exePath);
                        });
                    DrawIconBtnOpenFolder(exePath);
                    DrawRunButton(exePath);
                }
            }

            GUI.backgroundColor = oldBg;
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
                    PluginHubEditor.GuiContent("使用短小的构建路径", "当由于路径过长导致构建失败时，可以尝试勾选此选项。"));
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

                DrawBuildButton(
                    "构建 IOS 项目",
                    $"将构建到{path}",
                    () => ExecuteBuild(BuildTarget.iOS, path),
                    GUILayout.Height(PluginHubEditor.NormalBtnHeight));

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
                string apkPath = $"Build/Android/{PlayerSettings.applicationIdentifier}.apk";
                DrawBuildButton(
                    "构建 Android 项目",
                    $"将构建到{path}",
                    () => ExecuteBuild(BuildTarget.Android, apkPath));

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
                DrawBuildButton(
                    "构建 WebGL 项目",
                    $"将构建到{path}",
                    () => ExecuteBuild(BuildTarget.WebGL, @"Build/WebGL/"));
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
                string macBuildPath = $@"Build/MacOS/{PlayerSettings.productName}.app";
                DrawBuildButton(
                    "构建 MacOS 项目",
                    $"将构建到{path}",
                    () => ExecuteBuild(BuildTarget.StandaloneOSX, macBuildPath));
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
                    EditorApplication.delayCall += () => ZipBuildDirectoryAndCopyToClipboard(directory);
                }

                //运行按钮
                DrawRunButton(executeFullpath);
            }
            GUILayout.EndHorizontal();
            DrawZebraRowBackground(index);
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

        private static string ZipBuildDirectory(string directory)
        {
            string timeStr = CurrentDateTimeString();
            string destZipPath = Path.Combine(directory + $"_{timeStr}.zip");
            CreateZip(directory, destZipPath);
            return destZipPath;
        }

        /// <summary>
        /// 压缩构建目录并将 zip 复制到剪贴板（构建库「zip」按钮与「构建后自动压缩」共用）。
        /// </summary>
        private static void ZipBuildDirectoryAndCopyToClipboard(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                Debug.LogWarning($"[BuildModule] 跳过压缩：构建目录无效，directory={directory}");
                return;
            }

            string zipFilePath = ZipBuildDirectory(directory);
            WinClipboard.CopyFiles(new[] { zipFilePath });
            Debug.Log($"[BuildModule] 已压缩并复制到剪贴板: {zipFilePath}");
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
                bool isSceneBuildZipFile = zipFiles[i].Contains(SceneManager.GetActiveScene().name);
                DrawZipFileItem(zipFiles[i], i, isLatest, isSceneBuildZipFile);
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
                if (DrawIconBtn("Update-Available@2x", "上传到百度网盘,需要先配置BaiduPCS-Go,登录（已存在会跳过）"))
                {
                    Debug.Log("上传到百度网盘");
                    // 显式指定 skip：网盘已有同名文件时跳过，不覆盖
                    RunCmd(baiduPCSGoFolderFullPath, $"BaiduPCS-Go upload {zipFile} /apps/BaiduPCS-Go/{PlayerSettings.productName} --policy skip");
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
            DrawZebraRowBackground(index);
        }

        #endregion

        #endregion

        #region 辅助绘制

        /// <summary>
        /// 行绘制完成后为奇数行叠加半透明背景。所有行均使用相同的 BeginHorizontal()，避免样式差异导致列对不齐。
        /// </summary>
        private static void DrawZebraRowBackground(int index)
        {
            if (Event.current.type != EventType.Repaint || index % 2 != 1)
                return;

            Rect rowRect = GUILayoutUtility.GetLastRect();
            bool isDarkTheme = EditorGUIUtility.isProSkin;
            float alpha = isDarkTheme ? 0.08f : 0.06f;
            Color bgColor = isDarkTheme
                ? new Color(1f, 1f, 1f, alpha)
                : new Color(0f, 0f, 0f, alpha);
            EditorGUI.DrawRect(rowRect, bgColor);
        }

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
            Debug.Log($"[BuildModule] ▶️Run exe {exeFullPath}");
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

        #region 压缩

        // https://www.cnblogs.com/rainbow70626/p/4559691.html
        private static void CreateZip(string sourceFolderPath, string destinationZipFilePath)
        {
            //解决中文乱码
            Encoding gbk = Encoding.GetEncoding("gbk");
            ICSharpCode.SharpZipLib.Zip.ZipStrings.CodePage = gbk.CodePage;
            var fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fastZip.CreateZip(destinationZipFilePath, sourceFolderPath, true, null);
        }
        #endregion

        #region 日期时间处理
        private static string CurrentDateTimeString()
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