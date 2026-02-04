using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static Rect btnRect = new(0, 0, width, height);

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
                    btnRect.width = labelWidth;
                    btnRect.x -= labelWidth;
                    GUI.Label(btnRect, extension);
                    
                }
            }
            GUI.color = Color.white;
        }
    }
}