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
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace PluginHub.Editor
{
    // 处理构建
    public partial class BuildModule : PluginHubModuleBase
    {
        #region Build

        /// <summary>
        /// 绘制构建按钮
        /// </summary>
        private static void DrawBuildButton(string label, string tooltip, Action buildAction, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(PluginHubEditor.GuiContent(label, tooltip), options))
            {
                EditorApplication.delayCall += () => buildAction?.Invoke();
            }
        }

        private void ExecuteBuild(BuildTarget buildTarget, string locationPathName)
        {
            try
            {
                if (!OnPreprocessBuild(buildTarget, locationPathName))//构建预处理
                    return;//构建预处理失败，取消构建
                BuildReport report = null;
                {
                    BuildPlayerOptions buildPlayerOptions = new()
                    {
                        scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
                        locationPathName = locationPathName,
                        target = buildTarget,
                        options = GetBuildOptions(devBuild, buildAndRun)
                    };
                    report = BuildPipeline.BuildPlayer(buildPlayerOptions);//构建
                    LogBuildResult(report.summary);
                }
                if (report != null && report.summary.result == BuildResult.Succeeded)
                    OnBuildSucceeded(report.summary);//构建成功后处理
            }
            finally
            {
                // 无论成功/失败/异常，都还原临时改写的 Product Name，以及 StreamingAssets 隔离目录
                RestoreProductNameIfNeeded();
                RestoreParkedStreamingAssetsIfNeeded();
            }
        }

        #endregion

        #region 通用构建

        // 各平台 BuildXxx 统一调用顺序：
        // 1. DeleteOldBuildConfirm（用户可取消）
        // 2. 平台专属前置设置（如 Windows 设 productName、Android 设包名）
        // 3. ExecuteBuild → OnPreprocessBuild → BuildPipeline.BuildPlayer → OnBuildSucceeded

        private BuildOptions GetBuildOptions(bool developmentBuild, bool autoRunPlayer)
        {
            BuildOptions options = BuildOptions.None;
            if (developmentBuild)
                options |= BuildOptions.Development;
            if (autoRunPlayer)
                options |= BuildOptions.AutoRunPlayer;
            return options;
        }

        //删除旧构建的确认
        private bool DeleteOldBuildConfirm(string folder)
        {
            if (!deleteOldBuildBeforeBuild)
                return true;

            // Debug.Log($"[BuildModule] folder: {folder} : {AssetDatabase.IsValidFolder(folder)}");
            if (Directory.Exists(folder))
            {
                if (EditorUtility.DisplayDialog("删除旧构建", $"是否在构建前删除旧构建目录{folder} ?", "是,继续构建", "否,取消构建"))
                {
                    try
                    {
                        Directory.Delete(folder, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"删除旧构建目录失败: {e.Message}");
                        return false;//删除失败，取消构建
                    }
                    return true;//继续构建
                }
                else
                {
                    return false;//取消构建
                }
            }
            return true;//目录不存在，继续构建
        }

        private void LogBuildResult(BuildSummary summary)
        {
            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"[BuildModule] ✅Build succeeded");
                    break;
                case BuildResult.Failed:
                    Debug.LogError($"[BuildModule] ❌Build failed");
                    break;
            }
        }



        #endregion
        #region 构建预处理

        public string BuildInfoFilePath => Path.Combine(Application.streamingAssetsPath, "BuildInfo.txt");

        /// <summary>
        /// 构建前统一预处理（在 BuildPlayer 之前由各平台 BuildXxx 显式调用，不再走 IPreprocessBuildWithReport 回调）。
        /// 顺序：确保 StreamingAssets 存在 → 写入 BuildInfo → 按需清空 StreamingAssets → 按场景排除隔离。
        /// </summary>
        private bool OnPreprocessBuild(BuildTarget buildTarget, string locationPathName)
        {
            Debug.Log($"[BuildModule] 构建前预处理，平台: {buildTarget}");

            if (buildTarget == BuildTarget.StandaloneWindows64)
            {
                string buildDirectory = Path.Combine(PluginHubEditor.ProjectRoot, locationPathName);
                buildDirectory = Path.GetDirectoryName(buildDirectory);
                Debug.Log(buildDirectory);
                if (!DeleteOldBuildConfirm(buildDirectory))
                    return false;
            }
            ApplySceneNameAsProductNameIfNeeded();
            CreateStreamingAssetsIfNotExists();
            WriteBuildInfo();
            ClearStreamingAssetsIfNeeded();
            // 开关开启时：按「当前打开场景」的配置隔离 StreamingAssets（所有构建入口统一）
            if (!ApplyStreamingAssetsExcludeIfNeeded())
                return false;
            return true;
        }

        // 临时改写 Product Name 时的备份；null 表示无需还原
        private static string _productNameBackup;

        /// <summary>
        /// 勾选「使用场景名作为产品名称」时，将当前激活场景名写入 PlayerSettings.productName（构建结束后自动还原）。
        /// </summary>
        private void ApplySceneNameAsProductNameIfNeeded()
        {
            if (!useSceneNameAsProductName)
                return;

            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[BuildModule] 已勾选「使用场景名作为产品名称」，但当前激活场景名称为空，跳过设置 Product Name。");
                return;
            }

            // 已备份则不重复覆盖，避免异常路径下丢失原始值
            if (_productNameBackup == null)
                _productNameBackup = PlayerSettings.productName;

            PlayerSettings.productName = sceneName;
            Debug.Log($"[BuildModule] 使用场景名作为产品名称: \"{_productNameBackup}\" -> \"{sceneName}\"（构建后将自动还原）");
        }

        /// <summary>
        /// 还原 ApplySceneNameAsProductNameIfNeeded 临时改写的 Product Name。
        /// </summary>
        private void RestoreProductNameIfNeeded()
        {
            if (_productNameBackup == null)
                return;

            string restored = _productNameBackup;
            _productNameBackup = null;
            PlayerSettings.productName = restored;
            Debug.Log($"[BuildModule] 已还原产品名称: \"{restored}\"");
        }

        private void CreateStreamingAssetsIfNotExists()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        private void WriteBuildInfo()
        {
            string updateInfoText = EditorPrefs.GetString($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_updateInfo", "");
            Debug.Log($"[BuildModule] Write build info to {BuildInfoFilePath}");
            Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoFilePath));
            INIParser iniParser = new INIParser();
            iniParser.Open(BuildInfoFilePath);
            iniParser.WriteValue("BuildInfo", "UpdateInfo", updateInfoText.Replace("\n", "\\n").Trim());
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

        private void OnBuildSucceeded(BuildSummary summary)
        {
            Debug.Log($"[BuildModule] 构建成功后续处理，平台: {summary.platform}，输出: {summary.outputPath}");
            switch (summary.platform)
            {
                case BuildTarget.StandaloneWindows64:
                    OnWindowsBuildSucceeded(summary.outputPath);
                    break;
                case BuildTarget.iOS:
                    IncrementIOSBuildNumber();
                    break;
            }
        }

        /// <summary>
        /// Windows 构建成功后的统一后处理入口（由 OnBuildSucceeded 按平台分发调用，不再走 PostProcessBuild 回调）。
        /// </summary>
        private void OnWindowsBuildSucceeded(string pathToBuiltProject)
        {
            IncrementPCVersionNumber();// 增加版本号
            CopyDaemonRunBatToWindowsBuildDirectory(pathToBuiltProject);// 复制 daemon-run.bat 到构建目录
            CreateSteamingAssetsShortcutToBuildDirectory(pathToBuiltProject);// 创建SteamingAssets快捷方式到构建根目录
            ExecutePostCopyFolders(BuildTarget.StandaloneWindows64, pathToBuiltProject);// 执行构建后复制文件夹到构建目录

            if (autoZipAfterBuild)
            {
                string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
                Debug.Log($"[BuildModule] 构建后自动打包，目录: {buildDirectory}");
                ZipBuildDirectoryAndCopyToClipboard(buildDirectory);
            }
        }

        /// <summary>
        /// Windows 构建完成后，在 exe 同级目录生成 OpenStreamingAssets.bat，双击打开包体 StreamingAssets，
        /// 便于部署后直接打开资源目录，无需进入 _Data 子目录。使用相对路径，随包分发到任意电脑/目录均可用。
        /// </summary>
        private void CreateSteamingAssetsShortcutToBuildDirectory(string pathToBuiltProject)
        {
            if (string.IsNullOrWhiteSpace(pathToBuiltProject))
            {
                Debug.LogWarning("[BuildModule] 跳过创建SteamingAssets快捷方式：pathToBuiltProject 为空。");
                return;
            }

            string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
            if (string.IsNullOrWhiteSpace(buildDirectory) || !Directory.Exists(buildDirectory))
            {
                Debug.LogWarning($"[BuildModule] 跳过创建SteamingAssets快捷方式：构建目录无效，pathToBuiltProject={pathToBuiltProject}");
                return;
            }

            string streamingAssetsPath = GetPlayerStreamingAssetsDirectory(pathToBuiltProject, buildDirectory);
            if (string.IsNullOrWhiteSpace(streamingAssetsPath))
            {
                Debug.LogWarning("[BuildModule] 跳过创建SteamingAssets快捷方式：无法解析包体 StreamingAssets 目录。");
                return;
            }

            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
                Debug.Log($"[BuildModule] 包体 StreamingAssets 目录不存在，已创建：{streamingAssetsPath}");
            }

            // 清理旧版本生成的 .lnk（存的是构建机绝对路径，随包分发后会失效或指向错误目录）
            string legacyShortcutPath = Path.Combine(buildDirectory, "StreamingAssets.lnk");
            if (File.Exists(legacyShortcutPath))
            {
                File.Delete(legacyShortcutPath);
                Debug.Log($"[BuildModule] 已删除旧版 StreamingAssets 快捷方式：{legacyShortcutPath}");
            }

            // %~dp0 = bat 自身所在目录（带结尾反斜杠），因此包体移动/复制/换电脑后仍然有效。
            // 用 *_Data 通配而不是写死 {产品名}_Data：bat 内容保持纯 ASCII，避免中文产品名在 cmd 代码页（GBK/UTF-8）下乱码。
            // ponytail: 若构建根目录存在多个 *_Data 文件夹，会逐个打开；Unity Windows 包体正常只有一个。
            string batPath = Path.Combine(buildDirectory, "OpenStreamingAssets.bat");
            string batContent =
                "@echo off\r\n" +
                "for /d %%D in (\"%~dp0*_Data\") do start \"\" \"%%~fD\\StreamingAssets\"\r\n";
            File.WriteAllText(batPath, batContent, Encoding.ASCII);

            Debug.Log($"[BuildModule] 已创建 StreamingAssets 打开脚本：{batPath} -> {streamingAssetsPath}");
        }
        /// <summary>
        /// Windows 构建完成后，将 daemon-run.bat 复制到构建目录（exe 同级目录）。
        /// 优先通过 Package Manager API 解析包真实路径，再回退到多个候选路径，
        /// 兼容插件放在 Packages/Assets/工程根目录等不同场景。
        /// </summary>
        private void CopyDaemonRunBatToWindowsBuildDirectory(string pathToBuiltProject)
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

        private void IncrementIOSBuildNumber()
        {
            string oldBuildNumber = PlayerSettings.iOS.buildNumber;
            PlayerSettings.iOS.buildNumber = (int.Parse(PlayerSettings.iOS.buildNumber) + 1).ToString();
            Debug.Log($"Build ID从{oldBuildNumber}自增到{PlayerSettings.iOS.buildNumber}");
        }

        private void IncrementPCVersionNumber()
        {
            string oldVersion = PlayerSettings.bundleVersion;
            int lastIndex = oldVersion.LastIndexOf('.');
            string majorVersion = oldVersion.Substring(0, lastIndex);
            string minorVersion = oldVersion.Substring(lastIndex + 1);
            minorVersion = (int.Parse(minorVersion) + 1).ToString();
            PlayerSettings.bundleVersion = $"{majorVersion}.{minorVersion}";
            Debug.Log($"[BuildModule] 版本号从{oldVersion}自增到{PlayerSettings.bundleVersion}");
        }

        #endregion

        #region 场景管理

        private static void SceneManage_AddCurrSceneToBuildSetting()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            string currScenePath = SceneManager.GetActiveScene().path;
            int count = scenes.Count(scene => scene.path == currScenePath);
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
        private static void SceneManage_SetBuildSceneEnable(bool onlyCurrentScene)
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
    }
}