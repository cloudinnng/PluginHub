using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    /// <summary>
    /// 按当前打开场景名配置 StreamingAssets 一级子文件夹保留/排除。
    /// 全局开关开启时，所有经 ExecuteBuild 的构建都会：构建前 Move 未保留目录到 Library 临时区，构建结束（含失败）再还原。
    /// </summary>
    public partial class BuildModule : PluginHubModuleBase
    {
        #region 配置与状态

        private const string ExcludedFoldersConfigKey = "excludedStreamingAssetsFolders";
        private const char ExcludedFoldersSeparator = '|';

        /// <summary>全局开关：关掉后即使场景构建也不做隔离。</summary>
        private static bool enableStreamingAssetsExclude
        {
            get => EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_enableStreamingAssetsExclude", false);
            set => EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_enableStreamingAssetsExclude", value);
        }

        /// <summary>本次构建使用的临时隔离会话目录；null 表示未隔离。</summary>
        private static string _parkSessionPath;

        /// <summary>临时根目录：ProjectRoot/Library/PluginHub_StreamingAssetsPark/</summary>
        private static string ParkRootPath =>
            Path.Combine(PluginHubEditor.ProjectRoot, "Library", "PluginHub_StreamingAssetsPark");

        /// <summary>与 sceneBuildName 同一 section：BuildModule_{sceneName}</summary>
        private static string GetExcludeConfigSectionForScene(string sceneName)
        {
            return $"BuildModule_{sceneName}";
        }

        #endregion

        #region 配置读写

        /// <summary>
        /// 读取指定场景的排除文件夹名列表（仅文件夹名，不含路径）。
        /// </summary>
        private static List<string> ReadExcludedStreamingAssetsFolders(string sceneName)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(sceneName))
                return result;

            string raw = PluginHubConfig.ReadConfig(
                GetExcludeConfigSectionForScene(sceneName),
                ExcludedFoldersConfigKey,
                "");
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            string[] parts = raw.Split(new[] { ExcludedFoldersSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string name = part.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!result.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
                    result.Add(name);
            }

            return result;
        }

        /// <summary>
        /// 将排除列表写入当前场景对应的 PluginHubConfig section。
        /// </summary>
        private static void WriteExcludedStreamingAssetsFolders(string sceneName, List<string> folderNames)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[BuildModule] 场景名为空，无法写入 StreamingAssets 排除配置。");
                return;
            }

            List<string> cleaned = (folderNames ?? new List<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string value = string.Join(ExcludedFoldersSeparator.ToString(), cleaned);
            PluginHubConfig.WriteConfig(
                GetExcludeConfigSectionForScene(sceneName),
                ExcludedFoldersConfigKey,
                value);
            Debug.Log($"[BuildModule] 已保存场景「{sceneName}」StreamingAssets 排除列表：{(string.IsNullOrEmpty(value) ? "(空)" : value)}");
        }

        #endregion

        #region UI

        /// <summary>
        /// 更多选项：按当前打开场景勾选要保留的 SA 一级子文件夹（未勾选 = 构建时临时挪走）。
        /// 底层仍持久化为排除列表，默认全保留。
        /// </summary>
        private void DrawStreamingAssetsExcludeUI()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                EditorGUILayout.HelpBox("当前无打开场景，无法编辑按场景保留配置。", MessageType.Warning);
                return;
            }

            GUILayout.Label($"配置场景: {sceneName}（section=BuildModule_{sceneName}）", EditorStyles.miniLabel);

            string streamingAssetsPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                EditorGUILayout.HelpBox($"StreamingAssets 目录不存在：{streamingAssetsPath}", MessageType.Info);
                return;
            }

            string[] folderPaths = Directory.GetDirectories(streamingAssetsPath);
            if (folderPaths.Length == 0)
            {
                EditorGUILayout.HelpBox("StreamingAssets 下没有一级子文件夹。", MessageType.Info);
                return;
            }

            List<string> excluded = ReadExcludedStreamingAssetsFolders(sceneName);
            bool changed = false;

            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("勾选 = 保留在 StreamingAssets（进入包体）；取消勾选 = 场景构建时临时挪走", EditorStyles.miniLabel);
                foreach (string folderPath in folderPaths.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
                {
                    string folderName = Path.GetFileName(folderPath);
                    // UI：勾选=保留；配置：存的是排除列表，二者取反
                    bool isKept = !excluded.Any(n => string.Equals(n, folderName, StringComparison.OrdinalIgnoreCase));
                    bool newKept = EditorGUILayout.ToggleLeft(folderName, isKept);
                    if (newKept == isKept)
                        continue;

                    changed = true;
                    if (newKept)
                    {
                        excluded.RemoveAll(n => string.Equals(n, folderName, StringComparison.OrdinalIgnoreCase));
                        Debug.Log($"[BuildModule] 场景「{sceneName}」保留 StreamingAssets/{folderName}");
                    }
                    else
                    {
                        excluded.Add(folderName);
                        Debug.Log($"[BuildModule] 场景「{sceneName}」不保留（将排除）StreamingAssets/{folderName}");
                    }
                }
            }
            GUILayout.EndVertical();

            if (changed)
                WriteExcludedStreamingAssetsFolders(sceneName, excluded);
        }

        #endregion

        #region 隔离 / 还原

        /// <summary>
        /// 构建预处理：先还原残留会话，再按需隔离当前打开场景配置的排除目录。
        /// 仅受全局开关 enableStreamingAssetsExclude 控制（所有构建入口统一走这里）。
        /// </summary>
        /// <returns>失败时返回 false，取消构建。</returns>
        private bool ApplyStreamingAssetsExcludeIfNeeded()
        {
            // 启动兜底：上次崩溃可能导致 park 残留
            RecoverLeftoverParkSessions();

            if (!enableStreamingAssetsExclude)
            {
                Debug.Log("[BuildModule] StreamingAssets 按场景筛选开关关闭，跳过隔离。");
                return true;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[BuildModule] 当前场景名为空，跳过 StreamingAssets 排除。");
                return true;
            }

            List<string> excludedFolders = ReadExcludedStreamingAssetsFolders(sceneName);
            if (excludedFolders.Count == 0)
            {
                Debug.Log($"[BuildModule] 场景「{sceneName}」未配置排除文件夹，跳过隔离。");
                return true;
            }

            string streamingAssetsPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                Debug.LogWarning($"[BuildModule] StreamingAssets 不存在，跳过隔离：{streamingAssetsPath}");
                return true;
            }

            string sessionPath = Path.Combine(ParkRootPath, DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
            try
            {
                Directory.CreateDirectory(sessionPath);
                _parkSessionPath = sessionPath;
                Debug.Log($"[BuildModule] 开始隔离 StreamingAssets 排除目录 → {sessionPath}，场景={sceneName}，共 {excludedFolders.Count} 项");

                int movedCount = 0;
                foreach (string folderName in excludedFolders)
                {
                    // 仅允许一级文件夹名，拒绝路径穿越
                    if (folderName.IndexOfAny(new[] { '/', '\\' }) >= 0 ||
                        folderName == "." || folderName == "..")
                    {
                        Debug.LogWarning($"[BuildModule] 非法排除项，已跳过：{folderName}");
                        continue;
                    }

                    string sourceDir = Path.Combine(streamingAssetsPath, folderName);
                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.LogWarning($"[BuildModule] 排除目录不存在，跳过：StreamingAssets/{folderName}");
                        continue;
                    }

                    string destDir = Path.Combine(sessionPath, folderName);
                    Debug.Log($"[BuildModule] Move 隔离：{sourceDir} → {destDir}");
                    Directory.Move(sourceDir, destDir);
                    movedCount++;

                    string sourceMeta = sourceDir + ".meta";
                    string destMeta = destDir + ".meta";
                    if (File.Exists(sourceMeta))
                    {
                        File.Move(sourceMeta, destMeta);
                        Debug.Log($"[BuildModule] Move 隔离 meta：{sourceMeta} → {destMeta}");
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log($"[BuildModule] StreamingAssets 隔离完成，实际 Move {movedCount} 个文件夹。");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildModule] StreamingAssets 隔离失败，将取消构建并尝试还原。{e.Message}");
                // finally 也会还原；此处立即还原便于预处理返回 false 前状态干净
                RestoreParkedStreamingAssetsIfNeeded();
                return false;
            }
        }

        /// <summary>
        /// 还原本次会话（若有）。挂在 ExecuteBuild 的 finally。
        /// </summary>
        private static void RestoreParkedStreamingAssetsIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(_parkSessionPath))
                return;

            string sessionPath = _parkSessionPath;
            _parkSessionPath = null;
            RestoreParkSession(sessionPath);
        }

        /// <summary>
        /// 发现 park 根下残留会话时全部还原（防上次崩溃未还原）。
        /// </summary>
        private static void RecoverLeftoverParkSessions()
        {
            if (!Directory.Exists(ParkRootPath))
                return;

            string[] sessions = Directory.GetDirectories(ParkRootPath);
            if (sessions.Length == 0)
                return;

            Debug.LogWarning($"[BuildModule] 发现 {sessions.Length} 个残留 StreamingAssets 隔离会话，开始兜底还原。");
            foreach (string session in sessions)
            {
                // 跳过当前正在使用的会话（理论上预处理开头尚无当前会话）
                if (!string.IsNullOrWhiteSpace(_parkSessionPath) &&
                    string.Equals(Path.GetFullPath(session), Path.GetFullPath(_parkSessionPath), StringComparison.OrdinalIgnoreCase))
                    continue;

                RestoreParkSession(session);
            }
        }

        /// <summary>
        /// 将单个会话目录内的文件夹与 .meta Move 回 StreamingAssets，并删除会话目录。
        /// </summary>
        private static void RestoreParkSession(string sessionPath)
        {
            if (string.IsNullOrWhiteSpace(sessionPath) || !Directory.Exists(sessionPath))
            {
                Debug.Log($"[BuildModule] 隔离会话不存在，无需还原：{sessionPath}");
                return;
            }

            string streamingAssetsPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
                Debug.Log($"[BuildModule] 还原前创建 StreamingAssets：{streamingAssetsPath}");
            }

            Debug.Log($"[BuildModule] 开始还原隔离会话：{sessionPath}");

            // 先还原目录，再还原 .meta（同目录下的条目）
            string[] entries = Directory.GetFileSystemEntries(sessionPath);
            foreach (string entry in entries)
            {
                string name = Path.GetFileName(entry);
                string dest = Path.Combine(streamingAssetsPath, name);

                try
                {
                    if (Directory.Exists(entry))
                    {
                        if (Directory.Exists(dest))
                        {
                            Debug.LogWarning($"[BuildModule] 还原跳过：目标已存在目录 {dest}（会话内：{entry}）");
                            continue;
                        }

                        Directory.Move(entry, dest);
                        Debug.Log($"[BuildModule] 已还原目录：{dest}");
                    }
                    else if (File.Exists(entry))
                    {
                        // .meta 若已被 Unity 重新生成，先删再移回，避免 Move 失败
                        if (File.Exists(dest))
                        {
                            File.Delete(dest);
                            Debug.Log($"[BuildModule] 还原前删除已存在文件：{dest}");
                        }

                        File.Move(entry, dest);
                        Debug.Log($"[BuildModule] 已还原文件：{dest}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BuildModule] 还原失败：{entry} → {dest}，{e.Message}");
                }
            }

            try
            {
                // 若仍有残留（目标冲突），保留会话目录便于人工处理
                string[] leftover = Directory.GetFileSystemEntries(sessionPath);
                if (leftover.Length == 0)
                {
                    Directory.Delete(sessionPath, false);
                    Debug.Log($"[BuildModule] 已删除空隔离会话：{sessionPath}");
                }
                else
                {
                    Debug.LogWarning($"[BuildModule] 隔离会话仍有 {leftover.Length} 项未还原，保留目录：{sessionPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BuildModule] 删除隔离会话失败：{sessionPath}，{e.Message}");
            }

            AssetDatabase.Refresh();
            Debug.Log("[BuildModule] StreamingAssets 隔离还原流程结束。");
        }

        #endregion
    }
}
