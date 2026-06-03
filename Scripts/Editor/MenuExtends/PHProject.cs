using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using PluginHub.Runtime;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    // Project视图GUI工具
    public class PHProject
    {
        private const float width = 18f;
        private const float height = 18f;
        private const float referenceBtnWidth = 28f;
        private const float btnSpacing = 2f;
        private static Rect btnRect = new(0, 0, width, height);
        private static readonly string[] referenceSearchExtensions =
            { ".prefab", ".unity", ".mat", ".asset", ".controller" };

        // <guid, assetPath>  注意：缓存会过期（资产重命名后 path 变化但 guid 不变）
        //   通过 PHProjectCachePostprocessor 在资源变化时清空来保证一致
        private static Dictionary<string, string> guidToAssetPathMap = new();

        // <assetPath, isReparsePoint>  缓存文件夹是否是 junction/symlink/mount point
        //   避免每帧每个可见 item 都做 IO（File.GetAttributes）
        //   同样依赖 PHProjectCachePostprocessor 失效
        private static Dictionary<string, bool> assetPathToIsReparsePointMap = new();

        private static GUIContent tmpGUIContent = new();

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }
        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (!guidToAssetPathMap.ContainsKey(guid))
                guidToAssetPathMap[guid] = AssetDatabase.GUIDToAssetPath(guid);
            string assetPath = guidToAssetPathMap[guid];


            // if (Application.isPlaying) return;// 避免影响播放模式性能

            btnRect.Set(selectionRect.x + selectionRect.width - width, selectionRect.y, width, height);

            // 避免显示一些不需要显示的按钮
            // if (string.IsNullOrWhiteSpace(assetPath)) return;

            GUI.color = Color.gray;
            {
                if (GUI.Button(btnRect, PluginHubEditor.IconContent("FolderEmpty On Icon", "", $"使用资源管理器打开"), EditorStyles.label))
                {
                    Debug.Log("RevealInFinder: " + assetPath);
                    EditorUtility.RevealInFinder(assetPath);
                }
                btnRect.x -= width;
                if (GUI.Button(btnRect, PluginHubEditor.IconContent("d_TreeEditor.Duplicate", "", $"复制路径到剪贴板"), EditorStyles.label))
                {
                    Debug.Log("按下Ctrl可分别拷贝资源路径和完整路径");
                    // assetPath.Substring(7) 去掉 开头的"Assets/"
                    string fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7)).Replace("/", "\\");
                    if (PluginHubRuntime.IsCtrlPressed)
                    {
                        Debug.Log("CopyPathToClipboard: " + assetPath);
                        EditorGUIUtility.systemCopyBuffer = assetPath;
                    }
                    else
                    {
                        Debug.Log("CopyPathToClipboard: " + fullPath);
                        EditorGUIUtility.systemCopyBuffer = fullPath;
                    }
                }
                // 区分文件夹与文件：文件夹仅在是 junction/symlink 时绘制 "junction" 标签
                //                   文件则继续走原本的扩展名/Ref 按钮逻辑
                bool isDirectory = AssetDatabase.IsValidFolder(assetPath);
                if (isDirectory)
                {
                    // 仅检测 Assets/ 下的非根文件夹，避免 Packages 等内置目录被误判
                    if (!string.IsNullOrEmpty(assetPath)
                        && assetPath.StartsWith("Assets")
                        && assetPath != "Assets")
                    {
                        if (!assetPathToIsReparsePointMap.TryGetValue(assetPath, out bool isReparsePoint))
                        {
                            isReparsePoint = IsAssetReparsePoint(assetPath);
                            assetPathToIsReparsePointMap[assetPath] = isReparsePoint;
                        }

                        if (isReparsePoint)
                        {
                            const string junctionLabel = "junction";
                            tmpGUIContent.text = junctionLabel;
                            float labelWidth = GUI.skin.label.CalcSize(tmpGUIContent).x;
                            btnRect.x -= labelWidth;
                            Rect junctionRect = new(btnRect.x, btnRect.y, labelWidth, height);

                            // 点击：查询并打印真实 target，复制到剪贴板，并用资源管理器打开 target
                            if (GUI.Button(junctionRect, new GUIContent(junctionLabel, "该文件夹是 junction/symlink，点击查看其真实目标路径"), EditorStyles.label))
                            {
                                OnJunctionLabelClicked(assetPath);
                            }
                        }
                    }
                }
                else
                {
                    if (Selection.assetGUIDs.Contains(guid))
                    {
                        GUI.color = Color.yellow;
                    }

                    string extension = Path.GetExtension(assetPath);
                    tmpGUIContent.text = extension;
                    float labelWidth = GUI.skin.label.CalcSize(tmpGUIContent).x;
                    btnRect.x -= labelWidth;
                    Rect extensionBtnRect = new(btnRect.x, btnRect.y, labelWidth, height);
                    // Ref按钮仅在按住Ctrl时显示，避免常态占用项目视图空间
                    if (PluginHubRuntime.IsCtrlPressed)
                    {
                        Rect referenceBtnRect = new(
                            extensionBtnRect.x - referenceBtnWidth - btnSpacing,
                            btnRect.y,
                            referenceBtnWidth,
                            height
                        );

                        // 在扩展名按钮前增加引用统计按钮，用于快速检查该资源是否仍被项目引用
                        if (GUI.Button(referenceBtnRect, new GUIContent("Ref", "统计该资源在项目中的引用个数"), EditorStyles.miniButton))
                        {
                            Debug.Log($"开始统计项目引用: {assetPath}");
                            FindReferenceCountInProject(assetPath);
                        }
                    }

                    // 文件扩展名按钮
                    if (GUI.Button(extensionBtnRect, extension, EditorStyles.label))
                    {
                        // Debug.Log("按下扩展名按钮: " + extension);
                        if (IsImageExtension(extension))
                        {
                            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            if (importer != null)
                            {
                                importer.GetSourceTextureWidthAndHeight(out int origW, out int origH);
                                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                                string fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7)).Replace("/", "\\");
                                long fileSize = new FileInfo(fullPath).Length;
                                string importedRes = tex != null ? $"{tex.width} x {tex.height}" : "N/A";
                                Debug.Log($"原始分辨率: {origW} x {origH} 导入后分辨率: {importedRes} 文件大小: {FormatFileSize(fileSize)}");
                            }
                            else
                                Debug.LogWarning("无法获取图片导入信息: " + assetPath);
                        }
                        else
                        {
                            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7)).Replace("/", "\\");
                            long fileSize = new FileInfo(fullPath).Length;
                            Debug.Log($"文件大小: {FormatFileSize(fileSize)}");
                        }
                    }
                }
            }
            GUI.color = Color.white;
        }

        // 参考 PHProjectContextMenu.PHFindReferencesInProject 的实现思路，统计引用个数并输出日志
        private static void FindReferenceCountInProject(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogWarning("统计引用失败: 资源路径为空");
                return;
            }

            EditorSettings.serializationMode = SerializationMode.ForceText;
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                Debug.LogWarning($"统计引用失败: 无法获取GUID, path={assetPath}");
                return;
            }

            string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                .Where(s => referenceSearchExtensions.Contains(Path.GetExtension(s).ToLowerInvariant()))
                .ToArray();

            if (files.Length == 0)
            {
                Debug.LogWarning("统计引用结束: 没有可扫描的资源文件");
                return;
            }

            int startIndex = 0;
            int counter = 0;
            Object contextObj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            Debug.Log($"引用扫描已启动: path={assetPath}, guid={guid}, files={files.Length}");

            EditorApplication.update = delegate
            {
                string file = files[startIndex];
                bool isCancel = EditorUtility.DisplayCancelableProgressBar(
                    "统计资源引用中",
                    file,
                    (float)startIndex / files.Length
                );

                try
                {
                    if (Regex.IsMatch(File.ReadAllText(file), guid))
                    {
                        counter++;
                        string relativeAssetPath = GetRelativeAssetsPath(file);
                        Object hitObj = AssetDatabase.LoadAssetAtPath<Object>(relativeAssetPath);
                        Debug.Log($"引用文件: {relativeAssetPath}", hitObj);
                    }
                }
                catch (IOException ex)
                {
                    Debug.LogWarning($"读取文件失败，跳过: {file}\n{ex.Message}");
                }

                startIndex++;
                if (isCancel || startIndex >= files.Length)
                {
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update = null;
                    Debug.Log($"引用统计结束: {assetPath} 共找到 {counter} 个引用", contextObj);
                }
            };
        }

        private static string GetRelativeAssetsPath(string fullPath)
        {
            return "Assets" + Path.GetFullPath(fullPath)
                .Replace(Path.GetFullPath(Application.dataPath), "")
                .Replace('\\', '/');
        }

        private static bool IsImageExtension(string ext)
        {
            ext = ext?.ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".tga" or ".psd" or ".bmp" or ".exr" or ".hdr" or ".tif" or ".tiff" or ".iff";
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / 1024f / 1024f:F1} MB";
        }

        // 把 Unity 风格的 assetPath（如 "Assets/Foo/Bar"）转成 Windows 风格的绝对路径
        // Application.dataPath = "...\\Assets"，所以去掉前缀 "Assets/" 后拼到 dataPath 上
        private static string AssetPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;
            // assetPath 长度 > 7 即 "Assets/xxx" 形式
            string fullPath = assetPath.Length > 7
                ? Path.Combine(Application.dataPath, assetPath.Substring(7))
                : Application.dataPath;
            return fullPath.Replace('/', '\\');
        }

        // 检测一个 assetPath 指向的文件夹是否是 reparse point（junction/symlink/mount point）
        // 实现：读取 FileAttributes，看是否带 ReparsePoint 位
        // 注意：调用方应该已经确认这是个文件夹（IsValidFolder=true）
        private static bool IsAssetReparsePoint(string assetPath)
        {
            try
            {
                string fullPath = AssetPathToFullPath(assetPath);
                if (string.IsNullOrEmpty(fullPath) || !Directory.Exists(fullPath))
                    return false;
                var attrs = File.GetAttributes(fullPath);
                return (attrs & FileAttributes.ReparsePoint) != 0;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PHProject] 检测 ReparsePoint 失败: {assetPath}\n{ex.Message}");
                return false;
            }
        }

        // junction 标签点击行为：
        //  1. 在 Console 打印 junction 自身位置与真实 target
        //  2. 把 target 路径复制到剪贴板
        //  3. 用资源管理器打开 target（不是 junction 本身，方便看真实位置）
        private static void OnJunctionLabelClicked(string assetPath)
        {
            string fullPath = AssetPathToFullPath(assetPath);
            Debug.Log($"[PHProject] junction 位置: {fullPath}");

#if UNITY_EDITOR_WIN
            string target = GetReparsePointTarget(fullPath);
            if (!string.IsNullOrEmpty(target))
            {
                Debug.Log($"[PHProject] junction 指向: {target}（已复制到剪贴板）");
                EditorGUIUtility.systemCopyBuffer = target;
                if (Directory.Exists(target))
                    EditorUtility.RevealInFinder(target);
                else
                    Debug.LogWarning($"[PHProject] 真实目标路径已失效（可能外部文件夹被删除）: {target}");
            }
            else
            {
                Debug.LogWarning($"[PHProject] 无法解析 junction 目标路径: {fullPath}");
            }
#else
            Debug.Log("[PHProject] 非 Windows 平台不支持解析 reparse point target");
#endif
        }

#if UNITY_EDITOR_WIN
        // 用 Win32 API 解析 reparse point 真实目标
        // 比 P/Invoke FSCTL_GET_REPARSE_POINT 简单，且对所有 reparse 类型通用
        // 注意：必须用 FILE_FLAG_BACKUP_SEMANTICS 才能打开"目录"句柄
        //       不传 FILE_FLAG_OPEN_REPARSE_POINT，让句柄"跟随"reparse 到真实目标
        private static string GetReparsePointTarget(string fullPath)
        {
            const uint FILE_READ_ATTRIBUTES = 0x80;
            const uint FILE_SHARE_ALL = 0x1 | 0x2 | 0x4;
            const uint OPEN_EXISTING = 3;
            const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
            System.IntPtr INVALID_HANDLE_VALUE = new System.IntPtr(-1);

            System.IntPtr handle = NativeMethods.CreateFileW(
                fullPath,
                FILE_READ_ATTRIBUTES,
                FILE_SHARE_ALL,
                System.IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS,
                System.IntPtr.Zero);

            if (handle == INVALID_HANDLE_VALUE)
            {
                int err = Marshal.GetLastWin32Error();
                Debug.LogWarning($"[PHProject] CreateFileW 失败 errno={err}, path={fullPath}");
                return null;
            }

            try
            {
                var sb = new System.Text.StringBuilder(1024);
                uint len = NativeMethods.GetFinalPathNameByHandleW(handle, sb, (uint)sb.Capacity, 0);
                if (len == 0)
                {
                    int err = Marshal.GetLastWin32Error();
                    Debug.LogWarning($"[PHProject] GetFinalPathNameByHandleW 失败 errno={err}");
                    return null;
                }
                string result = sb.ToString();
                // 去掉 \\?\ 前缀（GetFinalPathNameByHandle 默认带）
                if (result.StartsWith(@"\\?\"))
                    result = result.Substring(4);
                return result;
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern System.IntPtr CreateFileW(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                System.IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                System.IntPtr hTemplateFile);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern uint GetFinalPathNameByHandleW(
                System.IntPtr hFile,
                System.Text.StringBuilder lpszFilePath,
                uint cchFilePath,
                uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(System.IntPtr hObject);
        }
#endif

        // 监听资源变化，清空两个缓存并主动重绘 Project 视图
        //   解决：
        //     1. 重命名/移动文件后，扩展名显示不更新（旧 guid→path 缓存）
        //     2. 创建/删除 junction 后，"junction" 标签不刷新
        //   策略：暴力清空全部缓存。条目数有限，重建成本可以接受
        private class PHProjectCachePostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                guidToAssetPathMap.Clear();
                assetPathToIsReparsePointMap.Clear();
                EditorApplication.RepaintProjectWindow();
            }
        }
    }
}