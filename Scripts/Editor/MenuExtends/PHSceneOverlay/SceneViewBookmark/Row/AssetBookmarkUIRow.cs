using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PluginHub.Editor
{
    //资产书签
    public class AssetBookmarkUIRow : IBookmarkUIRow
    {
        private GUIStyle bottomLabel;
        protected override void DrawHorizontalInnerGUI(SceneBookmarkGroup group)
        {
            if (bottomLabel == null)
            {
                bottomLabel = new GUIStyle(GUI.skin.label);
                bottomLabel.fontSize = 12;
            }
            GUI.color = (Selection.activeObject != null && AssetDatabase.Contains(Selection.activeObject))
                ? BookmarkSettings.COLOR_BOOKMARK_BUTTON_ACTIVE
                : Color.white;
            //画行首的图标
            GUILayout.Label(EditorGUIUtility.IconContent("d_FolderFavorite Icon").image, 
                GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), 
                GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));
            GUI.color = Color.white;
            
            for (int i = 0; i < BookmarkSettings.BOOKMARK_COUNT; i++)
            {
                AssetObjectBookmark assetBookmark = group.assetPaths[i];
                GUI.color = assetBookmark.hasContentSaved
                    ? (assetBookmark.IsActivated()
                        ? BookmarkSettings.COLOR_BOOKMARK_BUTTON_ACTIVE
                        : BookmarkSettings.COLOR_BOOKMARK_BUTTON_NORMAL)
                    : BookmarkSettings.COLOR_BOOKMARK_BUTTON_EMPTY;
                

                bool click = false;
                if (assetBookmark.hasContentSaved && !string.IsNullOrWhiteSpace(assetBookmark.iconName))
                {
                    // GUIContent icon = EditorGUIUtility.IconContent($"d_{assetBookmark.iconName} Icon");

                    string name = Path.GetFileNameWithoutExtension(assetBookmark.text);
                    // string showName = name.Length > 3 ? name.Substring(0, 3) : name;
                    string showName = name;
                    click = GUILayout.Button("", BookmarkButtonStyle, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    // texture
                    Rect textureRect = new Rect(lastRect.x + 13, lastRect.y+2,16,16);
                    GUI.DrawTexture(textureRect, EditorGUIUtility.IconContent(assetBookmark.iconName).image, ScaleMode.ScaleToFit);
                    // label
                    Rect labelRect = new Rect(lastRect.x, lastRect.y + 15, BookmarkSettings.BUTTON_SIZE.x, 15);
                    GUI.Label(labelRect, showName,bottomLabel);
                }
                else
                {
                    click = GUILayout.Button((i + 1).ToString(), BookmarkButtonStyle, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));
                }

                if (click)
                {
                    HandleButton(assetBookmark);
                }
                GUI.color = Color.white;

                //鼠标放上去显示文字
                Rect rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(Event.current.mousePosition))
                {
                    PHSceneOverlay.tempTipContent.text = assetBookmark.text;
                    PHSceneOverlay.tipContentKey = $"AssetBookmarkUIRow{i}";
                }
                else
                {
                    if (PHSceneOverlay.tipContentKey == $"AssetBookmarkUIRow{i}")
                    {
                        PHSceneOverlay.tempTipContent.text = "";
                        PHSceneOverlay.tipContentKey = "";
                    }
                }
            }
        }

        private void HandleButton(AssetObjectBookmark assetBookmark)
        {
            if (Event.current.control)//按了ctrl
            {
                if (Event.current.button == 0)//左键存储到书签
                {
                    Object assetObject = Selection.activeObject;
                    if (assetObject != null)
                    {
                        assetBookmark.text = AssetDatabase.GetAssetPath(assetObject);
                        assetBookmark.iconName = GetIconName(assetObject,assetBookmark.text);
                        BookmarkAssetSO.Instance.Save();
                    }
                }
                else if (Event.current.button == 1)//右键清空
                {
                    assetBookmark.text = "";
                    assetBookmark.iconName = "";
                    BookmarkAssetSO.Instance.Save();
                }
            }
            else//没按ctrl
            {
                if (assetBookmark.hasContentSaved)
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetBookmark.text);
                }
            }
        }

        private string GetIconName(Object obj, string path)
        {
            // Debug.Log(obj.GetType());
            path = path.ToLower();
            if (path.EndsWith(".prefab"))
                return "Prefab Icon";
            if (path.EndsWith(".fbx"))
                return "PrefabModel Icon";
            if (path.EndsWith(".mat"))
                return "Material Icon";
            if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
                return "RawImage Icon";
            if (path.EndsWith(".shader"))
                return "Shader Icon";
            if (path.EndsWith(".controller") || path.EndsWith(".animator"))
                return "AnimatorController Icon";
            if (path.EndsWith(".anim"))
                return "Animation Icon";
            if (path.EndsWith(".unity"))
                return "SceneAsset Icon";
            if (path.EndsWith(".asset"))
                return "ScriptableObject Icon";
            if (path.EndsWith(".cs"))
                return "CSharpScript Icon";
            if (path.EndsWith(".txt") || path.EndsWith(".json") || path.EndsWith(".xml"))
                return "TextAsset Icon";
            return "FolderEmpty On Icon";
        }
    }
}