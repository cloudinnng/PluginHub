
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginHub.Module
{

    public class TransformValue
    {
        public Vector3 position;
        public Vector3 eulerAngles;
        public Vector3 scale;
    }


    public class SelectionModule : PluginHubModuleBase
    {
        public override string moduleName
        {
            get { return "选择的对象"; }
        }
        public override string moduleDescription => "";
        private Object selectedObject;//选中的对象
        private GameObject selectedGameObject;//选中的游戏对象
        private GameObject[] selectedGameObjects;//选中的游戏对象们

        public override void OnUpdate()
        {
            base.OnUpdate();
            // Debug.Log("SelectionModule OnUpdate");

            selectedObject = Selection.activeObject;
            selectedGameObject = selectedObject as GameObject;
            selectedGameObjects = Selection.gameObjects;
            bool isGameObject = selectedGameObject != null;

            if (isGameObject)
                UpdateGameObject();
            else
                UpdateAsset();
        }

        protected override void DrawGuiContent()
        {
            if (selectedObject == null)
            {
                DrawRow("Selection", "None");
                return;
            }
            bool isGameObject = selectedGameObject != null;

            DrawRow("Selection", isGameObject? "GameObject" : "Asset",false,150);
            DrawRow("Count", Selection.objects.Length.ToString(),false,150);

            DrawRow("--------------------", "--------------------",false,150);

            if (selectedGameObject != null)//选中的是游戏对象
                DrawGameObjectGUI();
            else//选中的是资源
                DrawAssetGUI();
        }

        private void UpdateAsset()
        {

        }

        private void DrawAssetGUI()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
                return;

            string name = Path.GetFileName(path);
            string fullPath = Path.GetFullPath(path);

            DrawRow("FileName", name,true,150);
            DrawRow("Path", path,true,150);
            DrawRow("FullPath", fullPath,true,150);

            if (File.Exists(fullPath))
            {
                FileInfo fileInfo = new FileInfo(fullPath);
                long sizeInBytes = fileInfo.Length;
                DrawRow("Size", $"{GetPrttySize(sizeInBytes)}",false,150);
            }
        }

        private string GetPrttySize(long sizeInBytes)
        {
            if (sizeInBytes < 1024)
                return $"{sizeInBytes} 字节";
            else if (sizeInBytes < 1024 * 1024)
                return $"{sizeInBytes / 1024} KB";
            else if (sizeInBytes < 1024 * 1024 * 1024)
                return $"{sizeInBytes / 1024 / 1024} MB";
            else
                return $"{sizeInBytes / 1024 / 1024 / 1024} GB";
        }

        private Bounds _gameObjectBounds = default;

        private void UpdateGameObject()
        {
            //计算显示包围盒尺寸
            MeshRenderer[] meshRenderers = selectedGameObject.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers != null && meshRenderers.Length > 0)
            {
                _gameObjectBounds = meshRenderers[0].bounds;
                for (int i = 1; i < meshRenderers.Length; i++)
                    _gameObjectBounds.Encapsulate(meshRenderers[i].bounds);
                Vector3 size = _gameObjectBounds.size;
            }
            else
            {
                _gameObjectBounds = default;
            }
        }

        private void DrawGameObjectGUI()
        {
            StringBuilder sb = new StringBuilder();
            PluginHubFunc.GetFindPath(selectedGameObject.transform, sb);

            DrawRow("Name", selectedGameObject.name,true,150);
            DrawRow("Hierarchy Path", sb.ToString(),true,150);
            DrawRow("Chind Count", selectedGameObject.transform.childCount.ToString(),false,150);
            if (_gameObjectBounds != default)
            {
                DrawRow("Mesh Bounds Size", $"长:{_gameObjectBounds.size.x:F2}m,宽:{_gameObjectBounds.size.z:F2}m,高:{_gameObjectBounds.size.y:F2}m",false,150);
                DrawRow("Mesh Bounds Center", $"X:{_gameObjectBounds.center.x:F2}m,Y:{_gameObjectBounds.center.y:F2}m,Z:{_gameObjectBounds.center.z:F2}m",false,150);
            }


            //选择--------------------------------------------------------------
            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("选择：");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("选择所有直接子物体"))
                    {
                        List<GameObject> list = new List<GameObject>();
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                        {
                            Transform transform = selectedGameObjects[i].transform;
                            CollectDirectChild(transform, ref list, CollectType.ActiveChild | CollectType.DeactiveChild);
                        }
                        Debug.Log(list.Count);
                        Selection.objects = list.ToArray();
                    }
                    if (GUILayout.Button("选择active直接子物体"))
                    {
                        List<GameObject> list = new List<GameObject>();
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                        {
                            Transform transform = selectedGameObjects[i].transform;
                            CollectDirectChild(transform,ref list, CollectType.ActiveChild);
                        }
                        Selection.objects = list.ToArray();
                    }
                    if (GUILayout.Button("选择Deactive直接子物体"))
                    {
                        List<GameObject> list = new List<GameObject>();
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                        {
                            Transform transform = selectedGameObjects[i].transform;
                            CollectDirectChild(transform,ref list, CollectType.DeactiveChild);
                        }
                        Selection.objects = list.ToArray();
                    }
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("选择父物体"))
                {
                    List<GameObject> list = new List<GameObject>();
                    for (int i = 0; i < selectedGameObjects.Length; i++)
                    {
                        GameObject selectedGameObject = selectedGameObjects[i];
                        if (selectedGameObject.transform.parent != null)
                            list.Add(selectedGameObject.transform.parent.gameObject);
                    }
                    Selection.objects = list.ToArray();
                }
            }
            GUILayout.EndVertical();

            //操作--------------------------------------------------------------
            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("操作：");
                if (GUILayout.Button("删除所选"))
                {
                    for (int i = 0; i < selectedGameObjects.Length; i++)
                        Undo.DestroyObjectImmediate(selectedGameObjects[i]);
                }
            }
            GUILayout.EndVertical();



            //工具--------------------------------------------------------------
            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("工具：");
                if (GUILayout.Button(PluginHubFunc.GuiContent("在可见Mesh边界框中点创建父物体","会先计算物体下Mesh的中点位置，然后在该位置创建一个父物体，最后将初始物体移动到父物体下。这在不方便使用建模软件修改模型，又想居中对象轴心点的时候很有用。")))
                {
                    if (selectedGameObjects == null || selectedGameObjects.Length == 0)
                    {
                        GUIUtility.ExitGUI();
                        return;
                    }

                    bool isParentPrefab = false;
                    for (int i = 0; i < selectedGameObjects.Length; i++)
                    {
                        if (PrefabUtility.GetPrefabAssetType(selectedGameObjects[i].transform.parent) != PrefabAssetType.NotAPrefab)
                        {
                            isParentPrefab = true;
                            break;
                        }
                    }
                    if (isParentPrefab)
                    {
                        Debug.LogWarning("父物体是Prefab，不允许修改，请先解除Prefab连接");
                        GUIUtility.ExitGUI();
                        return;
                    }


                    List<MeshRenderer> meshRendererList = new List<MeshRenderer>();
                    //收集选中的所有MeshRenderer
                    for (int i = 0; i < selectedGameObjects.Length; i++)
                    {
                        GameObject selectedGameObject = selectedGameObjects[i];
                        MeshRenderer[] mrs = selectedGameObject.GetComponentsInChildren<MeshRenderer>();
                        if (mrs != null && mrs.Length > 0)
                            meshRendererList.AddRange(mrs);
                    }


                    if (meshRendererList != null && meshRendererList.Count > 0 && meshRendererList[0] != null)
                    {
                        //计算中点
                        Bounds bounds = meshRendererList[0].bounds;
                        for (int i = 1; i < meshRendererList.Count; i++)
                            bounds.Encapsulate(meshRendererList[i].bounds);
                        Vector3 meshWorldCenter = bounds.center;

                        //获取所有选中对象中最大的姊妹Index
                        int maxSiblingIndex = 0;
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                        {
                            if (selectedGameObjects[i].transform.GetSiblingIndex() > maxSiblingIndex)
                                maxSiblingIndex = selectedGameObjects[i].transform.GetSiblingIndex();
                        }

                        GameObject parent = new GameObject($"PluginHub_Parent_{maxSiblingIndex}");
                        parent.transform.SetParent(selectedGameObject.transform.parent);
                        parent.transform.position = meshWorldCenter;
                        Undo.RegisterCreatedObjectUndo(parent, "Create PluginHub_Parent");

                        //设置到新的父物体下
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                        {
                            Undo.SetTransformParent(selectedGameObjects[i].transform, parent.transform, "Set Parent");
                        }

                        //移动到合适的位置
                        parent.transform.SetSiblingIndex(maxSiblingIndex - selectedGameObjects.Length + 1);

                        //结束之后选中父物体
                        Selection.activeGameObject = parent;
                    }
                    else
                    {
                        Debug.LogWarning("物体没有MeshRenderer");
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(PluginHubFunc.GuiContent("归零 LocalPosition","归零物体的LocalPosition，但保持子物体Transform不变")))
                    {
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                            ResetTransformKeepChild(selectedGameObjects[i].transform,true,false,false);
                    }

                    if (GUILayout.Button(PluginHubFunc.GuiContent("归零 LocalRotation","归零物体的LocalRotation，但保持子物体Transform不变")))
                    {
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                            ResetTransformKeepChild(selectedGameObjects[i].transform,false,true,false);
                    }

                    if (GUILayout.Button(PluginHubFunc.GuiContent("归零 LocalScale","归零物体的LocalScale，但保持子物体Transform不变")))
                    {
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                            ResetTransformKeepChild(selectedGameObjects[i].transform,false,false,true);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("移动到SiblingIndex最前"))
                    {
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                        {
                            Transform transform = selectedGameObjects[i].transform;
                            transform.SetSiblingIndex(0);
                        }
                    }
                    if (GUILayout.Button("移动到SiblingIndex最后"))
                    {
                        for (int i = 0; i < selectedGameObjects.Length; i++)
                        {
                            Transform transform = selectedGameObjects[i].transform;
                            transform.SetSiblingIndex(transform.parent.childCount - 1);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }




        protected override bool OnSceneGUI(SceneView sceneView)
        {
            if (selectedObject == null)
                return false;
            if (_gameObjectBounds == default)
                return false;

            //画出选中的游戏对象的包围盒
            Handles.DrawWireCube(_gameObjectBounds.center, _gameObjectBounds.size);

            Handles.BeginGUI();
            {
                //画长
                Vector3 worldPos = _gameObjectBounds.center - new Vector3(0, _gameObjectBounds.extents.y, _gameObjectBounds.extents.z);
                DrawText(worldPos, $"长:{_gameObjectBounds.size.x:F2}m");
                //画宽
                worldPos = _gameObjectBounds.center - new Vector3(_gameObjectBounds.extents.x, _gameObjectBounds.extents.y, 0);
                DrawText(worldPos, $"宽:{_gameObjectBounds.size.z:F2}m");
                //画高
                worldPos = _gameObjectBounds.center - new Vector3(_gameObjectBounds.extents.x, 0, _gameObjectBounds.extents.z);
                DrawText(worldPos, $"高:{_gameObjectBounds.size.y:F2}m");
            }
            Handles.EndGUI();

            return true;
        }

        #region Helper Functions
        //在场景视图中画出文字
        public static void DrawText(Vector3 worldPos, string text)
        {
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);

            GUIContent content = new GUIContent(text);
            //caculate text width
            Vector2 textSize = EditorStyles.boldLabel.CalcSize(content);

            Rect rect = new Rect(screenPos.x - textSize.x / 2, screenPos.y - textSize.y / 2, textSize.x, textSize.y);
            GUI.color = Color.black;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, text, EditorStyles.boldLabel);
        }
        private void ResetTransformKeepChild(Transform transform,bool resetPosition,bool resetRotation,bool resetScale)
        {
            //记录所有直接子物体的世界Transform
            List<TransformValue> children = new List<TransformValue>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                TransformValue tv = new TransformValue();
                tv.position = child.position;
                tv.eulerAngles = child.rotation.eulerAngles;
                tv.scale = child.localScale;
                children.Add(tv);
            }
            Undo.RecordObject(transform, "Reset Transform");
            //归零
            if(resetPosition)
                transform.localPosition = Vector3.zero;
            if(resetRotation)
                transform.localRotation = Quaternion.identity;
            if(resetScale)
                transform.localScale = Vector3.one;
            //还原子物体Transform
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                TransformValue tv = children[i];
                Undo.RecordObject(child, "Reset Transform");
                child.position = tv.position;
                child.eulerAngles = tv.eulerAngles;
                child.localScale = tv.scale;
            }
        }


        //标志枚举
        [Flags]
        private enum CollectType
        {
            None = 0,
            DeactiveChild = 1,
            ActiveChild = 2,
        }

        //仅收集直接子物体
        private void CollectDirectChild(Transform transform,ref List<GameObject> list, CollectType collectType)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if ((collectType & CollectType.DeactiveChild) == CollectType.DeactiveChild)
                {
                    if (!child.gameObject.activeSelf)
                        list.Add(child.gameObject);
                }
                if ((collectType & CollectType.ActiveChild) == CollectType.ActiveChild)
                {
                    if (child.gameObject.activeSelf)
                        list.Add(child.gameObject);
                }
            }
        }


        #endregion

    }
}