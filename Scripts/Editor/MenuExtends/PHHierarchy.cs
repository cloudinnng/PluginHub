using System.Collections.Generic;
using System.Text;
using PluginHub.Editor;
using PluginHub.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    [InitializeOnLoad]
    public static class PHHierarchy
    {
        private static Event lastKeyBoardEvent;

        static PHHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= hierarchyWindowItemOnGUIHandler;
            EditorApplication.hierarchyWindowItemOnGUI += hierarchyWindowItemOnGUIHandler;
        }

        //instanceId: 层级视图中每个行的唯一ID  73372
        static void hierarchyWindowItemOnGUIHandler(int instanceId, Rect selectionRect)
        {
            Event currEvent = Event.current;

            hierarchySceneItemHandler(instanceId, selectionRect);

            hierarchyActiveObjItemHandler(instanceId, currEvent);

            // 为挂有mono脚本的游戏对象添加图标
            hierarchyMonoIconItemHandler(instanceId, selectionRect);

            lastKeyBoardEvent = Event.current.isMouse ? lastKeyBoardEvent : Event.current;
        }

        #region MonoBehaviour Icon

        private static Texture2D _monoBehaviourIconTexture;

        private static Texture2D monoBehaviourIconTexture
        {
            get
            {
                if (_monoBehaviourIconTexture == null)
                {
                    string base64 =
                        "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAMUlEQVQ4EWP8//8/AyWABagZZEIhuYYwkasRpm/UAAaG0TAYDQNQfgClgz+wjEEODQAZqgWLOZX9TgAAAABJRU5ErkJggg==";
                    _monoBehaviourIconTexture = new Texture2D(16, 16);
                    _monoBehaviourIconTexture.LoadImage(System.Convert.FromBase64String(base64));
                }

                return _monoBehaviourIconTexture;
            }
        }

        static Color monoIconColor = new Color(0.1059f, 0.427f, 0.7333f, 0.8f);

        private static void hierarchyMonoIconItemHandler(int instanceId, Rect selectionRect)
        {
            GameObject gameObject = (GameObject)EditorUtility.InstanceIDToObject(instanceId);
            if (gameObject == null) return;
            Component[] components = gameObject.GetComponents<MonoBehaviour>();
            if (components.Length == 0) return; // 没有挂载MonoBehaviour组件

            bool hasUserScript = false;
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component == null) continue;
                string name = component.GetType().FullName;
                // Debug.Log(name,gameObject);
                
                if (!name.Contains("UnityEngine") && !name.Contains("TextMeshPro") && !name.Contains("TMPro"))
                    hasUserScript = true;
            }

            if (!hasUserScript) return;

            GUI.color = monoIconColor;
            selectionRect.width = 16;
            GUI.DrawTexture(selectionRect, monoBehaviourIconTexture);
            GUI.color = Color.white;
        }

        #endregion

        private static void hierarchyActiveObjItemHandler(int instanceId, Event currEvent)
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return;

            GameObject go = Selection.gameObjects[0];
            // 当前选中的对象的这一行
            if (go != null && go.GetInstanceID() == instanceId)
            {
                if (currEvent.modifiers == EventModifiers.None)
                {
                    // 功能点： 按下鼠标中键可以选中所有兄弟节点
                    if (currEvent.button == 2 && currEvent.type == EventType.MouseUp)
                        SelectAllSilbing(go);
                }
                else if (currEvent.modifiers == EventModifiers.Control)
                {
                    // 功能点： 按下Ctrl + 鼠标中键可以选中所有同名兄弟节点
                    if (currEvent.button == 2 && currEvent.type == EventType.MouseUp)
                        SelectSameNameSilbing(go);
                }
                else if (currEvent.modifiers == EventModifiers.Alt)
                {
                    // 功能点： 按下Alt + 鼠标中键可以选中所有相似名字的兄弟节点
                    if (currEvent.button == 2 && currEvent.type == EventType.MouseUp)
                        SelectSimilarNamSilbing(go);
                }else if (currEvent.modifiers == EventModifiers.Shift){
                    // 功能点： 按下Shift + 鼠标中键可以选中父亲节点
                    if (currEvent.button == 2 && currEvent.type == EventType.MouseUp)
                        Selection.activeGameObject = go.transform.parent != null ? go.transform.parent.gameObject : go;
                }
            }
        }

        private static void hierarchySceneItemHandler(int instanceId, Rect selectionRect)
        {
            // 在层级视图场景GUI"条"上绘制几个快捷按钮
            int count = SceneManager.sceneCount; //目前拖入到层级视图的场景个数
            int loadedCount = 0; //目前已经加载的场景个数

            for (int i = 0; i < count; i++) //用于获取正确的loadedCount值
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    loadedCount++;
            }

            for (int i = count - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (instanceId == scene.handle) // 对于场景根节点来说，instanceId 就是 scene.handle
                {
                    Event currentEvent = Event.current;

                    //ping按钮
                    Vector2 sizePing = EditorStyles.label.CalcSize(new GUIContent("Ping"));
                    sizePing.x += 4;
                    Rect pingRect = new Rect(selectionRect.x + selectionRect.width - sizePing.x, selectionRect.y,
                        sizePing.x, sizePing.y);
                    GUI.Label(pingRect, "Ping");

                    //load/unload按钮
                    string buttonText = scene.isLoaded ? "Unload" : "Load";
                    if (currentEvent.shift)
                        buttonText = "Remove";
                    if (currentEvent.alt)
                        buttonText = "Isolate";

                    Vector2 sizeUnload = EditorStyles.label.CalcSize(new GUIContent(buttonText));
                    sizeUnload.x += 4;
                    Rect loadRect = new Rect(selectionRect.x + selectionRect.width - sizeUnload.x - sizePing.x,
                        selectionRect.y, sizeUnload.x, sizeUnload.y);


                    bool enableLoadBtnEvent = false;
                    // 只有一个场景时，不显示unload按钮
                    if (!(loadedCount == 1 && scene.isLoaded))
                    {
                        GUI.Label(loadRect, buttonText);
                        enableLoadBtnEvent = true;
                    }

                    //处理事件----------------------------------------------------------------------------------------
                    if (currentEvent.isMouse && currentEvent.button == 0 && currentEvent.type == EventType.MouseDown)
                    {
                        //ping按钮事件
                        if (pingRect.Contains(currentEvent.mousePosition))
                        {
                            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                            //ping场景资产
                            EditorGUIUtility.PingObject(sceneAsset);
                            //显示Project视图
                            EditorApplication.ExecuteMenuItem("Window/General/Project");
                        }

                        //load/unload/remove按钮事件
                        if (enableLoadBtnEvent && loadRect.Contains(currentEvent.mousePosition))
                        {
                            if (currentEvent.shift) //如果按下了Ctrl键，则是remove场景（将场景从Hierarchy中移除）
                            {
                                //check if scene is dirty
                                if (scene.isDirty)
                                {
                                    if (EditorUtility.DisplayDialog("场景未保存",
                                            $"场景 {scene.name} 未保存，是否保存？", "保存", "不保存"))
                                    {
                                        EditorSceneManager.SaveScene(scene);
                                    }

                                    EditorSceneManager.CloseScene(scene, true);
                                }
                                else
                                {
                                    //直接remove场景
                                    EditorSceneManager.CloseScene(scene, true);
                                }
                            }
                            else if (currentEvent.alt) //如果按下了Alt键，则在加载场景后关闭其他场景
                            {
                                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive); //加载场景
                                //关闭其他场景
                                for (int j = 0; j < count; j++)
                                {
                                    Scene scene1 = SceneManager.GetSceneAt(j);
                                    if (scene1.isLoaded && scene1.handle != scene.handle)
                                        EditorSceneManager.CloseScene(scene1, false);
                                }
                            }
                            else //啥也没按
                            {
                                if (scene.isLoaded)
                                    EditorSceneManager.CloseScene(scene, false); //卸载场景
                                else
                                    EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive); //加载场景
                            }
                        }
                    }
                }
            }
        }

        // 选中同层级中名称相同的游戏对象
        [MenuItem("GameObject/PH SelectSameNameSilbing", false, -50)]
        private static void SelectSameNameSilbingMenuItem()
        {
            SelectSameNameSilbing(Selection.activeGameObject);
        }

        private static void SelectSameNameSilbing(GameObject gameObject)
        {
            Transform parent = gameObject.transform.parent;
            if (parent == null)
            {
                Debug.LogWarning("No Parent");
                return;
            }

            string name = gameObject.name;
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    gameObjects.Add(child.gameObject);
            }

            // Select
            Selection.objects = gameObjects.ToArray();
            Debug.Log($"[PH] Selected {gameObjects.Count} Same Name Silbing");
        }

        private static void SelectSimilarNamSilbing(GameObject gameObject)
        {
            Transform parent = gameObject.transform.parent;
            if (parent == null)
            {
                Debug.LogWarning("No Parent");
                return;
            }

            string name = gameObject.name.Substring(0, gameObject.name.LastIndexOf(" "));
            // Debug.Log(name);
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name))
                    gameObjects.Add(child.gameObject);
            }

            // Select
            Selection.objects = gameObjects.ToArray();
            Debug.Log($"[PH] Selected {gameObjects.Count} Similar Name Silbing");
        }

        private static void SelectAllSilbing(GameObject gameObject)
        {
            Transform parent = gameObject.transform.parent;
            if (parent == null)
            {
                Debug.LogWarning("No Parent");
                return;
            }

            List<GameObject> gameObjects = new List<GameObject>();
            foreach (Transform child in parent)
            {
                gameObjects.Add(child.gameObject);
            }

            // Select
            Selection.objects = gameObjects.ToArray();
            Debug.Log($"[PH] Selected {gameObjects.Count} All Silbing");
        }


        [MenuItem("GameObject/PH 拷贝游戏对象名称", false, -50)]
        public static void CopyGameObjectName()
        {
            GameObject gameObject = Selection.activeGameObject;
            if (gameObject != null)
            {
                EditorGUIUtility.systemCopyBuffer = gameObject.name;
                Debug.Log($"[PH] {gameObject.name} 已拷贝");
            }
        }

        [MenuItem("GameObject/PH 拷贝两个对象父子相对路径", false, -50)]
        public static void CopyRelativePath()
        {
            GameObject gameObjectA = Selection.gameObjects[0];
            GameObject gameObjectB = Selection.gameObjects[1];
            if (TransformEx.AIsBParent(gameObjectA.transform, gameObjectB.transform))
            {
                string strCopy = GetParentChildPath(gameObjectA.transform, gameObjectB.transform);
                EditorGUIUtility.systemCopyBuffer = strCopy;
                Debug.Log($"[PH] {strCopy} 已拷贝");
            }
            else if (TransformEx.AIsBParent(gameObjectB.transform, gameObjectA.transform))
            {
                string strCopy = GetParentChildPath(gameObjectB.transform, gameObjectA.transform);
                EditorGUIUtility.systemCopyBuffer = strCopy;
                Debug.Log($"[PH] {strCopy} 已拷贝");
            }
        }

        //菜单验证函数
        [MenuItem("GameObject/PH 拷贝两个对象父子相对路径", true, -50)]
        public static bool CopyRelativePathValidate()
        {
            if (Selection.gameObjects.Length != 2)
                return false;
            GameObject gameObjectA = Selection.gameObjects[0];
            GameObject gameObjectB = Selection.gameObjects[1];

            if (TransformEx.AIsBParent(gameObjectA.transform, gameObjectB.transform) ||
                TransformEx.AIsBParent(gameObjectB.transform, gameObjectA.transform))
            {
                return true;
            }

            return false;
        }

        //获取父子相对路径  传入的parent必须是child的父亲
        private static string GetParentChildPath(Transform parent, Transform child)
        {
            List<string> stack = new List<string>();
            stack.Add(child.gameObject.name);
            Transform tmpParent = child.parent;
            while (true)
            {
                if (tmpParent == parent) //肯定会退出的
                {
                    stack.Add(parent.gameObject.name);
                    break;
                }

                stack.Add(tmpParent.gameObject.name);
                tmpParent = tmpParent.parent;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = stack.Count - 2; i >= 0; i--)
            {
                sb.Append(stack[i]);
                if (i != 0)
                    sb.Append("/");
            }

            return sb.ToString();
        }

        // [MenuItem("GameObject/PH 批量重命名", false, -52)]
        // public static void BatchRename()
        // {
        //     if (Selection.gameObjects.Length == 0)
        //         return;
        //
        //     GameObject[] gameObjects = Selection.gameObjects;
        //
        //     //input popup window
        //     CFCustomWindows.InputTextPopup.Init("输入前缀",gameObjects[0].name,(namePrefix)=>
        //     {
        //         for (int i = 0; i < gameObjects.Length; i++)
        //             gameObjects[i].name = $"{namePrefix}_{gameObjects[i].transform.GetSiblingIndex()}";
        //
        //        //make scene dirty
        //        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        //     });
        // }

        //创建一个分隔符游戏对象
        [MenuItem("GameObject/PH --------------------", false, -50)]
        public static void CreateSeparator()
        {
            GameObject go = new GameObject("-----------------------------");
        }


        /// <summary>
        /// 将选中对象的层级结构用字符串表达出来
        /// 使用以下字符表示层级：
        /// │ : 竖线，用于连接兄弟节点
        /// ├ : 左分支，用于非最后一个子节点
        /// └ : 左下角，用于最后一个子节点
        /// ─ : 横线，用于连接父子关系
        /// </summary>
        [MenuItem("GameObject/PH 拷贝选中对象层级的字符串表达形式", false, -50)]
        public static void CopyHierarchyStringRepresentation()
        {
            GameObject[] selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Length == 0)
                return;

            StringBuilder sb = new StringBuilder();
            
            // 为每个选中的根对象生成层级结构
            for (int i = 0; i < selectedGameObjects.Length; i++)
            {
                if (i > 0) sb.AppendLine();
                BuildHierarchyString(selectedGameObjects[i].transform, "", true, sb);
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log($"[PH] 已拷贝层级结构:\n{sb}");
        }

        /// <summary>
        /// 递归构建层级结构字符串
        /// </summary>
        /// <param name="transform">当前处理的Transform</param>
        /// <param name="prefix">当前行的前缀（用于显示层级关系）</param>
        /// <param name="isLastChild">是否是父节点的最后一个子节点</param>
        /// <param name="sb">StringBuilder用于构建最终字符串</param>
        private static void BuildHierarchyString(Transform transform, string prefix, bool isLastChild, StringBuilder sb)
        {
            // 添加当前节点
            sb.AppendLine($"{prefix}{(isLastChild ? "└─" : "├─")}{transform.name}");

            // 获取直接子节点
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }

            // 处理所有子节点
            for (int i = 0; i < children.Count; i++)
            {
                // 计算新的前缀：如果当前节点是最后一个子节点，则其子节点前面用空格；否则用竖线
                string newPrefix = prefix + (isLastChild ? "  " : "│ ");
                // 递归处理子节点
                BuildHierarchyString(children[i], newPrefix, i == children.Count - 1, sb);
            }
        }

        [MenuItem("GameObject/PH 拷贝选中对象层级的字符串表达形式", true, -50)]
        public static bool CopyHierarchyStringRepresentationValidate()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("GameObject/PH 拷贝 GameObject.Find 查找路径", false, -49)]
        public static void CopyGameObjectFindPath()
        {
            GameObject gameObject = Selection.activeGameObject;
            if (gameObject != null)
            {
                string path = GetGameObjectFindPath(gameObject.transform);
                EditorGUIUtility.systemCopyBuffer = path;
                Debug.Log($"[PH] {path} 已拷贝");
            }
        }

        #region Helper Functions

        private static string GetGameObjectFindPath(Transform transform)
        {
            StringBuilder sb = new StringBuilder();
            while (transform != null)
            {
                sb.Insert(0, transform.name);
                if (transform.parent != null)
                    sb.Insert(0, "/");
                transform = transform.parent;
            }

            return sb.ToString();
        }

        #endregion
    }
}