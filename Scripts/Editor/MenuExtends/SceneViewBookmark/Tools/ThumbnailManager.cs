using UnityEngine;
using UnityEditor;
using System.IO;

namespace PluginHub.Editor
{
    public static class ThumbnailManager
    {
        internal const int THUMBNAIL_WIDTH = 256;
        internal const int THUMBNAIL_HEIGHT = 256;
        private const string NO_SCENE_NAME = "NoScene";

        private static string ThumbnailPath
        {
            get { return $"{BookmarkSettings.ASSET_DIR}/Thumbnails"; }
        }

        public static Texture2D CreateThumbnail(Camera camera, string scenePath, string name)
        {
            if (string.IsNullOrEmpty(scenePath))
                scenePath = NO_SCENE_NAME;

            /*
            Create a temporary render texture and use it to grab a render from the target camrea.
            We do this because scene views aspect ratios may change all the time, but we have a 
            fixed size for our thumbnails for consitency and to reduce storage use.
            */
            RenderTexture camTarget = camera.targetTexture;
            RenderTexture tempRT = RenderTexture.GetTemporary(THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT, 16, RenderTextureFormat.Default);
            camera.targetTexture = tempRT;
            camera.Render();
            camera.targetTexture = camTarget;

            // Now take that lovely render and turn it into a good old Texture2D
            Texture2D tex = new Texture2D(THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT, TextureFormat.RGB24, false);
            RenderTexture.active = tempRT;
            tex.ReadPixels(new Rect(0, 0, THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT), 0, 0);
            RenderTexture.ReleaseTemporary(tempRT);

            // Make sure we have a target folder and save the texture to a file.
            string target = $"{ThumbnailPath}/{scenePath}";
            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            byte[] pngData = tex.EncodeToPNG();
            using (FileStream fs = new FileStream($"{ThumbnailPath}/{scenePath}/{name}.png", FileMode.Create))
            {
                fs.Write(pngData, 0, pngData.Length);
            }

            AssetDatabase.Refresh();

            return tex;
        }

        public static Texture2D TryGetThumbnail(string scenePath, string name)
        {
            try
            {
                if (string.IsNullOrEmpty(scenePath))
                    scenePath = NO_SCENE_NAME;

                string target = $"{ThumbnailPath}/{scenePath}/{name}.png";
                return AssetDatabase.LoadAssetAtPath<Texture2D>(target);
            }
            catch
            {
                return null;
            }
        }

        public static void DeleteThumbnail(string scenePath, string name)
        {
            if (string.IsNullOrEmpty(scenePath))
                scenePath = NO_SCENE_NAME;

            string target = $"{ThumbnailPath}/{scenePath}/{name}.png";
            if (File.Exists(target))
                File.Delete(target);

            //同时也删除meta文件避免控制台出现警告
            string metaPath = $"{target}.meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
            
            AssetDatabase.Refresh();
        }

        public static void DeleteThumbnails(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                scenePath = NO_SCENE_NAME;

            string target = $"{ThumbnailPath}/{scenePath}";
            if (Directory.Exists(target))
                Directory.Delete(target);

            AssetDatabase.Refresh();
        }
    }
}