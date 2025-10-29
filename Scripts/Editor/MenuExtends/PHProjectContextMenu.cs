using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    // 项目视图资产右键上下文菜单

    public static class PHProjectContextMenu
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
            Object[] objs = Selection.objects;
            StringBuilder sb = new StringBuilder();
            foreach (var obj in objs)
            {
                string name = obj.name;
                sb.AppendLine(name);
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString(); //复制到系统剪切板
            Debug.Log($"{sb}   已复制到剪切板");
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
        public static string native_ps_path_key = $"PH_PS_Path_Key_{SystemInfo.deviceUniqueIdentifier}";

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

        #region PhotoShop Menu

        //图片文件格式
        public static string[] imageExtensions = new string[]
            { ".png", ".jpg", ".jpeg", ".bmp", ".psd", ".tga", ".tif", ".tiff", ".gif", ".exr" };

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

        #endregion

        #region notepad++ Menu

        [MenuItem("Assets/PH 使用notepad++打开文件", true)]
        public static bool OpenFileUseNotepadValid(MenuCommand menuCommand)
        {
            return Application.platform == RuntimePlatform.WindowsEditor;
        }

        [MenuItem("Assets/PH 使用notepad++打开文件", false)]
        public static void OpenFileUseNotepad(MenuCommand menuCommand)
        {
            foreach (string g in Selection.assetGUIDs)
            {
                Debug.Log($"GUID:{g}");
                //拼接文件路径
                string filePath = AssetDatabase.GUIDToAssetPath(g);
                filePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), filePath);
                filePath = filePath.Replace("/", "\\");

                if (File.Exists(filePath))
                {
                    Debug.Log($"open {filePath}");
                    //使用PS打开图片
                    System.Diagnostics.Process.Start("notepad++", $"\"{filePath}\"");
                }
            }
        }

        #endregion


        [MenuItem("Assets/PH 使用目录名 重命名场景资产", false)]
        public static void RenameSceneAssetUseDirectoryName()
        {
            string dirName =
                Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject)));
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

        #region PH Find References

        // 这个方法比Unity的查找方法还好用，可以找到项目中哪个场景或者预制体挂载了这个脚本
        // 当你想清理项目时，可以先使用这个方法查找资源是否被引用，避免误删
        //
        [MenuItem("Assets/PH Find References In Project", false)]
        private static void PHFindReferencesInProject()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                string[] extensions = { ".prefab", ".unity", ".mat", ".asset", ".controller" };
                string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => extensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
                int startIndex = 0;

                int counter = 0;
                EditorApplication.update = delegate ()
                {
                    string file = files[startIndex];

                    bool isCancel =
                        EditorUtility.DisplayCancelableProgressBar("匹配资源中", file,
                            (float)startIndex / (float)files.Length);

                    if (Regex.IsMatch(File.ReadAllText(file), guid))
                    {
                        Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                        counter++;
                    }

                    startIndex++;
                    if (isCancel || startIndex >= files.Length)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorApplication.update = null;
                        startIndex = 0;
                        Debug.Log($"匹配结束，共找到{counter}个引用");
                    }
                };
            }
        }

        [MenuItem("Assets/PH Find References", true)]
        private static bool VPHFindReferences()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return (!string.IsNullOrEmpty(path));
        }

        private static string GetRelativeAssetsPath(string path)
        {
            return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "")
                .Replace('\\', '/');
        }

        #endregion

        #region File Prefix 根据资产命名约定命名资产
        [MenuItem("Assets/PH 根据资产命名约定添加前缀", false)]
        private static void AddPrefixToSelectedAssets()
        {
            // 获取当前选中的所有资源
            var selectedAssets = Selection.GetFiltered<Object>(SelectionMode.Assets);

            foreach (var asset in selectedAssets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                AddPrefixToAsset(assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/PH 根据资产命名约定添加前缀", true)]
        private static bool ValidateAddPrefixToSelectedAssets()
        {
            // 仅在有资源选中时启用菜单项
            return Selection.GetFiltered<Object>(SelectionMode.Assets).Length > 0;
        }
        private static void AddPrefixToAsset(string assetPath)
        {
            string fileName = System.IO.Path.GetFileName(assetPath);

            // 根据文件扩展名获取前缀
            string prefix = GetPrefixByExtension(assetPath);
            if (string.IsNullOrEmpty(prefix))
                return; // 没有匹配前缀则跳过

            // 检查文件是否已经有前缀，避免重复添加
            if (fileName.StartsWith(prefix))
                return;

            string directory = System.IO.Path.GetDirectoryName(assetPath);
            string newFileName = prefix + fileName;
            string newPath = System.IO.Path.Combine(directory, newFileName).Replace("\\", "/");

            // 检查是否已存在同名文件，避免冲突
            if (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
            {
                Debug.LogWarning($"文件重命名冲突: {newPath} 已存在，跳过此文件。");
                return;
            }

            // 重命名资源
            Debug.Log($"PH 重命名资源: {assetPath} -> {newPath}");
            AssetDatabase.RenameAsset(assetPath, newFileName);
        }

        private static string GetPrefixByExtension(string path)
        {
            string extension = System.IO.Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".png":
                case ".jpg":
                case ".tga":
                case ".psd":
                case ".bmp":
                case ".exr":
                case ".hdr":
                case ".tif":
                    return "T_";  // 纹理
                case ".mat":
                    return "M_";  // 材质
                case ".shadergraph":
                    return "SG_"; // Shader Graph
                case ".shader":
                    return "S_";  // Shader
                case ".fbx":
                case ".obj":
                case ".blend":
                    return "Mesh_";  // 模型 Mesh
                case ".prefab":
                    return "PFB_";  // 预制体 Prefab
                case ".controller":
                    return "AC_";  // 动画控制器 Animation Controller
                default:
                    return null;  // 不处理其他类型
            }
        }

        #endregion

        #region Materials

        // 将选中的嵌入式材质提取为独立材质，目录在当前目录下的Materials文件夹中
        [MenuItem("Assets/PH 提取该材质", false)]
        private static void ExtractMaterials()
        {
            Material material = Selection.objects[0] as Material;
            Material extractedMaterial = MaterialToolsModule.ExtractMaterial(material);
            Debug.Log($"材质已提取到：{AssetDatabase.GetAssetPath(extractedMaterial)}");
            Selection.activeObject = extractedMaterial;
        }
        
        [MenuItem("Assets/PH 提取该材质", true)]
        private static bool ValidateExtractMaterials()
        {
            return Selection.objects != null && Selection.objects.Length == 1 && Selection.objects[0] as Material != null && MaterialToolsModule.IsEmbeddedMaterial(Selection.objects[0] as Material);
        }

        #endregion
    }

}