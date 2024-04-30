using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    // 项目视图资产右键上下文菜单
    public static class ProjectContextMenu
    {
        //获取选中资产的绝对路径
        public static string GetSelectionAssetAbsolutePath()
        {
            Object obj = Selection.activeObject;
            if (obj == null)
                return "";
            string path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
            path = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            path = Path.GetFullPath(path); //会自动把/转换成\
            return path;
        }

        //获取选中资产的相对项目路径
        public static string GetSelectionAssetProjectPath()
        {
            Object obj = Selection.activeObject;
            if (obj == null)
                return "";
            string path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
            return path;
        }

        [MenuItem("Assets/PH Copy Asset Name WithoutEx")]
        public static void CopyAssetNameWithoutEx()
        {
            Object obj = Selection.activeObject;
            EditorGUIUtility.systemCopyBuffer = obj.name;
            Debug.Log($"{obj.name}   已复制到剪切板");
        }

        [MenuItem("Assets/PH Copy Directory Name", true)]
        public static bool CopyDirectoryNameValidate()
        {
            return !string.IsNullOrWhiteSpace(GetSelectionAssetAbsolutePath());
        }

        [MenuItem("Assets/PH Copy Directory Name")]
        public static void CopyDirectoryName()
        {
            string path = GetSelectionAssetAbsolutePath();
            string subPathStr = path.Substring(path.Length - 10);
            if (subPathStr.Contains('.')) //有后缀
            {
                path = Path.GetDirectoryName(path);
            }

            EditorGUIUtility.systemCopyBuffer = path; //复制到系统剪切板
            Debug.Log($"{path}   已复制到剪切板");
        }

        [MenuItem("Assets/PH Copy Absolute Path", true)]
        public static bool CopyAbsolutePathValidate()
        {
            return !string.IsNullOrWhiteSpace(GetSelectionAssetAbsolutePath());
        }

        [MenuItem("Assets/PH Copy Absolute Path")]
        public static void CopyAbsolutePath()
        {
            string path = GetSelectionAssetAbsolutePath();
            EditorGUIUtility.systemCopyBuffer = path; //复制到系统剪切板
            Debug.Log($"{path}   已复制到剪切板");
        }

        [MenuItem("Assets/PH Copy Project Path")]
        public static void CopyProjectPath()
        {
            string path = GetSelectionAssetProjectPath();
            EditorGUIUtility.systemCopyBuffer = path; //复制到系统剪切板
            Debug.Log($"{path}   已复制到剪切板");
        }

        //用于存储本机PH otoshop安装路径到EditorPrefs的Key
        public static string native_ps_path_key = $"CF_PS_Path_Key_{SystemInfo.deviceUniqueIdentifier}";

        //本机PH otoshop路径  eg： "D:\Program Files\Adobe\PH otoshop CC\PH otoshop.exe"
        private static string native_ps_path
        {
            get
            {
                //key与机器相关联
                return EditorPrefs.GetString(native_ps_path_key, "");
            }
            set { EditorPrefs.SetString(native_ps_path_key, value); }
        }


        //图片文件格式
        public static string[] imageExtensions = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".psd", ".tga", ".tif", ".tiff", ".gif" };

        //判断一个路径是否是一个图片文件
        private static bool IsImageFile(string path)
        {
            foreach (string ext in imageExtensions)
            {
                if (path.EndsWith(ext))
                {
                    return true;
                }
            }
            return false;
        }
        //菜单验证函数
        [MenuItem("Assets/PH 使用PS打开图片文件", true)]
        public static bool OpenImgUsePSValidate(MenuCommand menuCommand)
        {
            string[] paths = GetSelectedImageAbsPath();
            foreach (var path in paths)
            {
                if (!File.Exists(path) || !IsImageFile(path))
                {
                    return false;
                }
            }
            return true;
        }
        [MenuItem("Assets/PH 清除Phototoshop路径")]
        public static void ClearPhototoshopNativePath()
        {
            EditorPrefs.DeleteKey(native_ps_path_key);
        }


        [MenuItem("Assets/PH 使用PS打开图片文件")]
        public static void OpenImgUsePS(MenuCommand menuCommand)
        {
            //首次使用 配置PS路径
            if (string.IsNullOrWhiteSpace(native_ps_path))
            {
                //show dialog
                native_ps_path = EditorUtility.OpenFilePanel("选择PS路径", "", "exe");
                Debug.Log($"PS路径程序路径已经设置为：{native_ps_path}");
            }

            foreach (string g in UnityEditor.Selection.assetGUIDs)
            {
                //拼接图片路径
                string imagePath = AssetDatabase.GUIDToAssetPath(g);
                imagePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), imagePath);
                imagePath = imagePath.Replace("/", "\\");

                if (File.Exists(imagePath))
                {
                    Debug.Log($"open {imagePath}");
                    //使用PS打开图片
                    System.Diagnostics.Process.Start(native_ps_path, imagePath);
                }
            }
        }

        private static string[] GetSelectedImageAbsPath()
        {
            List<string> imagePaths = new List<string>();
            foreach (string g in UnityEditor.Selection.assetGUIDs)
            {
                //拼接图片路径
                string imagePath = AssetDatabase.GUIDToAssetPath(g);
                imagePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), imagePath);
                imagePath = imagePath.Replace("/", "\\");
                imagePaths.Add(imagePath);
            }

            return imagePaths.ToArray();
        }

        [MenuItem("Assets/PH 使用目录名 重命名场景资产", false)]
        public static void RenameSceneAssetUseDirectoryName()
        {
            string dirName = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject)));
            string assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(Selection.activeObject));
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string newPath = Path.Combine(Path.GetDirectoryName(assetPath), dirName + ".unity");
            Debug.Log($"重命名场景资产：{assetPath} -> {newPath}");
            AssetDatabase.MoveAsset(assetPath, newPath);
            AssetDatabase.Refresh();
        }
        [MenuItem("Assets/PH 使用目录名 重命名场景资产", true)]
        public static bool RenameSceneAssetUseDirectoryNameValidate()
        {
            //activeObject是场景资产
            return Selection.activeObject != null && Selection.activeObject.GetType() == typeof(SceneAsset);
        }

        //添加一个创建文本资产的菜单以创建txt文件
        //priority不起作用
        [MenuItem("Assets/Create/PluginHub/TextAsset", false, 0)]
        public static void CreateTextAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New TextAsset.txt", "");
        }

        [MenuItem("Assets/Create/PluginHub/Readme", false, 1)]
        public static void CreateTextAssetNamedReadme()
        {
            ProjectWindowUtil.CreateAssetWithContent("readme.txt", "");
        }
    }
}