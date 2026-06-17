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
            // Obsolete in unity 6000.3
            // string path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
            string path = AssetDatabase.GetAssetPath(obj);
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
            // Obsolete in unity 6000.3
            // string path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
            string path = AssetDatabase.GetAssetPath(obj);
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

        [MenuItem("Assets/PH Copy Absolute Directory Name", true)]
        public static bool CopyDirectoryNameValidate()
        {
            return !string.IsNullOrWhiteSpace(GetSelectionAssetAbsolutePath());
        }

        [MenuItem("Assets/PH Copy Absolute Directory Name")]
        public static void CopyDirectoryName()
        {
            string path = GetSelectionAssetAbsolutePath();
            // 如果有扩展名（即是文件而不是文件夹），则获取其目录，否则直接返回路径
            if (!string.IsNullOrEmpty(Path.GetExtension(path)))
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
                        Debug.Log($"匹配结束，共找到{counter}个引用", Selection.activeObject);
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

        #region 选择工具

        [MenuItem("Assets/PH 选择场景中所有该Mesh对象", false)]
        private static void SelectAllMeshObjects()
        {
            Mesh mesh = Selection.objects[0] as Mesh;
            List<GameObject> gameObjects = new List<GameObject>();
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                // 这里使用顶点数量判断相同mesh，因为大多情况下没那么多实例复制，所以顶点数量相同则认为相同mesh
                if (meshFilter != null && meshFilter.sharedMesh.vertexCount == mesh.vertexCount)
                {
                    gameObjects.Add(obj);
                }
            }
            Selection.objects = gameObjects.ToArray();
            Debug.Log($"选择了: {gameObjects.Count}");
        }
        [MenuItem("Assets/PH 选择场景中所有该Mesh对象", true)]
        private static bool ValidateSelectAllMeshObjects()
        {
            return Selection.objects != null && Selection.objects.Length > 0 && Selection.objects[0] as Mesh != null;
        }

        [MenuItem("Assets/PH 选择场景中所有引用该Material的对象", false)]
        private static void SelectAllObjectsWithMaterial()
        {
            Material selectedMaterial = Selection.objects[0] as Material;
            if (selectedMaterial == null)
            {
                Debug.LogWarning("请选择一个材质球！");
                return;
            }

            List<GameObject> gameObjects = new List<GameObject>();
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat == selectedMaterial)
                        {
                            gameObjects.Add(obj);
                            break;
                        }
                    }
                }
            }

            Selection.objects = gameObjects.ToArray();
            Debug.Log($"找到引用所选材质球的对象数量: {gameObjects.Count}");
        }

        [MenuItem("Assets/PH 选择场景中所有引用该Material的对象", true)]
        private static bool ValidateSelectAllObjectsWithMaterial()
        {
            return Selection.objects != null && Selection.objects.Length == 1 && Selection.objects[0] as Material != null;
        }
        #endregion


        #region mklink 引入外部文件夹

        // Validate：菜单可用性校验
        //  1. 仅 Windows 平台（mklink 是 Windows cmd 内建命令）
        //  2. 仅允许单选 1 个 Project 视图对象，且必须是文件夹
        [MenuItem("Assets/PH 使用 mklink 引入外部文件夹", true)]
        private static bool ImportExternalFolderUseMklinkValidate()
        {
#if UNITY_EDITOR_WIN
            // 严格单选
            if (Selection.objects == null || Selection.objects.Length != 1)
                return false;

            string projectPath = GetSelectionAssetProjectPath();
            if (string.IsNullOrEmpty(projectPath))
                return false;

            // 必须是文件夹
            return AssetDatabase.IsValidFolder(projectPath);
#else
            // 非 Windows 平台直接隐藏（置灰）
            return false;
#endif
        }

        //用于存储上次选择的mklink外部文件夹路径到EditorPrefs的Key
        private static string lastMklinkFolderPath
        {
            get { return EditorPrefs.GetString("PH_LastMklinkFolderPath", ""); }
            set { EditorPrefs.SetString("PH_LastMklinkFolderPath", value); }
        }

        /// <summary>
        /// mklink 禁止作为 junction 目标的路径：位于 Assets / Library / Temp / Logs 内（含目录自身）。
        /// </summary>
        private static bool MklinkExternalPathIsForbidden(string folderPath, out string matchedForbiddenRoot)
        {
            matchedForbiddenRoot = null;
            string projectRoot = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath))
                .Replace('/', '\\').TrimEnd('\\');
            string[] forbiddenRoots =
            {
                Application.dataPath,
                Path.Combine(projectRoot, "Library"),
                Path.Combine(projectRoot, "Temp"),
                Path.Combine(projectRoot, "Logs"),
            };

            foreach (string root in forbiddenRoots)
            {
                string forbidden = Path.GetFullPath(root).Replace('/', '\\').TrimEnd('\\');
                if (folderPath.Equals(forbidden, System.StringComparison.OrdinalIgnoreCase)
                    || folderPath.StartsWith(forbidden + "\\", System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedForbiddenRoot = forbidden;
                    Debug.Log($"[mklink] 外部路径命中禁止区域：{folderPath} ⊂ {forbidden}");
                    return true;
                }
            }

            return false;
        }

        // 在右键文件夹下创建以外部文件夹名命名的 junction，使外部资源被 Unity 索引
        // 命令: cmd.exe /c mklink /j "右键文件夹\外部文件夹名" "外部文件夹绝对路径"
        [MenuItem("Assets/PH 使用 mklink 引入外部文件夹")]
        private static void ImportExternalFolderUseMklink()
        {
            // 1. 取右键文件夹的绝对路径，作为 junction 的父目录
            string rightClickFolderAbs = GetSelectionAssetAbsolutePath();
            if (string.IsNullOrEmpty(rightClickFolderAbs) || !Directory.Exists(rightClickFolderAbs))
            {
                Debug.LogError($"[mklink] 右键文件夹路径无效：{rightClickFolderAbs}");
                return;
            }
            Debug.Log($"[mklink] 右键文件夹（junction 父目录）：{rightClickFolderAbs}");

            // 2. 弹出文件夹选择面板，让用户选外部文件夹
            string externalFolderPath = EditorUtility.OpenFolderPanel("选择要通过 mklink 引入的外部文件夹", lastMklinkFolderPath, "");
            if (string.IsNullOrEmpty(externalFolderPath))
            {
                // 用户取消，不算错误
                Debug.Log("[mklink] 用户取消了外部文件夹选择");
                return;
            }
            lastMklinkFolderPath = externalFolderPath;
            // 路径规整：拿到绝对路径并统一为反斜杠，去掉末尾分隔符，便于后续比对
            externalFolderPath = Path.GetFullPath(externalFolderPath).Replace('/', '\\').TrimEnd('\\');
            if (!Directory.Exists(externalFolderPath))
            {
                Debug.LogError($"[mklink] 外部文件夹不存在：{externalFolderPath}");
                return;
            }
            Debug.Log($"[mklink] 外部文件夹（junction 目标）：{externalFolderPath}");

            // 3. 边界检查：禁止指向 Assets / Library / Temp / Logs，其余项目根下目录允许
            if (MklinkExternalPathIsForbidden(externalFolderPath, out string forbiddenRoot))
            {
                string forbiddenName = Path.GetFileName(forbiddenRoot);
                string msg =
                    $"外部文件夹不能位于「{forbiddenName}」目录内（会导致重复索引或路径问题），操作已中止。\n\n" +
                    $"外部：{externalFolderPath}\n禁止区域：{forbiddenRoot}";
                EditorUtility.DisplayDialog("操作中止", msg, "确定");
                Debug.LogError($"[mklink] {msg}");
                return;
            }
            Debug.Log("[mklink] 边界检查通过：外部路径不在 Assets / Library / Temp / Logs 内");

            // 4. 推导链接名（= 外部文件夹名），并拼出最终链接的完整路径
            string linkName = Path.GetFileName(externalFolderPath);
            if (string.IsNullOrEmpty(linkName))
            {
                // 例如选了驱动器根 D:\，此时 GetFileName 为空
                Debug.LogError($"[mklink] 无法从外部路径推导出文件夹名（可能选了驱动器根目录）：{externalFolderPath}");
                return;
            }
            string linkFullPath = Path.Combine(rightClickFolderAbs, linkName).Replace('/', '\\');

            // 5. 同名冲突检查：目标位置不能已存在同名文件夹/文件/链接
            if (Directory.Exists(linkFullPath) || File.Exists(linkFullPath))
            {
                string msg = $"目标位置已存在同名条目，操作已中止。请先手动处理：\n{linkFullPath}";
                EditorUtility.DisplayDialog("操作中止", msg, "确定");
                Debug.LogError($"[mklink] {msg}");
                return;
            }

            // 6. 二次确认：把即将执行的命令明明白白给用户看一遍
            //    避免误操作（比如选错了外部文件夹、链接位置不是预期等）
            string confirmMsg =
                $"即将创建 junction（目录联接）：\n\n" +
                $"链接位置：\n{linkFullPath}\n\n" +
                $"指向目标：\n{externalFolderPath}\n\n" +
                $"等价命令：\nmklink /j \"{linkFullPath}\" \"{externalFolderPath}\"\n\n" +
                $"注意：Unity 会在外部文件夹真实位置生成 .meta 文件。";
            if (!EditorUtility.DisplayDialog("确认创建 mklink junction", confirmMsg, "确认创建", "取消"))
            {
                Debug.Log("[mklink] 用户在二次确认对话框中取消");
                return;
            }

            // 7. 调用 cmd.exe 执行 mklink /j（mklink 是 cmd 内建命令，不能直接 Process.Start）
            string args = $"/c mklink /j \"{linkFullPath}\" \"{externalFolderPath}\"";
            Debug.Log($"[mklink] 执行命令：cmd.exe {args}");

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };

            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi))
                {
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string errMsg = $"mklink 执行失败 (ExitCode={process.ExitCode})\nstdout: {stdout}\nstderr: {stderr}";
                        Debug.LogError($"[mklink] {errMsg}");
                        EditorUtility.DisplayDialog("mklink 失败", errMsg, "确定");
                        return;
                    }

                    Debug.Log($"[mklink] 创建成功：{linkFullPath} → {externalFolderPath}\n{stdout}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[mklink] 调用 cmd.exe 抛出异常：{e}");
                EditorUtility.DisplayDialog("mklink 异常", e.Message, "确定");
                return;
            }

            // 8. 刷新 AssetDatabase，让 Unity 索引到 junction 里的内容
            AssetDatabase.Refresh();
            Debug.Log("[mklink] AssetDatabase 已刷新，外部文件夹已可在 Project 视图中访问");
        }

        #endregion
    }

}