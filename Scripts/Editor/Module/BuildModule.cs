using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PluginHub.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PluginHub.Editor
{
    public class BuildModule : PluginHubModuleBase, IPreprocessBuildWithReport
    {
        public override ModuleType moduleType => ModuleType.Shortcut;
        public override string moduleName
        {
            get { return "构建"; }
        }

        public override string moduleDescription => "一键Build,便于快速迭代";

        private static float titleWidth = 70;


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
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_devBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_devBuild", value); }
        }

        //是否构建前删除旧的构建目录
        private static bool deleteOldBuildBeforeBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_deleteOldBuildBeforeBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_deleteOldBuildBeforeBuild", value); }
        }

        //是否构建前清空编辑器中的StreamingAssets文件夹
        private static bool clearStreamingAssetsBeforeBuild
        {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_clearStreamingAssetsBeforeBuild", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_clearStreamingAssetsBeforeBuild", value); }
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
                return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_iosUseShortBuildPath",
                    false);
            }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_iosUseShortBuildPath", value); }
        }


        private string updateInfo
        {
            get { return EditorPrefs.GetString($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_updateInfo", ""); }
            set { EditorPrefs.SetString($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_updateInfo", value); }
        }

        private static string lastBuildTime
        {
            get { return EditorPrefs.GetString($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_lastBuildTime", ""); }
            set { EditorPrefs.SetString($"{PluginHubFunc.ProjectUniquePrefix}_BuildModule_lastBuildTime", value); }
        }

        #region 构建预处理/后处理

        //构建预处理
        public void OnPreprocessBuild(BuildReport report)
        {
            if(!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);
            //写 BuildInfo.txt
            INIParser iniParser = new INIParser();
            iniParser.Open(Path.Combine(Application.streamingAssetsPath, "BuildInfo.txt"));
            //将updateInfo中的换行符替换为\n保存
            iniParser.WriteValue("BuildInfo", "UpdateInfo", updateInfo.Replace("\n", "\\n").Trim());
            iniParser.Close();

            if (clearStreamingAssetsBeforeBuild)
            {
                if(EditorUtility.DisplayDialog("清空StreamingAssets", "您选择了 clearStreamingAssetsBeforeBuild 是否删除 StreamingAssets 文件夹下的所有文件？", "是", "否"))
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

        public int callbackOrder => -999999;

        //构建后处理
        [PostProcessBuild]
        public static void PostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                //构建后自动自增Build号
                string oldBuildNumber = PlayerSettings.iOS.buildNumber;
                PlayerSettings.iOS.buildNumber = (int.Parse(PlayerSettings.iOS.buildNumber) + 1).ToString();
                Debug.Log($"Build ID从{oldBuildNumber}自增到{PlayerSettings.iOS.buildNumber}");
            }
            else if (buildTarget == BuildTarget.StandaloneWindows64)
            {
                string oldVersion = PlayerSettings.bundleVersion;
                int lastIndex = oldVersion.LastIndexOf('.');
                string majorVersion = oldVersion.Substring(0, lastIndex);
                string minorVersion = oldVersion.Substring(lastIndex + 1);
                minorVersion = (int.Parse(minorVersion) + 1).ToString();
                //构建后自增minorVersion号
                PlayerSettings.bundleVersion = $"{majorVersion}.{minorVersion}";
                Debug.Log($"版本号从{oldVersion}自增到{PlayerSettings.bundleVersion}");
            }
            lastBuildTime = DateTime.Now.ToString("yyyy - MM - dd  HH: mm: ss");
        }

        #endregion

        protected override void DrawGuiContent()
        {
            DrawSplitLine("构建信息");

            DrawItem("公司名称:", PlayerSettings.companyName);
            DrawItem("产品名称:", PlayerSettings.productName);
            DrawItem("版本:", PlayerSettings.bundleVersion);
            DrawItem("默认屏幕:", PlayerSettings.defaultScreenWidth + " x " + PlayerSettings.defaultScreenHeight);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("更新内容:", GUILayout.Width(titleWidth));
                updateInfo = GUILayout.TextArea(updateInfo);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("更多选项:", GUILayout.Width(titleWidth));
                devBuild = GUILayout.Toggle(devBuild, new GUIContent("开发构建"));
                deleteOldBuildBeforeBuild = GUILayout.Toggle(deleteOldBuildBeforeBuild, new GUIContent("构建前删除旧的构建", "虽然构建会将之前的覆盖,但有时动态生成的多余文件可能仍会被保留.使用此选项在构建前先删除旧构建文件夹以确保干净。"));
                clearStreamingAssetsBeforeBuild = GUILayout.Toggle(clearStreamingAssetsBeforeBuild, new GUIContent("构建前清空StreamingAssets"));
            }
            GUILayout.EndHorizontal();


            DrawSplitLine("快捷构建");

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("最近构建:", GUILayout.Width(titleWidth));
                GUILayout.Label(lastBuildTime == "" ? "无" : lastBuildTime);
            }
            GUILayout.EndHorizontal();

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
            DrawBuildLibary();
        }

        private void DrawPCBuildButtons()
        {
            //PC平台快捷构建按钮
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(PluginHubFunc.IconContent("BuildSettings.Standalone On"));
                    GUILayout.Label($"平台 : {BuildTarget.StandaloneWindows64}");
                }
                GUILayout.EndHorizontal();


                GUILayout.BeginVertical("Box");
                {
                    GUILayout.Label("项目构建:");
                    projectBuildName = EditorGUILayout.TextField("项目构建名称:", projectBuildName);
                    GUILayout.BeginHorizontal();
                    {
                        string path = CurrProjectBuildFullPath();
                        if (GUILayout.Button(PluginHubFunc.GuiContent("构建项目", $"将构建到{path}")))
                        {
                            BuildProject();
                            GUIUtility.ExitGUI();
                        }

                        DrawIconBtnOpenFolder(path, true);

                        //运行按钮
                        GUI.enabled = File.Exists(path);
                        if (GUILayout.Button("运行", GUILayout.ExpandWidth(false)))
                        {
                            ExecuteExe(path);
                        }

                        GUI.enabled = true;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();


                GUILayout.BeginVertical("Box");
                {
                    GUILayout.Label("场景构建:");
                    sceneBuildName = EditorGUILayout.TextField("场景构建名称:", sceneBuildName);

                    GUILayout.BeginHorizontal();
                    {
                        string path = CurrSceneBuildFullPath();
                        if (GUILayout.Button(PluginHubFunc.GuiContent("构建当前场景", $"将会直接构建到{path}。")))
                        {
                            BuildCurrScene(false);
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button(
                                PluginHubFunc.GuiContent("仅构建当前场景", $"程序将先在构建设置中取消激活其它已添加的场景\n然后构建到{path}。"),
                                GUILayout.ExpandWidth(false)))
                        {
                            BuildCurrScene(true);
                            GUIUtility.ExitGUI();
                        }

                        //open folder button
                        DrawIconBtnOpenFolder(path, true);

                        //运行按钮
                        GUI.enabled = File.Exists(path);
                        if (GUILayout.Button("运行", GUILayout.ExpandWidth(false)))
                        {
                            ExecuteExe(path);
                        }

                        GUI.enabled = true;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        public void DrawIOSBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(PluginHubFunc.IconContent("BuildSettings.iPhone On"));
                    GUILayout.Label($"平台 : {BuildTarget.iOS}");
                }
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                {
                    //ios build id
                    DrawItem("Build ID:", PlayerSettings.iOS.buildNumber);
                    if (GUILayout.Button("归零构建ID", GUILayout.ExpandWidth(false)))
                    {
                        PlayerSettings.iOS.buildNumber = "0";
                    }
                }
                GUILayout.EndHorizontal();
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

                    if (GUILayout.Button(PluginHubFunc.GuiContent("构建 IOS 项目", $"将构建到{path}"),GUILayout.Height(PluginHubFunc.NormalBtnHeight)))
                    {
                        //执行构建
                        BuildIOS(path);
                        GUIUtility.ExitGUI();
                    }

                    // 快捷打开xCode项目的icon按钮
                    if (Application.platform == RuntimePlatform.OSXEditor){
                        string xCodePath = Path.Combine(fullPath, $"Unity-iPhone.xcodeproj");
                        xCodePath = xCodePath.Replace('\\', '/');
                        bool exist = Directory.Exists(xCodePath);
                        GUI.enabled = exist;
                        if (DrawIconBtn("BuildSettings.iPhone On@2x", $"打开xCode项目{xCodePath}"))
                        {
                            OpenFile(xCodePath);
                            GUIUtility.ExitGUI();
                        }
                        GUI.enabled = true;
                    }

                    DrawIconBtnOpenFolder(path, true);
                    DrawIconBtnCopy(path);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public void DrawAndroidBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(PluginHubFunc.IconContent("BuildSettings.Android On"));
                    GUILayout.Label($"平台 : {BuildTarget.Android}");
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    string fullPath = Application.dataPath;
                    fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
                    fullPath = Path.Combine(fullPath, $"Build/Android/");
                    fullPath = fullPath.Replace('/', '\\');
                    string path = fullPath;
                    if (GUILayout.Button(PluginHubFunc.GuiContent("构建 Android 项目", $"将构建到{path}")))
                    {
                        //执行构建
                        BuildAndroid($"Build/Android/{PlayerSettings.applicationIdentifier}.apk");
                        GUIUtility.ExitGUI();
                    }

                    DrawIconBtnOpenFolder(path, true);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public void DrawWebGLBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(PluginHubFunc.IconContent("BuildSettings.WebGL On"));
                    GUILayout.Label($"平台 : {BuildTarget.WebGL}");
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    string fullPath = Application.dataPath;
                    fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
                    fullPath = Path.Combine(fullPath, $"Build/WebGL/");
                    fullPath = fullPath.Replace('/', '\\');
                    string path = fullPath;
                    if (GUILayout.Button(PluginHubFunc.GuiContent("构建 WebGL 项目", $"将构建到{path}")))
                    {
                        //执行构建
                        BuildWebGL($@"Build/WebGL/");
                        GUIUtility.ExitGUI();
                    }

                    DrawIconBtnOpenFolder(path, true);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawMacOSBuildButtons()
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(PluginHubFunc.IconContent("BuildSettings.Standalone On"));
                    GUILayout.Label($"平台 : {BuildTarget.StandaloneOSX}");
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    string fullPath = Application.dataPath;
                    fullPath = fullPath.Substring(0, fullPath.LastIndexOf('/') + 1);
                    fullPath = Path.Combine(fullPath, $"Build/MacOS/{PlayerSettings.productName}.app");
                    string path = fullPath;
                    if (GUILayout.Button(PluginHubFunc.GuiContent("构建 MacOS 项目", $"将构建到{path}")))
                    {
                        //执行构建
                        BuildMacOS($@"Build/MacOS/{PlayerSettings.productName}.app");
                        GUIUtility.ExitGUI();
                    }
                    DrawIconBtnOpenFolder(path, true);
                    //

                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawBuildLibary()
        {
            //构建库
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(PluginHubFunc.IconContent("VerticalLayoutGroup Icon"));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(PluginHubFunc.GuiContent("构建库:", "下方显示项目Build目录下的所有打包文件"));
                    GUILayout.FlexibleSpace();
                    DrawIconBtnOpenFolder(Path.Combine(Application.dataPath, "../Build/"), true);
                }
                GUILayout.EndHorizontal();


                string buildPath = Path.Combine(Application.dataPath, "../Build");
                if (Directory.Exists(buildPath))
                {
                    string[] directories = Directory.GetDirectories(buildPath);
                    for (int i = 0; i < directories.Length; i++)
                    {
                        string directory = directories[i];
                        string folderName = Path.GetFileName(directory);
                        string executeFullpath = Path.Combine(directory, $"{folderName}.exe");

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label($"{i}. {folderName}");

                            string streamingAssetsPath = Path.Combine(directory, $"{folderName}_Data/StreamingAssets/");
                            //打开StreamingAssets文件夹按钮
                            DrawIconBtnOpenFolder(streamingAssetsPath, true, "StrmAs");

                            //打开文件夹按钮
                            DrawIconBtnOpenFolder(directory, true);

                            //压缩这个构建到当前目录
                            if (GUILayout.Button("zip", GUILayout.ExpandWidth(false)))
                            {
                                // 2024年10月18日,目前遇到跨年的项目了-_-,为了分清楚版本添加了年份
                                string timeStr = DateTime.Now.ToString("yy年MM月dd日 HH-mm");
                                string destZipPath = Path.Combine(directory + $" {timeStr}.zip");
                                CreateZip(directory, destZipPath);
                            }

                            // TODO
                            // PluginHubFunc.DrawCopyFileButton(executeFullpath);

                            GUI.enabled = File.Exists(executeFullpath);
                            if (GUILayout.Button("运行", GUILayout.ExpandWidth(false)))
                            {
                                ExecuteExe(executeFullpath);
                            }

                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
        }

        public static void ExecuteExe(string exeFullPath, bool admin = false)
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

        private static void DrawItem(string title, string content)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(titleWidth));
                GUILayout.Label(content, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
        }

        private static BuildOptions GetBuildOptions()
        {
            BuildOptions options = BuildOptions.None;
            if (devBuild)
                options |= BuildOptions.Development;
            return options;
        }

        #region for pc build

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
            string currProjectName = projectBuildName;
            return GetBuildFullPath(currProjectName, currProjectName);
        }

        private static void BuildProject()
        {
            string buildName = projectBuildName;
            AddCurrSceneToBuildSetting();
            SetBuildSceneEnable(false);
            BuildStandalone(buildName, buildName);
        }

        private static void BuildCurrScene(bool uncheckOtherScene)
        {
            AddCurrSceneToBuildSetting();
            SetBuildSceneEnable(uncheckOtherScene);
            BuildStandalone(sceneBuildName, sceneBuildName);
        }

        #endregion

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

        #region final build


        // private static void PreBuild(BuildTarget buildTarget)
        // {
        //
        // }

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
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded");
                    // Debug.Log($"{summary.outputPath}");
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed");
                    // Debug.Log(summary.outputPath);
                    break;
            }
        }

        //$"Build/IOS/{PlayerSettings.applicationIdentifier}_xcode";
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
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded");
                    // Debug.Log($"{summary.outputPath}");
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed");
                    break;
            }
        }

        //$"Build/Android/{PlayerSettings.applicationIdentifier}.apk";
        private static void BuildAndroid(string locationPathName)
        {
            if (!DeleteOldBuildConfirm(locationPathName))
                return;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = GetBuildOptions();
            //开始构建
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded");
                    // Debug.Log($"{summary.outputPath}");
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed");
                    break;
            }
        }

        //$"Build/WebGL/"
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
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded");
                    // Debug.Log($"{summary.outputPath}");
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed");
                    break;
            }
        }

        //$"Build/MacOS/[ProductName].app"
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
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded");
                    // Debug.Log($"{summary.outputPath}");
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed");
                    break;
            }
        }


        #endregion


        private static void AddCurrSceneToBuildSetting()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            string currScenePath = EditorSceneManager.GetActiveScene().path;
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

        private static void SetBuildSceneEnable(bool uncheckOtherScene)
        {
            //设置构建场景为启用状态，其他禁用
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            foreach (var scene in scenes)
            {
                if (scene.path == EditorSceneManager.GetActiveScene().path)
                    scene.enabled = true;
                else
                {
                    if (uncheckOtherScene)
                        scene.enabled = false;
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// https://www.cnblogs.com/rainbow70626/p/4559691.html
        /// </summary>
        /// <param name="sourceFolderPath">E:\test\</param>
        /// <param name="destinationZipFilePath">E:\test.zip</param>
        public static void CreateZip(string sourceFolderPath, string destinationZipFilePath)
        {
            //解决中文乱码
            Encoding gbk = Encoding.GetEncoding("gbk");
            ICSharpCode.SharpZipLib.Zip.ZipStrings.CodePage = gbk.CodePage;

            var fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fastZip.CreateZip(destinationZipFilePath, sourceFolderPath, true, null);
        }
    }
}