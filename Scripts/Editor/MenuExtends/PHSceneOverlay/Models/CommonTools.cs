using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PluginHub.Runtime;
using System.Text;
using System.IO;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Unity.CodeEditor;

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

        public static void DrawTools()
        {
            // PerformanceTest.Start();
            // PerformanceTest.End();

            Color GetShortcutBtnColor(Func<Object> findFunc)
            {
                if (!_enableRealtimeBtnColor)// 关闭实时按钮颜色提醒，不然会卡死
                    return Color.white;
                if (findFunc == null)
                    return PluginHubFunc.Red;

                Object findResult = findFunc.Invoke();
                // Debug.Log(findResult + "" + Selection.activeGameObject);
                if (findResult != null)
                    return (Selection.activeGameObject == findResult || Selection.activeObject == findResult) ? PluginHubFunc.SelectedColor : Color.white;
                else
                    return PluginHubFunc.Red;
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
                if (GUILayout.Button(PluginHubFunc.IconContent("tab_prev", "", $"选择上次选中的游戏对象\n{_lastSelectedGameObjectPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeGameObject = FindGameObjectFunc(_lastSelectedGameObjectPath) as GameObject;
                }
                // 选择上次选中的资产文件
                GUI.color = GetShortcutBtnColor(() => FindAssetFunc(_lastSelectedAssetPath));
                if (GUILayout.Button(PluginHubFunc.IconContent("Folder Icon", "", $"选择上次选中的资产文件\n{_lastSelectedAssetPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
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
                if (GUILayout.Button(PluginHubFunc.IconContent("Camera Gizmo", "", "选择Main相机"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (Camera.main != null)
                    {
                        Selection.activeGameObject = Camera.main.gameObject;
                        FindFirstGameObject<Camera>(true);// 打印一下
                    }
                }
                // 选择主光源
                GUI.color = GetShortcutBtnColor(() => RenderSettings.sun == null ? null : RenderSettings.sun.gameObject);
                if (GUILayout.Button(PluginHubFunc.IconContent("DirectionalLight Gizmo", "", "选择主光源"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (RenderSettings.sun != null)
                        Selection.activeGameObject = RenderSettings.sun.gameObject;
                }
                // 选择主天空盒
                GUI.color = GetShortcutBtnColor(() => RenderSettings.skybox);
                if (GUILayout.Button(PluginHubFunc.IconContent("d_Skybox Icon", "", "选择天空盒材质"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
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
                if (GUILayout.Button(PluginHubFunc.IconContent("d_ToolHandleGlobal", "", "选择Global Volume对象"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = FindGlobalVolumeFunc(true);
                }
                // 选择地形
                GUI.color = GetShortcutBtnColor(() => FindFirstGameObject<Terrain>(false));
                if (GUILayout.Button(PluginHubFunc.IconContent("d_Terrain Icon", "", "选择地形"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = FindFirstGameObject<Terrain>(true);
                }
                // 选择UICanvas
                GUI.color = GetShortcutBtnColor(() => FindFirstGameObject<Canvas>(false));
                if (GUILayout.Button(PluginHubFunc.IconContent("Canvas Icon", "", "选择UICanvas"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = FindFirstGameObject<Canvas>(true);
                }
                // 渲染管线资产
                GUI.color = GetShortcutBtnColor(() => GraphicsSettings.defaultRenderPipeline);
                if (GUILayout.Button(PluginHubFunc.IconContent("AssemblyDefinitionAsset Icon", "", "选择渲染管线资产"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    Selection.activeObject = GraphicsSettings.defaultRenderPipeline;
                }
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.GuiContent("↓", "放到地上"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    SelectionObjToGround();
                }

                GUI.color = _enableRealtimeBtnColor ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("ColorPicker.CycleColor", "", "是否开启场景常用对象的实时按钮颜色提醒"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    _enableRealtimeBtnColor = !_enableRealtimeBtnColor;
                }
                GUI.color = Color.white;

                if (GUILayout.Button(PluginHubFunc.IconContent("d_SceneViewCamera", "", "场景相机移动到Main相机视图"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (SceneView.lastActiveSceneView != null && Camera.main != null)
                        ViewTweenInitializeOnLoad.GotoCamera(Camera.main, SceneView.lastActiveSceneView);
                }

                if (GUILayout.Button(PluginHubFunc.IconContent("ClothInspector.ViewValue", "", "场景相机移动到选中的对象(GameObject -> Align View To Selected)"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (SceneView.lastActiveSceneView != null && Selection.activeGameObject != null)
                        ViewTweenInitializeOnLoad.GotoTransform(Selection.activeGameObject.transform, SceneView.lastActiveSceneView);
                }

                GUI.color = PHSceneShiftMenu.NoNeedShift ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("d__Menu", "", "右键菜单不需要shift,这会使得SceneView中的右键单击直接显示PH菜单，而Unity的菜单将不会显示。"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    PHSceneShiftMenu.NoNeedShift = !PHSceneShiftMenu.NoNeedShift;
                }
                GUI.color = Color.white;

                GUI.color = PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("d_PhysicsRaycaster Icon", "", "使用新的方法获取SceneView中的鼠标射线，当旧方法获取的射线不正确时可以使用"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay = !PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay;
                }
                GUI.color = Color.white;

                if (GUILayout.Button(PluginHubFunc.IconContent("d_SceneAsset Icon", "", $"切换到最近打开的场景 ({lastScenePath})"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (lastScenePath != null)
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(lastScenePath);
                    }
                }
                // 复制Recording目录中最新的文件
                string recordingDir = Path.Combine(Application.dataPath, "../Recordings");
                if (GUILayout.Button(PluginHubFunc.IconContent("Animation.Record", "", $"复制Recording目录中最新的文件,可直接粘贴到其他软件中\n{recordingDir}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
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
                if (GUILayout.Button(PluginHubFunc.GuiContent(currentEditor.Substring(0, 1).ToUpper(), $"切换代码编辑器（当前{currentEditor}）\n{CodeEditor.CurrentEditorPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    var newEditorPath = "";
                    // 循环切换三种编辑器
                    switch (currentEditor)
                    {
                        case "VS Code":
                            // 此路径是为系统安装的Cursor，不是为用户安装的
                            newEditorPath = @"C:\Program Files\cursor\Cursor.exe";
                            break;
                        case "Cursor":
                            // 切换到Rider，为了兼容自动升级后的路径，这里动态查找最新版本的Rider
                            string[] listDir = Directory.GetDirectories(@"C:\Program Files\JetBrains");
                            listDir = listDir.Where(dir => dir.Contains("JetBrains Rider")).ToArray();
                            listDir = listDir.OrderBy(dir => dir).ToArray();
                            // for (int i = 0; i < listDir.Length; i++)
                            // {
                            //     Debug.Log(listDir[i]);
                            // }
                            // 取最后一个，即最新版本
                            newEditorPath = Path.Combine($@"{listDir[^1]}", @"bin\rider64.exe"); 
                            break;
                        case "rider64":
                            // 此路径是为系统安装的vscode，不是为用户安装的
                            newEditorPath = @"C:\Program Files\Microsoft VS Code\Code.exe";
                            break;
                    }
                    CodeEditor.SetExternalScriptEditor(newEditorPath);
                    Debug.Log("CurrentEditorPath: " + CodeEditor.CurrentEditorPath);
                }
            }
            GUILayout.EndHorizontal();
        }

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