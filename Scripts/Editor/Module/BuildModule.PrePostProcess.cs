using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using PluginHub.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PluginHub.Editor
{
    public partial class BuildModule : PluginHubModuleBase, IPreprocessBuildWithReport
    {
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
                CreateSteamingAssetsShortcutToBuildDirectory(pathToBuiltProject);// 创建SteamingAssets快捷方式到构建根目录
                ExecutePostCopyFolders(buildTarget, pathToBuiltProject);// 执行构建后复制文件夹到构建目录
                if (autoZipAfterBuild)
                {
                    string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
                    Debug.Log($"[BuildModule] 构建后自动打包，目录: {buildDirectory}");
                    ZipBuildDirectoryAndCopyToClipboard(buildDirectory);
                }
            }
        }

        /// <summary>
        /// Windows 构建完成后，在 exe 同级目录创建指向包体 StreamingAssets 的快捷方式（StreamingAssets.lnk），
        /// 便于部署后直接打开资源目录，无需进入 _Data 子目录。
        /// </summary>
        private static void CreateSteamingAssetsShortcutToBuildDirectory(string pathToBuiltProject)
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

            string shortcutPath = Path.Combine(buildDirectory, "StreamingAssets.lnk");
            string description = $"打开 {Path.GetFileNameWithoutExtension(pathToBuiltProject)} 的 StreamingAssets 文件夹";
            if (!TryCreateWindowsFolderShortcut(shortcutPath, streamingAssetsPath, description))
            {
                Debug.LogWarning($"[BuildModule] 创建 StreamingAssets 快捷方式失败：{shortcutPath}");
                return;
            }

            Debug.Log($"[BuildModule] 已创建 StreamingAssets 快捷方式：{shortcutPath} -> {streamingAssetsPath}");
        }

        /// <summary>
        /// 通过 PowerShell 调用 WScript.Shell 在 Windows 上创建文件夹快捷方式（.lnk）。
        /// ponytail: Unity 进程内 COM 激活会报 Unmanaged activation is not supported，故外包给 PowerShell。
        /// </summary>
        private static bool TryCreateWindowsFolderShortcut(string shortcutFilePath, string targetFolderPath, string description)
        {
            if (string.IsNullOrWhiteSpace(shortcutFilePath) || string.IsNullOrWhiteSpace(targetFolderPath))
            {
                Debug.LogWarning("[BuildModule] 创建快捷方式失败：shortcutFilePath 或 targetFolderPath 为空。");
                return false;
            }

            try
            {
                string shortcutArg = EscapePowerShellSingleQuotedString(shortcutFilePath);
                string targetArg = EscapePowerShellSingleQuotedString(targetFolderPath);
                string descriptionArg = EscapePowerShellSingleQuotedString(description ?? string.Empty);

                // 使用 -EncodedCommand 避免中文路径、空格、引号在命令行中转义出错
                // 注意：不能用 C# 插值字符串 $"..."，否则 $s/$ws 会被当成 C# 变量吞掉
                string psCommand =
                    "$ws = New-Object -ComObject WScript.Shell; " +
                    "$s = $ws.CreateShortcut('" + shortcutArg + "'); " +
                    "$s.TargetPath = '" + targetArg + "'; " +
                    "$s.Description = '" + descriptionArg + "'; " +
                    "$s.Save()";
                string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(psCommand));

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Debug.LogWarning($"[BuildModule] PowerShell 创建快捷方式失败，ExitCode={process.ExitCode}，stderr={error}");
                        return false;
                    }
                }

                bool created = File.Exists(shortcutFilePath);
                if (!created)
                    Debug.LogWarning($"[BuildModule] 快捷方式保存后未找到文件：{shortcutFilePath}");
                return created;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildModule] 创建快捷方式异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>PowerShell 单引号字符串转义：' → ''</summary>
        private static string EscapePowerShellSingleQuotedString(string value)
        {
            return value.Replace("'", "''");
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
    }
}