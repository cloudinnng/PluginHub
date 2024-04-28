using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Helper
{
    // 实现场景视图和游戏视图截图功能
    public static class SceneGameScreenShot
    {
        private static int count = 0;

        private static string newPath
        {
            get{
                //准备保存路径
                string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 7);
                string tmpFilePath = Path.Combine(projectPath,
                    $"Recordings/Screenshot {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")} {Screen.width}x{Screen.height}.png");
                tmpFilePath = tmpFilePath.Replace("\\", "/");
                if (!Directory.Exists(Path.GetDirectoryName(tmpFilePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tmpFilePath));
                return tmpFilePath;
            }
        }

        struct TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }
        static TransformData cameraData;
        private static void SaveTransform(Camera cameraToSave)
        {
            cameraData.position = cameraToSave.transform.position;
            cameraData.rotation = cameraToSave.transform.rotation;
            cameraData.scale = cameraToSave.transform.localScale;
        }
        private static void RestoreTransform(Camera cameraToRestore)
        {
            cameraToRestore.transform.position = cameraData.position;
            cameraToRestore.transform.rotation = cameraData.rotation;
            cameraToRestore.transform.localScale = cameraData.scale;
        }


        //Scene视图屏幕截图
        //其原理是将Scene视图的相机的位置和旋转赋值给Game视图的相机，然后截图,最后恢复Game视图的相机
        public static void ScreenShotSceneView()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("only use in edit mode");
                return;
            }
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            Camera gameMainCamera = Camera.main;

            if (sceneCamera == null || gameMainCamera == null)
            {
                Debug.LogError("没有相机");
                return;
            }
            SaveTransform(gameMainCamera);
            gameMainCamera.transform.position = sceneCamera.transform.position;
            gameMainCamera.transform.rotation = sceneCamera.transform.rotation;
            gameMainCamera.transform.localScale = sceneCamera.transform.localScale;
            ScreenShotGameView();
            EditorApplication.update += DelayRestoreGameCamera;
        }

        private static void DelayRestoreGameCamera()
        {
            count += 1;
            if (count > 1)
            {
                count = 0;
                EditorApplication.update -= DelayRestoreGameCamera;
                RestoreTransform(Camera.main);
            }
        }


        //执行真正的屏幕截图
        public static void ScreenShotGameView()
        {
            //确保打开Game视图,不然会截图失败
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            string path = newPath;
            ScreenCapture.CaptureScreenshot(path); //进行截图
            Debug.Log($"截图已保存到 {path}");
            //打开截图文件夹
            EditorUtility.RevealInFinder(path);
        }



        //将图片放到剪切板，仅限Windows
        //亲测：这个方法在Unity 2021.3.2f1中可以直接使用，但是在Unity 2021.3.29f1中需要
        //将D:\Unity\2021.3.29f1\Editor\Data\MonoBleedingEdge\lib\mono\unityjit-win32\System.Windows.Forms.dll拷贝到Plugin文件夹里
        //如果遇到这个错误(File does not contain a valid CIL image)表示拷贝错了，需要拷贝正确的winform.dll
        //拷贝dll的方案来自这个帖子：https://forum.unity.com/threads/problems-trying-to-use-sqlite-with-unity-2020-2-2.1044718/
        private static void CopyImageToClipboard(string filePath)
        {
#if PH_WINFORMS
            System.Drawing.Image image = System.Drawing.Image.FromFile(filePath);
            System.Windows.Forms.Clipboard.SetImage(image);
            image.Dispose();
            Debug.Log("截图已复制到剪切板");
#else
            Debug.Log("PH_WINFORMS not define");
#endif
        }
    }
}