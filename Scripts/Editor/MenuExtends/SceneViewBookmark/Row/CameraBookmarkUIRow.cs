﻿using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    public class CameraBookmarkUIRow : IBookmarkUIRow
    {
        protected override void DrawHorizontalInnerGUI(SceneBookmarkGroup group)
        {
            //画行首的图标
            GUILayout.Label(EditorGUIUtility.IconContent("Camera Icon").image, 
                GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), 
                GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));
            
            string currScenePath = SceneManager.GetActiveScene().path;
            
            for (int i = 0; i < BookmarkSettings.BOOKMARK_COUNT; i++)
            {
                CameraBookmark cameraBookmark = group.cameraBookmarks[i];
                GUI.color = cameraBookmark.valid 
                    ? (cameraBookmark.IsActivated() 
                        ? BookmarkSettings.COLOR_BOOKMARK_BUTTON_ACTIVE 
                        : BookmarkSettings.COLOR_BOOKMARK_BUTTON_NORMAL)
                    : BookmarkSettings.COLOR_BOOKMARK_BUTTON_EMPTY;

                bool click = false;
                if (cameraBookmark.valid)
                {
                    Texture texture = ThumbnailManager.TryGetThumbnail(currScenePath, i.ToString());
                    click = GUILayout.Button(texture, BookmarkButtonStyle, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));
                }
                else
                {
                    click = GUILayout.Button((i + 1).ToString(), BookmarkButtonStyle, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));
                }

                if (click)
                {
                    HandleButton(cameraBookmark, i);
                }

                GUI.color = Color.white;

                //鼠标放上去显示文字
                Rect rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(Event.current.mousePosition))
                {
                    // ShowHoverTip(rect, cameraBookmark.valid ? ThumbnailManager.TryGetThumbnail(currScenePath, i.ToString()) : null);
                }
            }
        }
        private void HandleButton(CameraBookmark cameraBookmark, int i)
        {
            string currScenePath = SceneManager.GetActiveScene().path;
            if (Event.current.control)//按了ctrl
            {
                if (Event.current.button == 0)//左键
                {
                    cameraBookmark.Create();
                    BookmarkAssetSO.Instance.Save();
                    ThumbnailManager.CreateThumbnail(SceneView.lastActiveSceneView.camera, currScenePath, i.ToString());
                }else if (Event.current.button == 1)//右键
                {
                    ThumbnailManager.DeleteThumbnail(currScenePath, i.ToString());
                    cameraBookmark.size = 0;
                    BookmarkAssetSO.Instance.Save();
                }
            }
            else//没按ctrl
            {
                if (cameraBookmark.valid)
                {
                    cameraBookmark.FlyTo();
                }
            }   
        }
    }
}