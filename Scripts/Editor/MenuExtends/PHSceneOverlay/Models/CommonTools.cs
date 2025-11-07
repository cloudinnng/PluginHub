using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PluginHub.Runtime;
using System.Text;
using System.IO;

namespace PluginHub.Editor
{
    public class CommonTools
    {
        private static Vector2 _iconBtnSize = new Vector2(20, 20);
        private static GUIStyle _iconBtnStyle;
        private static GUIStyle iconBtnStyle
        {
            get
            {
                if (_iconBtnStyle == null)
                {
                    _iconBtnStyle = new GUIStyle(GUI.skin.button);
                    _iconBtnStyle.border = new RectOffset(0, 0, 0, 0);
                    _iconBtnStyle.padding = new RectOffset(1, 1, 0, 0);
                    _iconBtnStyle.margin = new RectOffset(3, 3, 0, 0);
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

        public static void DrawTools()
        {
            // PerformanceTest.Start();
            // PerformanceTest.End();
            GUILayout.BeginHorizontal();
            {
                if (Selection.activeGameObject != null)
                {
                    StringBuilder sb = new StringBuilder();
                    Selection.activeGameObject.transform.GetFindPath(sb);
                    _lastSelectedGameObjectPath = sb.ToString();
                }else{
                    if (Selection.activeObject != null)
                    {
                        _lastSelectedAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    }
                }


                // 按钮，以选择上次选中的游戏对象
                GUI.color = Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("tab_prev", "", $"选择上次选中的游戏对象\n{_lastSelectedGameObjectPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    string lastPath = _lastSelectedGameObjectPath;
                    if (!string.IsNullOrEmpty(lastPath))
                    {
                        // 尝试通过路径查找对象
                        GameObject found = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(g =>
                        {
                            StringBuilder sb = new StringBuilder();
                            g.transform.GetFindPath(sb);
                            return sb.ToString() == lastPath;
                        });
                        if (found != null)
                        {
                            Selection.activeGameObject = found;
                        }
                        else
                        {
                            Debug.LogWarning("未找到上次选中的对象: " + lastPath);
                        }
                    }
                }
                // 选择上次选中的资产文件
                GUI.color = Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("Folder Icon", "", $"选择上次选中的资产文件\n{_lastSelectedAssetPath}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    string lastAssetPath = _lastSelectedAssetPath;
                    if (!string.IsNullOrEmpty(lastAssetPath))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(lastAssetPath);
                        if (obj != null)
                        {
                            Selection.activeObject = obj;
                        }
                        else
                        {
                            Debug.LogWarning("未找到上次选中的资产文件: " + lastAssetPath);
                        }
                    }
                }

                // 选择主相机
                GUI.color = (Camera.main != null && Selection.activeGameObject == Camera.main.gameObject) ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("Camera Gizmo", "", "选择Main相机"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (Camera.main != null)
                        Selection.activeGameObject = Camera.main.gameObject;
                }
                // 选择主光源
                GUI.color = (RenderSettings.sun != null && Selection.activeGameObject == RenderSettings.sun.gameObject) ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("DirectionalLight Gizmo", "", "选择主光源"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (RenderSettings.sun != null)
                        Selection.activeGameObject = RenderSettings.sun.gameObject;
                }
                // 选择主天空盒
                GUI.color = (RenderSettings.skybox != null && Selection.objects.Contains(RenderSettings.skybox)) ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("d_Skybox Icon", "", "选择天空盒材质"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (RenderSettings.skybox != null)
                        Selection.objects = new Object[] { RenderSettings.skybox };
                }
                // 选择Global Volume
                GUI.color = (Selection.gameObjects != null && Selection.gameObjects.Length == 1 && Selection.gameObjects[0].name.Contains("Volume")) ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("d_ToolHandleGlobal", "", "选择名为Global Volume的对象"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    var globalVolume = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.name == "Global Volume");
                    if (globalVolume != null)
                    {
                        Selection.objects = new Object[] { globalVolume.gameObject };
                    }
                }
                // 选择地形
                GUI.color = (Selection.gameObjects != null && Selection.gameObjects.Length == 1 && Selection.gameObjects[0].GetComponent<Terrain>() != null) ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("d_Terrain Icon", "", "选择地形"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    var terrain = GameObject.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
                    if (terrain != null)
                    {
                        Selection.objects = new Object[] { terrain.gameObject };
                    }
                }
                // 选择UICanvas
                GUI.color = (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Canvas>() != null) ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("Canvas Icon", "", "选择UICanvas"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    var allCanvases = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (allCanvases != null && allCanvases.Length > 0)
                    {
                        Selection.activeGameObject = allCanvases[0].gameObject;
                    }
                }
                GUI.color = Color.white;

                // 复制Recording目录中最新的文件
                string recordingDir = Path.Combine(Application.dataPath, "../Recordings");
                if(GUILayout.Button(PluginHubFunc.IconContent("Animation.Record", "", $"复制Recording目录中最新的文件,可直接粘贴到其他软件中\n{recordingDir}"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    string[] files = Directory.GetFiles(recordingDir);
                    if(files.Length > 0)
                    {
                        string latestFile = files.OrderByDescending(f => File.GetCreationTime(f)).First();
                        WinClipboard.CopyFiles(new string[] { latestFile });
                        Debug.Log($"复制Recording目录中最新的文件: {latestFile}");
                    }
                }
        

            }
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            {
                // 检查选中物体
                if (Selection.activeGameObject != null)
                {
                    var selectedObject = Selection.activeGameObject;
                    if (selectedObject.GetComponent<MeshRenderer>())
                    {
                        if (GUILayout.Button(PluginHubFunc.GuiContent("↓", "放到地上"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                        {
                            SelectionObjToGround(false);
                        }
                    }
                }

                if (GUILayout.Button(PluginHubFunc.IconContent("d_SceneViewCamera", "", "移动到Main相机视图"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if (SceneView.lastActiveSceneView != null && Camera.main != null)
                        ViewTweenInitializeOnLoad.GotoCamera(Camera.main, SceneView.lastActiveSceneView);
                }

                GUI.color = PHSceneShiftMenu.NoNeedShift ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("Button Icon", "", "右键菜单不需要shift,这会使得SceneView中的右键单击直接显示PH菜单，而Unity的菜单将不会显示。"), iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
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

            }
            GUILayout.EndHorizontal();
        }

        private static void SelectionObjToGround(bool detectFromTop)
        {
            GameObject[] gameObjects = Selection.gameObjects;
            Undo.RecordObjects(gameObjects.Select((o) => o.transform).ToArray(), "SelectionObjToGroundObj");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                MoveGameObjectToGround(gameObjects[i], detectFromTop);
            }
        }

        private static void MoveGameObjectToGround(GameObject obj, bool detectFromTop)
        {
            Vector3 origin = detectFromTop ? obj.transform.position + Vector3.up * 1000 : obj.transform.position;
            bool raycastResult = RaycastWithoutCollider.Raycast(origin, Vector3.down, out RaycastWithoutCollider.HitResult result);
            if (raycastResult)
            {
                Undo.RecordObject(obj.transform, "Move Selection To Ground");
                obj.transform.position = new Vector3(obj.transform.position.x, result.hitPoint.y, obj.transform.position.z);
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