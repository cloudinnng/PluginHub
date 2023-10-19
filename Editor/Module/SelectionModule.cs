
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class SelectionModule : PluginHubModuleBase
    {
        public override string moduleName
        {
            get { return "选择的对象"; }
        }

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


            GUILayout.BeginHorizontal();
            {
                DrawRow("Selection", isGameObject? "GameObject" : "Asset");

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(PluginHubFunc.Icon("CollabExclude Icon", "", "取消选择"), PluginHubFunc.IconBtnLayoutOptions))
                    Selection.activeObject = null;
            }
            GUILayout.EndHorizontal();


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
            string name = Path.GetFileName(path);

            DrawRow("Path", path,true);
            DrawRow("FileName", name);
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

            DrawRow("Name", selectedGameObject.name,false,150);
            DrawRow("Path", sb.ToString(),true,150);
            DrawRow("Chind Count", selectedGameObject.transform.childCount.ToString(),false,150);
            if (_gameObjectBounds != default)
            {
                DrawRow("Mesh Bounds Size", $"长:{_gameObjectBounds.size.x:F2}m,宽:{_gameObjectBounds.size.z:F2}m,高:{_gameObjectBounds.size.y:F2}m",false,150);
                DrawRow("Mesh Bounds Center", $"X:{_gameObjectBounds.center.x:F2}m,Y:{_gameObjectBounds.center.y:F2}m,Z:{_gameObjectBounds.center.z:F2}m",false,150);
            }



            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.GuiContent("在可见Mesh边界框中点创建父物体","会先计算物体下Mesh的中点位置，然后在该位置创建一个父物体，最后将初始物体移动到父物体下。这在不方便使用建模软件修改模型，又想居中对象轴心点的时候很有用。")))
                {
                    if(selectedGameObjects == null || selectedGameObjects.Length == 0)
                        return;

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

                if (GUILayout.Button("选中直接子物体"))
                {
                    List<GameObject> list = new List<GameObject>();
                    for (int i = 0; i < selectedGameObject.transform.childCount; i++)
                    {
                        list.Add(selectedGameObject.transform.GetChild(i).gameObject);
                    }
                    Selection.objects = list.ToArray();
                }
            }
            GUILayout.EndHorizontal();
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
    }
}