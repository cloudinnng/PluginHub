﻿using System.Collections.Generic;
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

            hierachySceneItemHandler(instanceId, selectionRect);

            hierachyActiveObjItemHandler(instanceId, currEvent);

            lastKeyBoardEvent = Event.current.isMouse ? lastKeyBoardEvent : Event.current;

        }

        private static void hierachyActiveObjItemHandler(int instanceId, Event currEvent)
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                return;

            GameObject go = Selection.gameObjects[0];
            // 当前选中的对象的这一行
            if (go != null && go.GetInstanceID() == instanceId)
            {
                // 功能点： 按下鼠标中键可以选中同名的兄弟节点
                if (currEvent.button == 2 && currEvent.type == EventType.MouseUp)
                    SelectSameNameSilbing(go);
            }
        }

        private static void hierachySceneItemHandler(int instanceId, Rect selectionRect)
        {
            // 在层级视图场景GUI"条"上绘制几个快捷按钮
            int count = SceneManager.sceneCount;//目前拖入到层级视图的场景个数
            int loadedCount = 0;//目前已经加载的场景个数

            for (int i = 0; i < count; i++)//用于获取正确的loadedCount值
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
                    if(currentEvent.control)
                        buttonText = "Remove";
                    if(currentEvent.alt)
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
                            if (currentEvent.control)//如果按下了Ctrl键，则是remove场景（将场景从Hierarchy中移除）
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
                            else if (currentEvent.alt)//如果按下了Alt键，则在加载场景后关闭其他场景
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
                            else//啥也没按
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
        }

        [MenuItem("GameObject/PH 拷贝游戏对象名称", false, -50)]
        public static void CopyGameObjectName()
        {
            GameObject gameObject = Selection.activeGameObject;
            if (gameObject != null)
            {
                EditorGUIUtility.systemCopyBuffer = gameObject.name;
                Debug.Log($"{gameObject.name} 已拷贝");
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
                Debug.Log($"{strCopy} 已拷贝");
            }
            else if (TransformEx.AIsBParent(gameObjectB.transform, gameObjectA.transform))
            {
                string strCopy = GetParentChildPath(gameObjectB.transform, gameObjectA.transform);
                EditorGUIUtility.systemCopyBuffer = strCopy;
                Debug.Log($"{strCopy} 已拷贝");
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


        /// TODO
        /// 例如:
        /// ▼GameObject
        ///    ▶Child1
        ///    ▶Child2
        /// ▼GameObject1
        ///    ▶Child1
        ///    ▶Child2
        /// </summary>
        [MenuItem("GameObject/PH 拷贝选中对象层级的字符串表达形式", false, -50)]
        public static void CopyHierarchyStringRepresentation()
        {
            GameObject[] selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Length == 0)
                return;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < selectedGameObjects.Length; i++)
            {
                sb.Append("▼");
                sb.Append(selectedGameObjects[i].name);
                sb.Append("\n");
                Transform[] children = selectedGameObjects[i].GetComponentsInChildren<Transform>();
                for (int j = 0; j < children.Length; j++)
                {
                    if (children[j].parent == selectedGameObjects[i].transform)
                    {
                        sb.Append("    ▶");
                        sb.Append(children[j].name);
                        sb.Append("\n");
                    }
                }
            }
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }
    }
}