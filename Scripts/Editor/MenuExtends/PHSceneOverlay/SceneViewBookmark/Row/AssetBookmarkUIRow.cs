using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    //资产书签
    public class AssetBookmarkUIRow : IBookmarkUIRow
    {
        protected override void DrawHorizontalInnerGUI(SceneBookmarkGroup group)
        {
            GUI.color = (Selection.activeObject != null && Selection.activeObject as GameObject == null) 
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
                    string showName = name.Length > 3 ? name.Substring(0, 3) : name;
                    click = GUILayout.Button(showName, BookmarkButtonStyle, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));
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
                        assetBookmark.iconName = GetIconName(assetObject);
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

        private string GetIconName(Object obj)
        {
            // Debug.Log(obj.GetType());
            if (obj is ScriptableObject)
                return "ScriptableObject";
            if(obj is MonoScript)
                return "cs Script";
            if(obj is SceneAsset)
                return "SceneAsset";
            if(obj is GameObject)
                return "GameObject";
            if (obj is Material)
                return "Material";
            if (obj is Shader)
                return "Shader";
            if (obj is Texture)
                return "Texture";
            if (obj is Mesh)
                return "Mesh";
            if (obj is AnimationClip)
                return "AnimationClip";
            if (obj is AudioClip)
                return "AudioClip";
            if (obj is DefaultAsset)
                return "Folder";
            return null;
        }
    }
}