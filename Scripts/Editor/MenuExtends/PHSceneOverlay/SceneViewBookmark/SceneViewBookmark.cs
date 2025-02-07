using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    public class SceneViewBookmark
    {
        private static CameraBookmarkUIRow _cameraBookmarkUIRow = new CameraBookmarkUIRow();
        private static GameObjectBookmarkUIRow _gameObjectBookmarkUIRow = new GameObjectBookmarkUIRow();
        private static AssetBookmarkUIRow _assetBookmarkUIRow = new AssetBookmarkUIRow();
        public static void DrawSceneBookmark()
        {
            //当前场景路径
            string currScenePath = SceneManager.GetActiveScene().path;
            //所有场景书签
            List<SceneBookmarkGroup> bookmarkGroups = BookmarkAssetSO.Instance.bookmarkGroups;
            //找到当前场景的书签
            SceneBookmarkGroup sceneBookmarkGroup =  bookmarkGroups.Find(x => x.scenePath == currScenePath);
            if(sceneBookmarkGroup == null)
            {
                sceneBookmarkGroup = new SceneBookmarkGroup() { scenePath = currScenePath };
                bookmarkGroups.Add(sceneBookmarkGroup);
            }
            //相机书签--------------------------------------------------------------------------------
            _cameraBookmarkUIRow.DrawGUI(sceneBookmarkGroup);
            //游戏对象书签--------------------------------------------------------------------------------
            _gameObjectBookmarkUIRow.DrawGUI(sceneBookmarkGroup);
            //资产书签--------------------------------------------------------------------------------
            _assetBookmarkUIRow.DrawGUI(sceneBookmarkGroup);
        }
    }
}