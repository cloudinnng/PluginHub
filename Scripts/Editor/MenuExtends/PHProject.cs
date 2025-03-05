using System.IO;
using ICSharpCode.SharpZipLib.Core;
using PluginHub.Runtime;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{

    // Project视图GUI
    public class PHProject
    {
        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }
        private static float width = 18f;
        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if(Application.isPlaying)return;
            if(selectionRect.width < 150) return;
            if(selectionRect.height > 18) return; // 避免在图标视图中显示，只在列表视图中显示

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            Rect rect = selectionRect;
            rect.x += selectionRect.width - width;
            rect.width = width;

            // 显示资源管理器按钮
            // 避免显示一些不需要显示的按钮
            if (string.IsNullOrWhiteSpace(assetPath)) return;

            GUI.color = Color.gray;
            if (GUI.Button(rect, PluginHubFunc.IconContent("FolderEmpty On Icon","",
                    $"使用资源管理器打开{assetPath}"), EditorStyles.label))
            {
                Debug.Log("RevealInFinder: " + assetPath);
                EditorUtility.RevealInFinder(assetPath);
            }
            GUI.color = Color.white;
        }
    }
}