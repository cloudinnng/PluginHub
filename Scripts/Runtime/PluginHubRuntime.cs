using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace PluginHub.Runtime
{
    public static class PluginHubRuntime
    {
        public static bool IsCtrlPressed => (Event.current != null) && (Event.current.control || (Event.current.modifiers & EventModifiers.Control) != 0);
        public static bool IsShiftPressed => (Event.current != null) && (Event.current.shift || (Event.current.modifiers & EventModifiers.Shift) != 0);
        public static bool IsAltPressed => (Event.current != null) && (Event.current.alt || (Event.current.modifiers & EventModifiers.Alt) != 0);

        public static IEnumerator DelayAction(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        /// <summary>
        /// 将包内的相对路径（例如 "Scripts/Runtime/UITK/ToastOverlap.uxml" ）转换为 Unity 工程中的可用资源路径（如 "Packages/com.hellottw.pluginhub/..." 或 "Assets/PluginHub-dev/..."）。
        ///
        /// 该方法会智能判断 PluginHub 当前安装位置（即作为 UPM 包安装在 Packages/ 下，还是直接存储在 Assets/ 目录下），自动拼接出正确的项目内路径。
        /// 优先返回目标文件/文件夹真实存在的路径（工程内可访问），若文件尚未实际生成，则回退到包根目录存在即返回预期工程路径。
        /// 用于 Editor、运行时均可用于加载资源、UI 文件等。
        ///
        /// 用法举例：
        /// - 输入: "Scripts/Runtime/UITK/ToastOverlap.uxml"
        ///   若包在 Packages，则返回: "Packages/com.hellottw.pluginhub/Scripts/Runtime/UITK/ToastOverlap.uxml"
        ///   若包在 Assets，则返回: "Assets/PluginHub-dev/Scripts/Runtime/UITK/ToastOverlap.uxml" 或 "Assets/PluginHub/Scripts/Runtime/UITK/ToastOverlap.uxml"
        ///
        /// 特别注意：
        /// - <paramref name="packagePath"/> 必须是相对于 PluginHub 包根目录的相对路径，不要以 "Assets/" 或 "Packages/" 打头。
        /// - 返回字符串为空表示路径解析失败（如传入路径为空或未找到包根）。
        /// </summary>
        /// <param name="packagePath">
        ///     PluginHub 包内的相对路径（从包根目录起，如 "Scripts/Runtime/UITK/ToastOverlap.uxml"）。
        ///     请勿添加 "Assets/"、"Packages/" 或其它工程根路径前缀。
        /// </param>
        /// <returns>
        ///     返回工程内的完整资源路径（如 "Packages/com.hellottw.pluginhub/..." 或 "Assets/PluginHub-dev/..."）。
        ///     若解析失败，返回空字符串。
        /// </returns>
        public static string ResolveRelativePath(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                Debug.LogWarning("[PluginHubRuntime] ResolveRelativePath: packagePath 为空");
                return string.Empty;
            }

            string normalized = packagePath.Replace('\\', '/').Trim();
            // 已是 Unity 资产路径则直接返回
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[PluginHubRuntime] ResolveRelativePath: 已是工程路径 -> {normalized}");
                return normalized;
            }

            string relative = normalized.TrimStart('/');
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogWarning("[PluginHubRuntime] ResolveRelativePath: 无法解析工程根目录（Application.dataPath 无效）");
                return string.Empty;
            }

            // 与 BuildModule 等 Editor 逻辑一致：UPM 包名 + 常见 Assets 安装目录名
            string[] packageRoots =
            {
                "Packages/com.hellottw.pluginhub",
                "Assets/PluginHub-dev",
                "Assets/PluginHub"
            };

            // 优先：目标文件/目录真实存在的根
            foreach (string root in packageRoots)
            {
                string unityPath = $"{root}/{relative}";
                string fullPath = Path.GetFullPath(
                    Path.Combine(projectRoot, unityPath.Replace('/', Path.DirectorySeparatorChar)));
                if (File.Exists(fullPath) || Directory.Exists(fullPath))
                {
                    // Debug.Log($"[PluginHubRuntime] ResolveRelativePath: {relative} -> {unityPath}");
                    return unityPath;
                }
            }

            // 回退：包根目录存在即可（路径尚未生成文件时）
            foreach (string root in packageRoots)
            {
                string rootFull = Path.GetFullPath(
                    Path.Combine(projectRoot, root.Replace('/', Path.DirectorySeparatorChar)));
                if (!Directory.Exists(rootFull))
                    continue;

                string fallbackPath = $"{root}/{relative}";
                Debug.LogWarning(
                    $"[PluginHubRuntime] ResolveRelativePath: 目标不存在，按包根回退 -> {fallbackPath}");
                return fallbackPath;
            }

            Debug.LogWarning(
                "[PluginHubRuntime] ResolveRelativePath: 未检测到 PluginHub 包根，使用默认 Assets/PluginHub-dev");
            return $"Assets/PluginHub-dev/{relative}";
        }
    }
}