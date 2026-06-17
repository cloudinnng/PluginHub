using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PluginHub.Runtime;
using System.Text;
using System.IO;
using Microsoft.Win32;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Unity.CodeEditor;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    public static class CommonTools
    {
        private static readonly Vector2 _iconBtnSize = new Vector2(20, 20);
        private static GUIStyle _iconBtnStyle;
        private static GUIStyle iconBtnStyle
        {
            get
            {
                if (_iconBtnStyle == null)
                {
                    _iconBtnStyle = new GUIStyle(GUI.skin.button)
                    {
                        border = new RectOffset(0, 0, 0, 0),
                        padding = new RectOffset(1, 1, 0, 0),
                        margin = new RectOffset(3, 3, 0, 0)
                    };
                }
                return _iconBtnStyle;
            }
        }

        public static string lastScenePath
        {
            set => EditorPrefs.SetString("PH_SceneOverlayLastScene", value);
            get => EditorPrefs.GetString("PH_SceneOverlayLastScene", "");
        }
        public static string tempScenePath
        {
            set => EditorPrefs.SetString("PH_SceneOverlayTempScene", value);
            get => EditorPrefs.GetString("PH_SceneOverlayTempScene", "");
        }

        private static string _lastSelectedGameObjectPath
        {
            set => EditorPrefs.SetString("PH_SceneOverlayLastSelectedGameObjectPath", value);
            get => EditorPrefs.GetString("PH_SceneOverlayLastSelectedGameObjectPath", "");
        }

        private static string _lastSelectedAssetPath
        {
            set => EditorPrefs.SetString("PH_SceneOverlayLastSelectedAssetPath", value);
            get => EditorPrefs.GetString("PH_SceneOverlayLastSelectedAssetPath", "");
        }

        private static bool _enableRealtimeBtnColor
        {
            set => EditorPrefs.SetBool("PH_SceneOverlayEnableRealtimeBtnColor", value);
            get => EditorPrefs.GetBool("PH_SceneOverlayEnableRealtimeBtnColor", false);
        }

        static CommonTools()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        // Unity在打开场景的时候会先触发旧场景的sceneClosed事件，然后触发新场景的sceneOpened事件
        private static void OnSceneClosed(Scene scene)
        {
            // Debug.Log($"Scene closed: {scene.path}");
            tempScenePath = scene.path;
        }
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // Debug.Log($"Scene opened: {scene.path}");
            //只有打开了一个新的场景时，才更新lastScenePath
            if (tempScenePath != scene.path)
            {
                lastScenePath = tempScenePath;
            }
        }


        public static void DrawTools()
        {
            // PerformanceTest.Start();
            // PerformanceTest.End();

            Color GetShortcutBtnColor(Func<Object> findFunc)
            {
                if (!_enableRealtimeBtnColor)// 关闭实时按钮颜色提醒，不然会卡死
                    return Color.white;
                if (findFunc == null)
                    return PluginHubEditor.Red;

                Object findResult = findFunc.Invoke();
                // Debug.Log(findResult + "" + Selection.activeGameObject);
                if (findResult != null)
                    return (Selection.activeGameObject == findResult || Selection.activeObject == findResult) ? PluginHubEditor.SelectedColor : Color.white;
                else
                    return PluginHubEditor.Red;
            }

            GUILayout.BeginHorizontal();
            {
                if (Selection.activeGameObject != null)
                {
                    StringBuilder sb = new StringBuilder();
                    Selection.activeGameObject.transform.GetFindPath(sb);
                    _lastSelectedGameObjectPath = sb.ToString();
                }
                else
                {
                    if (Selection.activeObject != null)
                    {
                        _lastSelectedAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    }
                }
                // ------------------------------------------------------------

                Object FindGameObjectFunc(string findStr)
                {
                    GameObject[] gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    return gameObjects.FirstOrDefault(g => g.transform.GetFindPath() == findStr);
                }
                Object FindAssetFunc(string assetPath)
                {
                    return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                }
                GameObject FindFirstGameObject<T>(bool printLog) where T : Component
                {
                    T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (printLog)
                    {
                        foreach (var component in components)
                        {
                            Debug.Log(component.gameObject.transform.GetFindPath(), component.gameObject);
                        }
                    }
                    return components.FirstOrDefault()?.gameObject;
                }

                // 选择上次选中的游戏对象
                GUI.color = GetShortcutBtnColor(() => FindGameObjectFunc(_lastSelectedGameObjectPath));
                if (GUILayout.Button(PluginHubEditor.IconContent("tab_prev", "", $"选择上次选中的游戏对象\n{_lastSelectedGameObjectPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeGameObject = FindGameObjectFunc(_lastSelectedGameObjectPath) as GameObject;
                }
                // 选择上次选中的资产文件
                GUI.color = GetShortcutBtnColor(() => FindAssetFunc(_lastSelectedAssetPath));
                if (GUILayout.Button(PluginHubEditor.IconContent("Folder Icon", "", $"选择上次选中的资产文件\n{_lastSelectedAssetPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    string lastAssetPath = _lastSelectedAssetPath;
                    if (!string.IsNullOrEmpty(lastAssetPath))
                    {
                        Selection.activeObject = FindAssetFunc(lastAssetPath);
                    }
                }
                // ------------------------------------------------------------
                // 选择主相机
                GUI.color = GetShortcutBtnColor(() => Camera.main == null ? null : Camera.main.gameObject);
                if (GUILayout.Button(PluginHubEditor.IconContent("Camera Gizmo", "", "选择Main相机"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (Camera.main != null)
                    {
                        Selection.activeGameObject = Camera.main.gameObject;
                        FindFirstGameObject<Camera>(true);// 打印一下
                    }
                }
                // 选择主光源
                GUI.color = GetShortcutBtnColor(() => RenderSettings.sun == null ? null : RenderSettings.sun.gameObject);
                if (GUILayout.Button(PluginHubEditor.IconContent("DirectionalLight Gizmo", "", "选择主光源"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (RenderSettings.sun != null)
                        Selection.activeGameObject = RenderSettings.sun.gameObject;
                }
                // 选择主天空盒
                GUI.color = GetShortcutBtnColor(() => RenderSettings.skybox);
                if (GUILayout.Button(PluginHubEditor.IconContent("d_Skybox Icon", "", "选择天空盒材质"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (RenderSettings.skybox != null)
                        Selection.objects = new Object[] { RenderSettings.skybox };
                }
                // 选择Global Volume
                Object FindGlobalVolumeFunc(bool printLog)
                {
                    Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    transforms = transforms.Where(t =>
                    {
                        Component[] components = t.GetComponents<Component>();
                        bool AnyPredicate(Component component)
                        {
                            return component?.GetType()?.Name.Contains("Volume") ?? false;
                        }
                        return components.Any(AnyPredicate);
                    }).ToArray();
                    if (printLog)
                    {
                        foreach (var transform in transforms)
                        {
                            Debug.Log(transform.GetFindPath(), transform.gameObject);
                        }
                    }
                    return transforms.FirstOrDefault()?.gameObject;
                }

                GUI.color = GetShortcutBtnColor(() => FindGlobalVolumeFunc(false));
                if (GUILayout.Button(PluginHubEditor.IconContent("d_ToolHandleGlobal", "", "选择Global Volume对象"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = FindGlobalVolumeFunc(true);
                }
                // 选择地形
                GUI.color = GetShortcutBtnColor(() => FindFirstGameObject<Terrain>(false));
                if (GUILayout.Button(PluginHubEditor.IconContent("d_Terrain Icon", "", "选择地形"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = FindFirstGameObject<Terrain>(true);
                }
                // 选择UICanvas
                GUI.color = GetShortcutBtnColor(() => FindFirstGameObject<Canvas>(false));
                if (GUILayout.Button(PluginHubEditor.IconContent("Canvas Icon", "", "选择UICanvas"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = FindFirstGameObject<Canvas>(true);
                }
                // 渲染管线资产
                GUI.color = GetShortcutBtnColor(() => GraphicsSettings.defaultRenderPipeline);
                if (GUILayout.Button(PluginHubEditor.IconContent("AssemblyDefinitionAsset Icon", "", "选择渲染管线资产"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = QualitySettings.renderPipeline;
                }
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubEditor.GuiContent("↓", "放到地上"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    SelectionObjToGround();
                }

                GUI.color = _enableRealtimeBtnColor ? PluginHubEditor.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubEditor.IconContent("ColorPicker.CycleColor", "", "是否开启场景常用对象的实时按钮颜色提醒"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    _enableRealtimeBtnColor = !_enableRealtimeBtnColor;
                }
                GUI.color = Color.white;

                if (GUILayout.Button(PluginHubEditor.IconContent("d_SceneViewCamera", "", "场景相机移动到Main相机视图"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (SceneView.lastActiveSceneView != null && Camera.main != null)
                        ViewTweenInitializeOnLoad.GotoCamera(Camera.main, SceneView.lastActiveSceneView);
                }

                if (GUILayout.Button(PluginHubEditor.IconContent("ClothInspector.ViewValue", "", "场景相机移动到选中的对象(GameObject -> Align View To Selected)"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (SceneView.lastActiveSceneView != null && Selection.activeGameObject != null)
                        ViewTweenInitializeOnLoad.GotoTransform(Selection.activeGameObject.transform, SceneView.lastActiveSceneView);
                }

                GUI.color = PHSceneShiftMenu.NoNeedShift ? PluginHubEditor.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubEditor.IconContent("d__Menu", "", "右键菜单不需要shift,这会使得SceneView中的右键单击直接显示PH菜单，而Unity的菜单将不会显示。"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    PHSceneShiftMenu.NoNeedShift = !PHSceneShiftMenu.NoNeedShift;
                }
                GUI.color = Color.white;

                GUI.color = PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay ? PluginHubEditor.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubEditor.IconContent("d_PhysicsRaycaster Icon", "", "使用新的方法获取SceneView中的鼠标射线，当旧方法获取的射线不正确时可以使用"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay = !PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay;
                }
                GUI.color = Color.white;

                if (GUILayout.Button(PluginHubEditor.IconContent("d_SceneAsset Icon", "", $"切换到最近打开的场景 ({lastScenePath})"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Debug.Log("User click to open last scene: " + lastScenePath);
                    if (lastScenePath != null)
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(lastScenePath);
                    }
                }
                // 复制Recording目录中最新的文件
                string recordingDir = Path.Combine(Application.dataPath, "../Recordings");
                if (GUILayout.Button(PluginHubEditor.IconContent("Animation.Record", "", $"复制Recording目录中最新的文件,可直接粘贴到其他软件中\n{recordingDir}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    string[] files = Directory.GetFiles(recordingDir);
                    if (files.Length > 0)
                    {
                        string latestFile = files.OrderByDescending(f => File.GetCreationTime(f)).First();
                        WinClipboard.CopyFiles(new string[] { latestFile });
                        Debug.Log($"复制Recording目录中最新的文件: {latestFile}");
                    }
                }

                string currentEditor = Path.GetFileNameWithoutExtension(CodeEditor.CurrentEditorPath);
                if (currentEditor == "Code") currentEditor = "VS Code";
                string text = (currentEditor != null && currentEditor.Trim() != "") ? currentEditor.Substring(0, 1).ToUpper() : "N/A";
                if (GUILayout.Button(PluginHubEditor.GuiContent(text, $"切换代码编辑器（当前{currentEditor}）\n{CodeEditor.CurrentEditorPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Debug.Log($"[编辑器切换] 当前: {currentEditor}, 路径: {CodeEditor.CurrentEditorPath}");
                    string newEditorPath = GetNextEditorPath(currentEditor);
                    Debug.Log("newEditorPath: " + newEditorPath);
                    if (!string.IsNullOrEmpty(newEditorPath) && File.Exists(newEditorPath))
                    {
                        CodeEditor.SetExternalScriptEditor(newEditorPath);
                        Debug.Log("CurrentEditorPath: " + CodeEditor.CurrentEditorPath);
                    }
                    else
                    {
                        Debug.LogError("newEditorPath: " + newEditorPath + " 不存在");
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        #region Rider 路径检测

        private struct RiderInstallCandidate
        {
            public string SortKey;
            public string Path;
        }

        /// <summary>
        /// 根据当前编辑器名称，循环切换到下一个：VS Code → Cursor → Rider → VS Code
        /// </summary>
        private static string GetNextEditorPath(string currentEditor)
        {
            string editorName = (currentEditor ?? string.Empty).Trim();

            // VS Code -> Cursor
            if (string.Equals(editorName, "VS Code", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(editorName, "Code", StringComparison.OrdinalIgnoreCase))
            {
                string cursorPath = @"C:\Program Files\cursor\Cursor.exe";
                if (!File.Exists(cursorPath))
                {
                    cursorPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Programs", "cursor", "Cursor.exe");
                }
                return cursorPath;
            }

            // Cursor -> Rider
            if (string.Equals(editorName, "Cursor", StringComparison.OrdinalIgnoreCase))
            {
                return TryFindRiderExecutablePath();
            }

            // Rider -> VS Code
            if (IsRiderEditorName(editorName))
            {
                string vscodePath = @"C:\Program Files\Microsoft VS Code\Code.exe";
                if (!File.Exists(vscodePath))
                {
                    vscodePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Programs", "Microsoft VS Code", "Code.exe");
                }
                return vscodePath;
            }

            Debug.LogWarning($"[编辑器切换] 未识别编辑器 '{editorName}'，尝试切换到 Rider");
            return TryFindRiderExecutablePath();
        }

        private static bool IsRiderEditorName(string editorName)
        {
            return string.Equals(editorName, "rider64", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(editorName, "Rider", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 查找 rider64.exe：优先读注册表，再扫描安装目录。
        /// </summary>
        private static string TryFindRiderExecutablePath()
        {
            string fromRegistry = TryFindRiderFromRegistry();
            if (!string.IsNullOrEmpty(fromRegistry))
            {
                return fromRegistry;
            }

            string fromDirectory = TryFindRiderFromInstallDirectories();
            if (!string.IsNullOrEmpty(fromDirectory))
            {
                return fromDirectory;
            }

            Debug.LogError("[Rider检测] 注册表与目录扫描均未找到 rider64.exe");
            return string.Empty;
        }

        /// <summary>
        /// 从 HKLM\SOFTWARE\JetBrains\JetBrains Rider 读取安装路径（JetBrains 官方写入）。
        /// </summary>
        private static string TryFindRiderFromRegistry()
        {
            string[] registryRoots = new string[]
            {
                @"SOFTWARE\JetBrains\JetBrains Rider",
                @"SOFTWARE\WOW6432Node\JetBrains\JetBrains Rider",
            };

            List<RiderInstallCandidate> candidates = new List<RiderInstallCandidate>();

            for (int r = 0; r < registryRoots.Length; r++)
            {
                string registryRoot = registryRoots[r];
                try
                {
                    using RegistryKey riderRootKey = Registry.LocalMachine.OpenSubKey(registryRoot);
                    if (riderRootKey == null)
                    {
                        Debug.Log($"[Rider检测] 注册表项不存在: HKLM\\{registryRoot}");
                        continue;
                    }

                    string[] buildKeys = riderRootKey.GetSubKeyNames();
                    for (int i = 0; i < buildKeys.Length; i++)
                    {
                        string buildKey = buildKeys[i];
                        using RegistryKey buildRegKey = riderRootKey.OpenSubKey(buildKey);
                        if (buildRegKey == null)
                        {
                            continue;
                        }

                        object installDirValue = buildRegKey.GetValue(string.Empty);
                        if (installDirValue == null)
                        {
                            Debug.LogWarning($"[Rider检测] 注册表 {buildKey} 无安装路径");
                            continue;
                        }

                        string installDir = installDirValue.ToString();
                        string riderExe = Path.Combine(installDir, "bin", "rider64.exe");
                        Debug.Log($"[Rider检测] 注册表: build={buildKey}, dir={installDir}, exists={File.Exists(riderExe)}");

                        if (!File.Exists(riderExe))
                        {
                            continue;
                        }

                        candidates.Add(new RiderInstallCandidate
                        {
                            SortKey = buildKey,
                            Path = riderExe,
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Rider检测] 读取注册表 {registryRoot} 失败: {ex.Message}");
                }
            }

            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            string latestPath = candidates
                .OrderByDescending(c => c.SortKey, StringComparer.Ordinal)
                .First()
                .Path;

            Debug.Log($"[Rider检测] 注册表选用: {latestPath}");
            return latestPath;
        }

        /// <summary>
        /// 扫描 Program Files 下各子目录的 bin\rider64.exe。
        /// </summary>
        private static string TryFindRiderFromInstallDirectories()
        {
            List<RiderInstallCandidate> candidates = new List<RiderInstallCandidate>();
            string[] jetBrainsRoots = new string[]
            {
                @"C:\Program Files\JetBrains",
                @"C:\Program Files (x86)\JetBrains",
            };

            for (int r = 0; r < jetBrainsRoots.Length; r++)
            {
                string jetBrainsRoot = jetBrainsRoots[r];
                if (!Directory.Exists(jetBrainsRoot))
                {
                    Debug.Log($"[Rider检测] 目录不存在: {jetBrainsRoot}");
                    continue;
                }

                try
                {
                    string[] installDirs = Directory.GetDirectories(jetBrainsRoot);
                    for (int i = 0; i < installDirs.Length; i++)
                    {
                        string installDir = installDirs[i];
                        string riderExe = Path.Combine(installDir, "bin", "rider64.exe");
                        if (!File.Exists(riderExe))
                        {
                            continue;
                        }

                        string dirName = Path.GetFileName(installDir);
                        candidates.Add(new RiderInstallCandidate
                        {
                            SortKey = TryParseRiderVersionFromName(dirName).ToString(),
                            Path = riderExe,
                        });
                        Debug.Log($"[Rider检测] 目录候选: {riderExe}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Rider检测] 扫描 {jetBrainsRoot} 失败: {ex.Message}");
                }
            }

            // Toolbox: .../apps/Rider/ch-0/<hash>/bin/rider64.exe
            string toolboxRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JetBrains", "Toolbox", "apps", "Rider");
            if (Directory.Exists(toolboxRoot))
            {
                try
                {
                    string[] riderExes = Directory.GetFiles(toolboxRoot, "rider64.exe", SearchOption.AllDirectories);
                    for (int i = 0; i < riderExes.Length; i++)
                    {
                        string riderExe = riderExes[i];
                        string parentDirName = Path.GetFileName(Path.GetDirectoryName(riderExe));
                        if (!string.Equals(parentDirName, "bin", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        candidates.Add(new RiderInstallCandidate
                        {
                            SortKey = TryParseRiderVersionFromPath(riderExe).ToString(),
                            Path = riderExe,
                        });
                        Debug.Log($"[Rider检测] Toolbox 候选: {riderExe}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Rider检测] 扫描 Toolbox 失败: {ex.Message}");
                }
            }

            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            string latestPath = candidates
                .OrderByDescending(c => c.SortKey, StringComparer.Ordinal)
                .First()
                .Path;

            Debug.Log($"[Rider检测] 目录扫描选用: {latestPath}");
            return latestPath;
        }

        /// <summary>
        /// 从安装目录名解析版本，例如 JetBrains Rider 2025.3.3、Rider2024.3。
        /// </summary>
        private static Version TryParseRiderVersionFromName(string dirName)
        {
            Match match = Regex.Match(dirName, @"(?:JetBrains\s+)?Rider\s*(\d{4}\.\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (match.Success && Version.TryParse(match.Groups[1].Value, out Version version))
            {
                return version;
            }

            return new Version(0, 0);
        }

        /// <summary>
        /// 从完整路径解析版本（主要用于 Toolbox 安装）。
        /// </summary>
        private static Version TryParseRiderVersionFromPath(string path)
        {
            Match match = Regex.Match(path, @"(?:JetBrains\s+)?Rider\s*(\d{4}\.\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (match.Success && Version.TryParse(match.Groups[1].Value, out Version version))
            {
                return version;
            }

            return new Version(0, 0);
        }

        #endregion

        private static void SelectionObjToGround()
        {
            Debug.Log("SelectionObjToGround");
            GameObject[] gameObjects = Selection.gameObjects;
            Undo.RecordObjects(gameObjects.Select((o) => o.transform).ToArray(), "SelectionObjToGroundObj");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                MoveGameObjectToGround(gameObjects[i]);
            }
        }

        private static void MoveGameObjectToGround(GameObject obj)
        {
            // 2025年12月8日 优化了此方法，现在可以正确的应用偏移量的将物体“放到地上”，而不是直接将轴点放在地面上
            Vector3 origin = obj.transform.position + Vector3.up * 0.5f;
            // 检测前先禁用，避免检测到自身
            obj.SetActive(false);
            bool raycastResult = RaycastWithoutCollider.Raycast(origin, Vector3.down, out RaycastWithoutCollider.HitResult result);
            obj.SetActive(true);
            if (raycastResult)
            {
                // 获得物体所有渲染器的包围盒
                Bounds bounds = default;
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (bounds == default)
                        bounds = renderer.bounds;
                    else
                        bounds.Encapsulate(renderer.bounds);
                }
                // 物体轴点距离渲染框最低位置的距离
                float yOffset = bounds == default ? 0 : obj.transform.position.y - (bounds.center.y - bounds.extents.y);
                // 放置到地面
                obj.transform.position = new Vector3(obj.transform.position.x, result.hitPoint.y + yOffset, obj.transform.position.z);
            }
            else
            {
                Debug.LogError("未检测到地面");
            }
        }
        /// <summary>
        /// 找出一个对象身上的所有组件，并在其中找世界坐标Y最矮的一个返回
        /// </summary>
        private static T FindLowestComponent<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponentsInChildren<T>();
            int minIndex = 0;
            float minY = 999999;
            for (int i = 0; i < components.Length; i++)
            {
                if (minY > components[i].transform.position.y)
                {
                    minY = components[i].transform.position.y;
                    minIndex = i;
                }
            }

            return (components == null || components.Length == 0) ? null : components[minIndex];
        }
    }
}