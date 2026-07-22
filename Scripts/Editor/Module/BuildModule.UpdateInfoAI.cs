using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PluginHub.Editor
{
    /// <summary>
    /// 构建模块：用本机 Cursor Agent（无头 ask）根据 git 整仓 diff 润色「更新内容」。
    /// </summary>
    public partial class BuildModule
    {
        #region UpdateInfo AI 状态

        /// <summary>是否正在异步生成更新说明（入口按钮忙碌态）。</summary>
        private static bool isGeneratingUpdateInfo;

        /// <summary>项目根目录（含 .git 的仓库根）。</summary>
        private static string GitRepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        /// <summary>喂给 Agent 的 diff 文本最大长度（字符）。prompt 经临时文件传入，可适当放大。</summary>
        private const int MaxDiffCharsForAgent = 30 * 1024;

        /// <summary>区间选择窗口拉取的 commit 条数。</summary>
        private const int CommitListLimit = 80;

        // 后台线程完成后写入，由 EditorApplication.update 在主线程消费（Unity API 不可跨线程调用）
        private static readonly object generateResultLock = new object();
        private static bool generateResultReady;
        private static string generateResultText;
        private static string generateResultError;

        #endregion

        #region UpdateInfo AI 入口回调

        /// <summary>
        /// 区间选择确认后：启动异步生成。
        /// </summary>
        private static void OnUpdateInfoCommitRangeConfirmed(string fromHash, string toHash)
        {
            if (isGeneratingUpdateInfo)
            {
                Debug.LogWarning("[BuildModule.UpdateInfoAI] 已有生成任务进行中，忽略本次请求");
                return;
            }

            if (string.IsNullOrWhiteSpace(fromHash) || string.IsNullOrWhiteSpace(toHash))
            {
                EditorUtility.DisplayDialog("更新内容 AI", "请选择有效的 From / To commit。", "确定");
                return;
            }

            if (string.Equals(fromHash, toHash, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("更新内容 AI", "From 与 To 不能是同一个 commit。", "确定");
                return;
            }

            Debug.Log($"[BuildModule.UpdateInfoAI] 开始生成: {fromHash}..{toHash}");
            isGeneratingUpdateInfo = true;
            lock (generateResultLock)
            {
                generateResultReady = false;
                generateResultText = null;
                generateResultError = null;
            }

            // 主线程轮询结果，避免从后台线程触碰 EditorApplication
            EditorApplication.update -= PollGenerateUpdateInfoResult;
            EditorApplication.update += PollGenerateUpdateInfoResult;

            string repoRoot = GitRepoRoot;
            Task.Run(() =>
            {
                string error = null;
                string resultText = null;
                try
                {
                    // 1) 探测 agent 是否可用
                    if (!TryResolveAgentExecutable(out string agentExe, out string resolveErr))
                    {
                        error = resolveErr;
                        return;
                    }

                    // 2) 采集整仓 diff（stat + name-status + 截断 patch）
                    string diffPayload = CollectRepoDiffForAgent(repoRoot, fromHash, toHash, out bool hasChanges);
                    if (!hasChanges)
                    {
                        error = "该区间内没有可解析的 diff（可能无变更）。";
                        return;
                    }

                    // 3) 短 prompt 内嵌裁剪后的 diff（避开依赖 ask 读文件；并控制命令行长度）
                    string prompt = BuildUpdateInfoPrompt(fromHash, toHash, diffPayload);
                    Debug.Log($"[BuildModule.UpdateInfoAI] 调用 agent: {agentExe}, promptLen={prompt.Length}");

                    if (!TryRunCursorAgentAsk(agentExe, repoRoot, prompt, out resultText, out string agentErr))
                    {
                        error = agentErr;
                        return;
                    }

                    resultText = SanitizeAgentUpdateInfo(resultText);
                    if (string.IsNullOrWhiteSpace(resultText))
                    {
                        error = "Agent 返回为空，请检查 agent 登录状态（agent status / agent login）。";
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    Debug.LogException(ex);
                }
                finally
                {
                    lock (generateResultLock)
                    {
                        generateResultText = resultText;
                        generateResultError = error;
                        generateResultReady = true;
                    }
                }
            });
        }

        /// <summary>
        /// 主线程轮询：消费后台生成结果；生成中持续 Repaint 以刷新「生成中…」按钮。
        /// </summary>
        private static void PollGenerateUpdateInfoResult()
        {
            // 生成过程中刷新 PluginHub 窗口，让入口按钮文案从「AI」变为「生成中…」
            if (isGeneratingUpdateInfo)
            {
                PluginHubWindow[] hubs = Resources.FindObjectsOfTypeAll<PluginHubWindow>();
                for (int i = 0; i < hubs.Length; i++)
                {
                    if (hubs[i] != null)
                        hubs[i].Repaint();
                }
            }

            string resultText;
            string error;
            lock (generateResultLock)
            {
                if (!generateResultReady)
                    return;
                resultText = generateResultText;
                error = generateResultError;
                generateResultReady = false;
            }

            EditorApplication.update -= PollGenerateUpdateInfoResult;
            FinishGenerateUpdateInfo(resultText, error);
        }

        /// <summary>
        /// 主线程收尾：写回 updateInfo 或报错。
        /// </summary>
        private static void FinishGenerateUpdateInfo(string resultText, string error)
        {
            isGeneratingUpdateInfo = false;

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[BuildModule.UpdateInfoAI] 生成失败: {error}");
                EditorUtility.DisplayDialog("更新内容 AI", $"生成失败：\n{error}\n\n若提示找不到 agent，请确认已安装 Cursor Agent 并已登录（agent login）。", "确定");
                return;
            }

            // 直接写 EditorPrefs（与 updateInfo 属性同一 key），避免依赖模块实例
            string prefsKey = $"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_updateInfo";
            EditorPrefs.SetString(prefsKey, resultText.Trim());
            Debug.Log($"[BuildModule.UpdateInfoAI] 已写入更新内容，长度={resultText.Trim().Length}");
            EditorUtility.DisplayDialog("更新内容 AI", "已生成并填入「更新内容」，请检查后按需微调。", "确定");
        }

        #endregion

        #region Git 辅助

        /// <summary>
        /// 拉取最近 commit 列表（hash + 单行说明），供区间选择窗口使用。
        /// </summary>
        internal static List<GitCommitItem> FetchRecentCommits(int limit = CommitListLimit)
        {
            List<GitCommitItem> list = new List<GitCommitItem>();
            string output = RunGitCapture(GitRepoRoot, $"log --oneline -n {limit}");
            if (string.IsNullOrWhiteSpace(output))
                return list;

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length < 8)
                    continue;

                int spaceIdx = line.IndexOf(' ');
                if (spaceIdx <= 0)
                {
                    list.Add(new GitCommitItem { ShortHash = line, Subject = "" });
                    continue;
                }

                list.Add(new GitCommitItem
                {
                    ShortHash = line.Substring(0, spaceIdx),
                    Subject = line.Substring(spaceIdx + 1).Trim()
                });
            }

            Debug.Log($"[BuildModule.UpdateInfoAI] 拉取 commit 列表: {list.Count} 条");
            return list;
        }

        /// <summary>
        /// 采集整仓 A..B 的 --stat / --name-status / 截断 patch，拼成 Agent 输入。
        /// </summary>
        private static string CollectRepoDiffForAgent(string repoRoot, string fromHash, string toHash, out bool hasChanges)
        {
            string range = $"{fromHash}..{toHash}";
            Debug.Log($"[BuildModule.UpdateInfoAI] 采集 diff: {range}");

            string stat = RunGitCapture(repoRoot, $"diff --stat {range}");
            string nameStatus = RunGitCapture(repoRoot, $"diff --name-status {range}");
            // 尽量减少二进制噪音：排除常见媒体/库后缀（pathspec exclude）
            string patch = RunGitCapture(repoRoot,
                $"diff {range} -- . " +
                "\":(exclude)*.png\" \":(exclude)*.jpg\" \":(exclude)*.jpeg\" \":(exclude)*.gif\" " +
                "\":(exclude)*.mp4\" \":(exclude)*.mov\" \":(exclude)*.wav\" \":(exclude)*.mp3\" " +
                "\":(exclude)*.fbx\" \":(exclude)*.dll\" \":(exclude)*.so\" \":(exclude)*.a\" " +
                "\":(exclude)*.zip\" \":(exclude)*.7z\" \":(exclude)*.psd\" \":(exclude)*.tga\"");

            hasChanges = !string.IsNullOrWhiteSpace(stat)
                         || !string.IsNullOrWhiteSpace(nameStatus)
                         || !string.IsNullOrWhiteSpace(patch);

            StringBuilder sb = new StringBuilder(Math.Min(MaxDiffCharsForAgent + 2048, 64 * 1024));
            sb.AppendLine("### git diff --stat");
            sb.AppendLine(string.IsNullOrWhiteSpace(stat) ? "(empty)" : stat.Trim());
            sb.AppendLine();
            sb.AppendLine("### git diff --name-status");
            sb.AppendLine(string.IsNullOrWhiteSpace(nameStatus) ? "(empty)" : nameStatus.Trim());
            sb.AppendLine();
            sb.AppendLine("### git diff (text patch, may be truncated; binary/media excluded)");

            if (string.IsNullOrWhiteSpace(patch))
            {
                sb.AppendLine("(empty patch — rely on stat / name-status)");
            }
            else
            {
                string trimmedPatch = patch.Trim();
                // 预留给 stat/name-status 的空间
                int headerLen = sb.Length;
                int patchBudget = Math.Max(2048, MaxDiffCharsForAgent - headerLen);
                if (trimmedPatch.Length > patchBudget)
                {
                    sb.Append(trimmedPatch, 0, patchBudget);
                    sb.AppendLine();
                    sb.AppendLine($"...[truncated, original patch length={trimmedPatch.Length}]");
                    Debug.Log($"[BuildModule.UpdateInfoAI] patch 已截断: {trimmedPatch.Length} -> {patchBudget}");
                }
                else
                {
                    sb.AppendLine(trimmedPatch);
                }
            }

            string payload = sb.ToString();
            Debug.Log($"[BuildModule.UpdateInfoAI] diff payload 长度={payload.Length}, hasChanges={hasChanges}");
            return payload;
        }

        /// <summary>
        /// 同步执行 git 并捕获 stdout（在后台线程调用；勿在主线程跑重 diff）。
        /// </summary>
        private static string RunGitCapture(string repoRoot, string gitArgs)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{repoRoot}\" {gitArgs}",
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(120000);

                if (process.ExitCode != 0)
                {
                    Debug.LogWarning($"[BuildModule.UpdateInfoAI] git 退出码={process.ExitCode}, args={gitArgs}, stderr={stderr}");
                }

                return stdout ?? "";
            }
        }

        #endregion

        #region Cursor Agent 调用

        /// <summary>
        /// 解析本机 agent 可执行文件（优先 agent.ps1，因 .cmd 对多行 prompt 传参易丢内容）。
        /// </summary>
        private static bool TryResolveAgentExecutable(out string agentPath, out string error)
        {
            agentPath = null;
            error = null;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] preferredCandidates =
            {
                Path.Combine(localAppData, "cursor-agent", "agent.ps1"),
                Path.Combine(localAppData, "cursor-agent", "agent.cmd"),
                Path.Combine(localAppData, "cursor-agent", "agent.exe"),
            };
            for (int i = 0; i < preferredCandidates.Length; i++)
            {
                if (File.Exists(preferredCandidates[i]))
                {
                    agentPath = PreferPs1Sibling(preferredCandidates[i]);
                    Debug.Log($"[BuildModule.UpdateInfoAI] 通过候选路径找到 agent: {agentPath}");
                    return true;
                }
            }

            // where agent：若命中 .cmd，尝试同目录 .ps1
            string whereOut = RunProcessCapture("where", "agent", null, 5000);
            if (!string.IsNullOrWhiteSpace(whereOut))
            {
                string[] lines = whereOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string p = lines[i].Trim();
                    if (!File.Exists(p))
                        continue;
                    agentPath = PreferPs1Sibling(p);
                    Debug.Log($"[BuildModule.UpdateInfoAI] 通过 where 找到 agent: {agentPath}");
                    return true;
                }
            }

            error = "找不到本机 Cursor Agent（agent）。请确认已安装并在 PATH 中，或先运行 agent login。";
            return false;
        }

        /// <summary>
        /// 若存在同目录 agent.ps1，优先使用（PowerShell 传多行参数更可靠）。
        /// </summary>
        private static string PreferPs1Sibling(string agentPath)
        {
            if (string.IsNullOrEmpty(agentPath))
                return agentPath;
            if (agentPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                return agentPath;

            string dir = Path.GetDirectoryName(agentPath);
            if (string.IsNullOrEmpty(dir))
                return agentPath;

            string ps1 = Path.Combine(dir, "agent.ps1");
            if (File.Exists(ps1))
            {
                Debug.Log($"[BuildModule.UpdateInfoAI] 改用同目录 agent.ps1: {ps1}");
                return ps1;
            }

            return agentPath;
        }

        /// <summary>
        /// 调用 agent -p --mode ask --trust。
        /// prompt 经临时 UTF-8 文件由 PowerShell Get-Content 读入再传给 agent，避开 cmd 转义与长度限制。
        /// </summary>
        private static bool TryRunCursorAgentAsk(string agentExe, string workspace, string prompt, out string stdout, out string error)
        {
            stdout = null;
            error = null;

            string promptFile = Path.Combine(Path.GetTempPath(), $"BuildModule_UpdateInfoPrompt_{Guid.NewGuid():N}.txt");
            File.WriteAllText(promptFile, prompt, new UTF8Encoding(false));
            Debug.Log($"[BuildModule.UpdateInfoAI] prompt 文件: {promptFile}");

            try
            {
                // 用 Base64 EncodedCommand，彻底避开引号嵌套
                string psInner =
                    "$ErrorActionPreference='Stop';\n" +
                    $"$prompt = Get-Content -LiteralPath '{promptFile.Replace("'", "''")}' -Raw -Encoding UTF8;\n" +
                    $"Set-Location -LiteralPath '{workspace.Replace("'", "''")}';\n" +
                    $"& '{agentExe.Replace("'", "''")}' -p --mode ask --trust --workspace '{workspace.Replace("'", "''")}' --output-format text -- $prompt\n";

                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(psInner));
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}",
                    WorkingDirectory = workspace,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    Task<string> outTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errTask = process.StandardError.ReadToEndAsync();
                    bool exited = process.WaitForExit(10 * 60 * 1000);
                    if (!exited)
                    {
                        try { process.Kill(); } catch { /* ignore */ }
                        error = "Agent 执行超时（>10 分钟）。";
                        return false;
                    }

                    stdout = outTask.Result ?? "";
                    string stderr = errTask.Result ?? "";
                    Debug.Log($"[BuildModule.UpdateInfoAI] agent exit={process.ExitCode}, stdoutLen={stdout.Length}, stderrLen={stderr.Length}");
                    if (!string.IsNullOrWhiteSpace(stderr))
                        Debug.Log($"[BuildModule.UpdateInfoAI] agent stderr:\n{stderr}");

                    if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
                    {
                        error = $"Agent 退出码={process.ExitCode}。stderr:\n{stderr}\n请检查 agent status / agent login。";
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                error = $"启动 Agent 失败: {ex.Message}";
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(promptFile))
                        File.Delete(promptFile);
                }
                catch (Exception delEx)
                {
                    Debug.LogWarning($"[BuildModule.UpdateInfoAI] 删除临时 prompt 失败: {delEx.Message}");
                }
            }
        }

        /// <summary>
        /// 组装 Agent prompt（含裁剪后的 diff 正文）。
        /// </summary>
        private static string BuildUpdateInfoPrompt(string fromHash, string toHash, string diffPayload)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("你是发布说明助手。根据下面的 git diff，写一段给「非开发人员」看的更新内容。");
            sb.AppendLine("硬性要求：");
            sb.AppendLine("1. 使用简体中文；");
            sb.AppendLine("2. 压成 3～5 条短 bullet（每条一行，以「- 」开头）；");
            sb.AppendLine("3. 只写用户能感知的变化（功能、界面、体验、内容），不要写重构/依赖/工程配置细节；");
            sb.AppendLine("4. 只能依据提供的 diff，禁止编造 diff 中没有的内容；");
            sb.AppendLine("5. 若没有明显用户可见变更，只输出一行：- 本次无明显用户可见更新；");
            sb.AppendLine("6. 不要输出前言、标题、代码块或解释，只输出 bullet 列表本身。");
            sb.AppendLine();
            sb.AppendLine($"Commit 区间: {fromHash}..{toHash}");
            sb.AppendLine();
            sb.AppendLine(diffPayload);
            return sb.ToString();
        }

        /// <summary>
        /// 去掉 Agent 可能夹带的 markdown 围栏等杂质。
        /// </summary>
        private static string SanitizeAgentUpdateInfo(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string text = raw.Trim();
            // 去掉 ``` 代码块包裹
            if (text.StartsWith("```"))
            {
                int firstNl = text.IndexOf('\n');
                if (firstNl >= 0)
                    text = text.Substring(firstNl + 1);
                int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0)
                    text = text.Substring(0, lastFence);
                text = text.Trim();
            }

            return text;
        }

        /// <summary>
        /// 轻量进程捕获（用于 where 等短命令）。
        /// </summary>
        private static string RunProcessCapture(string fileName, string arguments, string workingDirectory, int timeoutMs)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments ?? "",
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string stdout = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(timeoutMs);
                    return stdout ?? "";
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildModule.UpdateInfoAI] RunProcessCapture 失败: {fileName} {arguments}, {ex.Message}");
                return "";
            }
        }

        #endregion

        #region 数据类型

        /// <summary>git log --oneline 解析结果。</summary>
        internal struct GitCommitItem
        {
            public string ShortHash;
            public string Subject;

            public string DisplayLabel => string.IsNullOrEmpty(Subject) ? ShortHash : $"{ShortHash}  {Subject}";
        }

        #endregion
    }

    /// <summary>
    /// 手选 git commit 区间，确认后回调生成更新说明。
    /// </summary>
    public class UpdateInfoCommitRangeWindow : EditorWindow
    {
        #region 窗口状态

        private List<BuildModule.GitCommitItem> commits = new List<BuildModule.GitCommitItem>();
        private string[] displayLabels = Array.Empty<string>();
        private int fromIndex;
        private int toIndex;
        private Vector2 scroll;
        private Action<string, string> onConfirm;
        private string loadError;

        #endregion

        #region 打开 / 绘制

        public static void Open(Action<string, string> onConfirm)
        {
            UpdateInfoCommitRangeWindow window = GetWindow<UpdateInfoCommitRangeWindow>(true, "选择 Commit 区间", true);
            window.minSize = new Vector2(520, 360);
            window.onConfirm = onConfirm;
            window.ReloadCommits();
            window.Show();
        }

        private void ReloadCommits()
        {
            loadError = null;
            try
            {
                commits = BuildModule.FetchRecentCommits();
                if (commits == null || commits.Count == 0)
                {
                    loadError = "未能读取 git log。请确认工程根目录是 git 仓库。";
                    displayLabels = Array.Empty<string>();
                    return;
                }

                displayLabels = new string[commits.Count];
                for (int i = 0; i < commits.Count; i++)
                    displayLabels[i] = commits[i].DisplayLabel;

                // To = 最新(HEAD)，From = 尽量取第 6 条（约 HEAD~5），不足则取最后一条
                toIndex = 0;
                fromIndex = Math.Min(5, commits.Count - 1);
                if (fromIndex == toIndex && commits.Count > 1)
                    fromIndex = 1;

                Debug.Log($"[BuildModule.UpdateInfoAI] 区间窗口默认 From={commits[fromIndex].ShortHash}, To={commits[toIndex].ShortHash}");
            }
            catch (Exception ex)
            {
                loadError = ex.Message;
                Debug.LogException(ex);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("选择更新说明对应的 git 区间（整仓 diff）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "From = 旧提交（起点，不含），To = 新提交（终点，含）。将分析 From..To 的整仓变更，经 Cursor Agent 润色后写入「更新内容」。",
                MessageType.Info);

            if (!string.IsNullOrEmpty(loadError))
            {
                EditorGUILayout.HelpBox(loadError, MessageType.Error);
                if (GUILayout.Button("重新加载"))
                    ReloadCommits();
                return;
            }

            if (displayLabels.Length == 0)
            {
                EditorGUILayout.HelpBox("没有可用的 commit。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);
            toIndex = EditorGUILayout.Popup("To（新）", toIndex, displayLabels);
            fromIndex = EditorGUILayout.Popup("From（旧）", fromIndex, displayLabels);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("预览列表（新 → 旧）");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            {
                for (int i = 0; i < displayLabels.Length; i++)
                {
                    bool inRange = IsIndexInSelectedRange(i);
                    Color old = GUI.color;
                    if (inRange)
                        GUI.color = new Color(0.55f, 0.9f, 0.55f);
                    EditorGUILayout.LabelField($"{i}. {displayLabels[i]}");
                    GUI.color = old;
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("刷新列表"))
                    ReloadCommits();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("取消", GUILayout.Width(80)))
                    Close();

                EditorGUI.BeginDisabledGroup(fromIndex < 0 || toIndex < 0 || fromIndex == toIndex);
                if (GUILayout.Button("生成更新内容", GUILayout.Width(120)))
                {
                    string fromHash = commits[fromIndex].ShortHash;
                    string toHash = commits[toIndex].ShortHash;
                    // git A..B：A 为旧、B 为新。若用户把新旧选反，自动交换
                    if (fromIndex < toIndex)
                    {
                        // 列表是新→旧，index 更小更新；若 fromIndex < toIndex，说明 From 比 To 新，需交换
                        Debug.LogWarning("[BuildModule.UpdateInfoAI] From 比 To 更新，自动交换以保证 From..To 语义");
                        string tmp = fromHash;
                        fromHash = toHash;
                        toHash = tmp;
                    }

                    Action<string, string> callback = onConfirm;
                    Close();
                    callback?.Invoke(fromHash, toHash);
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 列表按新→旧排序；选中区间为两 index 之间（含两端）。
        /// </summary>
        private bool IsIndexInSelectedRange(int index)
        {
            int lo = Math.Min(fromIndex, toIndex);
            int hi = Math.Max(fromIndex, toIndex);
            return index >= lo && index <= hi;
        }

        #endregion
    }
}
