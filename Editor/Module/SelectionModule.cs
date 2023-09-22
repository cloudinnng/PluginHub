using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using NUnit.Framework.Internal;
using PluginHub.Helper;
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

        private Object selectedObject;
        private GameObject selectedGameObject;

        public override void OnUpdate()
        {
            base.OnUpdate();

            selectedObject = Selection.activeObject;
            selectedGameObject = selectedObject as GameObject;
        }

        protected override void DrawGuiContent()
        {
            if (selectedObject == null)
            {
                DrawRow("Selection", "None");
                return;
            }

            GUILayout.BeginHorizontal();
            {
                string text;
                if (selectedGameObject != null)
                {
                    text = "GameObject";
                }
                else
                {
                    text = "Asset";
                }

                DrawRow("Selection", text);

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(PluginHubFunc.Icon("CollabExclude Icon", "", "Deselect"), PluginHubFunc.IconBtnLayoutOptions))
                {
                    Selection.activeObject = null;
                }
            }
            GUILayout.EndHorizontal();

            string path, name;
            if (selectedGameObject == null) //选中的是一个资产
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                name = Path.GetFileName(path);

                DrawRow("Path", path);
                DrawRow("FileName", name);
            }
            else //选中的是一个游戏对象
            {
                name = selectedGameObject.name;
                StringBuilder sb = new StringBuilder();
                PluginHubFunc.GetFindPath(selectedGameObject.transform, sb);
                path = sb.ToString();


                DrawRow("Name", name);
                DrawRow("Path", path);
                DrawRow("Chind Count", selectedGameObject.transform.childCount.ToString());


                //MeshRenderer
                MeshRenderer[] meshRenderers = selectedGameObject.GetComponentsInChildren<MeshRenderer>();
                
                if (meshRenderers != null && meshRenderers.Length > 0)
                {
                    tmpBounds = meshRenderers[0].bounds;
                    for (int i = 1; i < meshRenderers.Length; i++)
                        tmpBounds.Encapsulate(meshRenderers[i].bounds);
                    Vector3 size = tmpBounds.size;
                    DrawRow("Mesh Bounds Size", $"长:{size.x:F2}m,宽:{size.z:F2}m,高:{size.y:F2}m");
                }
                else
                {
                    tmpBounds = default;
                }
            }

        }

        private Bounds tmpBounds = default;

        private void DrawGameObjectGUI()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.GuiContent("在可见Mesh中间创建父亲","会先计算物体下Mesh的中点位置，然后再该位置创建一个父物体，最后将该物体移动到父物体下。这在不方便使用建模软件修改模型，又想居中对象轴心点的时候很有用。")))
                {
                    MeshRenderer[] meshRenderers = selectedGameObject.GetComponents<MeshRenderer>();
                    if (meshRenderers != null && meshRenderers.Length > 0 && meshRenderers[0] != null)
                    {
                        Bounds bounds = meshRenderers[0].bounds;
                        for (int i = 1; i < meshRenderers.Length; i++)
                            bounds.Encapsulate(meshRenderers[i].bounds);

                        Vector3 meshWorldCenter = bounds.center;

                        GameObject parent = new GameObject("PluginHub_Parent");
                        parent.transform.SetParent(selectedGameObject.transform.parent);
                        parent.transform.position = meshWorldCenter;
                        selectedGameObject.transform.SetParent(parent.transform);
                        Undo.RegisterCreatedObjectUndo(parent, "Create PluginHub_Parent");
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



        public override bool OnSceneGUI(SceneView sceneView)
        {
            if (selectedObject == null)
                return false;
            if (tmpBounds == default)
                return false;

            //画出选中的游戏对象的包围盒
            Handles.DrawWireCube(tmpBounds.center, tmpBounds.size);

            Handles.BeginGUI();
            {
                GUI.color = Color.red;
                //画长
                Vector3 worldPos = tmpBounds.center - new Vector3(0, tmpBounds.extents.y, tmpBounds.extents.z);
                DrawText(worldPos, $"长:{tmpBounds.size.x:F2}m");
                //画宽
                worldPos = tmpBounds.center - new Vector3(tmpBounds.extents.x, tmpBounds.extents.y, 0);
                DrawText(worldPos, $"宽:{tmpBounds.size.z:F2}m");
                //画高
                worldPos = tmpBounds.center - new Vector3(tmpBounds.extents.x, 0, tmpBounds.extents.z);
                DrawText(worldPos, $"高:{tmpBounds.size.y:F2}m");

                GUI.color = Color.white;
            }
            Handles.EndGUI();

            return true;
        }

        public static void DrawText(Vector3 worldPos, string text)
        {
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);

            GUIContent content = new GUIContent(text);
            //caculate text width
            Vector2 textSize = EditorStyles.boldLabel.CalcSize(content);

            Rect rect = new Rect(screenPos.x - textSize.x / 2, screenPos.y - textSize.y / 2, textSize.x, textSize.y);

            GUI.Label(rect, text, EditorStyles.boldLabel);
        }
    }
}