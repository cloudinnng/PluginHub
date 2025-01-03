using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace PluginHub.Editor
{
    [System.Serializable]
    public abstract class IBookmark
    {
        //指这个书签位是否保存了内容
        public virtual bool valid { get; private set; }
        
        public abstract bool IsActivated();//激活表示刚刚跳转到这个书签时候的状态
    }
    
    [System.Serializable]
    public class CameraBookmark : IBookmark
    {
        public bool is2DMode;
        public bool Orthographic;
        public Vector3 pivot;
        public Quaternion rotation;
        public float size;    
        
        public void Create()
        {
            is2DMode = SceneView.lastActiveSceneView.in2DMode;
            Orthographic = SceneView.lastActiveSceneView.orthographic;
            pivot = SceneView.lastActiveSceneView.pivot;
            rotation = SceneView.lastActiveSceneView.rotation;
            size = SceneView.lastActiveSceneView.size;
        }
        
        public void FlyTo()
        {
            ViewTweenInitializeOnLoad.GotoCameraBookmark(this, SceneView.lastActiveSceneView);
        }

        public override bool valid => pivot != Vector3.zero && rotation != Quaternion.identity && size != 0;

        public override bool IsActivated()
        {
            return SceneView.lastActiveSceneView.pivot == pivot && SceneView.lastActiveSceneView.rotation == rotation && SceneView.lastActiveSceneView.size == size;
        }
    }

    //基于文本记录的书签
    [System.Serializable]
    public class TextBookmark : IBookmark
    {
        public string text;

        //该书签是否选中
        public override bool IsActivated()
        {
            string name = GetLastNameWithoutEx(text);
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return name.Equals(Selection.activeObject?.name);
        }
        
        protected string GetLastNameWithoutEx(string path)
        {
            string[] names = path.Split('/');
            return Path.GetFileNameWithoutExtension(names[names.Length - 1]);
        }
    }
        
    //场景特定的书签组，每个场景单独记录。不同场景不共享
    [System.Serializable]
    public class SceneBookmarkGroup : IEquatable<SceneBookmarkGroup>
    {
        public string scenePath;
        public List<CameraBookmark> cameraBookmarks = new List<CameraBookmark>();
        public List<GameObjectBookmark> gameObjectPaths = new List<GameObjectBookmark>();
        public List<AssetObjectBookmark> assetPaths = new List<AssetObjectBookmark>();
        
        public SceneBookmarkGroup()
        {
            //默认初始化全部位置，以免遍历出错。使用valid来判断是否存储了内容
            for (int i = 0; i < BookmarkSettings.BOOKMARK_COUNT; i++)
            {
                cameraBookmarks.Add(new CameraBookmark());
                gameObjectPaths.Add(new GameObjectBookmark());
                assetPaths.Add(new AssetObjectBookmark());
            }
        }

        public bool Equals(SceneBookmarkGroup other)
        {
            return scenePath == other.scenePath;
        }
    }
    
    [System.Serializable]
    public class SceneAssetBookmark : TextBookmark
    {
        // public SceneAsset sceneAsset;
        //
        public override bool IsActivated()
        {
            return SceneManager.GetActiveScene().name.Equals(GetLastNameWithoutEx(text));
        }
    }

    [System.Serializable]
    public class GameObjectBookmark : TextBookmark
    {
        public string componentName;

        public override bool valid => string.IsNullOrWhiteSpace(text) == false;
    }
    
    [System.Serializable]
    public class AssetObjectBookmark : TextBookmark
    {
        public string iconName;

        public override bool valid => string.IsNullOrWhiteSpace(text) == false;
    }
    

    public class BookmarkAssetSO : ScriptableObject
    {
        
        #region Singleton
        /// <summary>
        /// 对BookmarkCollection资产的自构造实例访问。
        /// 如果资源已经存在，则加载，否则生成新的资源。
        /// </summary>
        internal static BookmarkAssetSO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.LoadAssetAtPath<BookmarkAssetSO>(BookmarkSettings.BOOKMARKS_ASSET_PATH);
                    if (instance == null)
                    {
                        if (!System.IO.Directory.Exists(BookmarkSettings.ASSET_DIR))
                            System.IO.Directory.CreateDirectory(BookmarkSettings.ASSET_DIR);
                        instance = CreateInstance(typeof(BookmarkAssetSO)) as BookmarkAssetSO;
                        AssetDatabase.CreateAsset(instance, BookmarkSettings.BOOKMARKS_ASSET_PATH);
                        AssetDatabase.SaveAssets();
                    }
                }
                return instance;
            }
        }
        private static BookmarkAssetSO instance;

        #endregion
        
        //场景特定的书签，每个场景都有不同的数据
        public List<SceneBookmarkGroup> bookmarkGroups = new List<SceneBookmarkGroup>();
        
        

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}