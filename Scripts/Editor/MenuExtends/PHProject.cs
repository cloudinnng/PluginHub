using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    // Project视图GUI工具
    public class PHProject
    {
        private const float width = 18f;
        private const float height = 18f;
        private static Rect btnRect = new(0, 0, width, height);

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }
        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            // if (Application.isPlaying) return;// 避免影响播放模式性能

            btnRect.Set(selectionRect.x + selectionRect.width - width, selectionRect.y, width, height);

            // 避免显示一些不需要显示的按钮
            // if (string.IsNullOrWhiteSpace(assetPath)) return;

            GUI.color = Color.gray;
            {
                if (GUI.Button(btnRect, PluginHubFunc.IconContent("FolderEmpty On Icon", "", $"使用资源管理器打开"), EditorStyles.label))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    Debug.Log("RevealInFinder: " + assetPath);
                    EditorUtility.RevealInFinder(assetPath);
                }
                btnRect.x -= width;
                if (GUI.Button(btnRect, PluginHubFunc.IconContent("d_TreeEditor.Duplicate", "", $"复制路径到剪贴板"), EditorStyles.label))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", "")).Replace("/", "\\");
                    Debug.Log("CopyPathToClipboard: " + fullPath);
                    EditorGUIUtility.systemCopyBuffer = fullPath;
                }
            }
            GUI.color = Color.white;
        }
    }
}