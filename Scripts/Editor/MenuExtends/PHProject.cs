using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PluginHub.Runtime.Runtime;
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

        // <guid, assetPath>
        private static Dictionary<string, string> guidToAssetPathMap = new();
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
                // 显示资产扩展名
                bool isDirectory = AssetDatabase.IsValidFolder(assetPath);
                if (!isDirectory)
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
    }
}