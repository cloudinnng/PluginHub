using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
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

        private Object selectObj;
        private GameObject selectGameObject;

        protected override void DrawGuiContent()
        {
            selectObj = Selection.activeObject;
            selectGameObject = selectObj as GameObject;

            if (selectObj == null)
            {
                DrawRow("Selection", "None");
                return;
            }

            GUILayout.BeginHorizontal();
            {
                string text;
                if (selectGameObject != null)
                {
                    text = "GameObject";
                }
                else
                {
                    text = "Asset";
                }

                DrawRow("Selection", text);


                GUILayout.FlexibleSpace();
                if (GUILayout.Button(PluginHubFunc.Icon("CollabExclude Icon", "", "Deselect"), PluginHubFunc.IconBtnLOS))
                {
                    Selection.activeObject = null;
                }
            }
            GUILayout.EndHorizontal();

            string path, name;
            if (selectGameObject == null) //选中的是一个资产
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                name = Path.GetFileName(path);

                DrawRow("Path", path);
                DrawRow("FileName", name);
            }
            else //选中的是一个游戏对象
            {
                name = selectGameObject.name;
                StringBuilder sb = new StringBuilder();
                PluginHubFunc.GetFindPath(selectGameObject.transform, sb);
                path = sb.ToString();


                DrawRow("Name", name);
                DrawRow("Path", path);
                DrawRow("Chind Count", selectGameObject.transform.childCount.ToString());


                //MeshRenderer
                MeshRenderer[] meshRenderers = selectGameObject.GetComponentsInChildren<MeshRenderer>();
                
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

            //会在可见网格中点创建父对象（不一定是选中对象的原点），并将选中的对象作为子对象。
            if (GUILayout.Button("在可见Mesh中间创建父亲"))
            {
                MeshRenderer[] meshRenderers = selectGameObject.GetComponents<MeshRenderer>();
                if (meshRenderers != null && meshRenderers.Length > 0 && meshRenderers[0] != null)
                {
                    Bounds bounds = meshRenderers[0].bounds;
                    for (int i = 1; i < meshRenderers.Length; i++)
                        bounds.Encapsulate(meshRenderers[i].bounds);

                    Vector3 meshWorldCenter = bounds.center;

                    GameObject parent = new GameObject("CF_Parent");
                    parent.transform.SetParent(selectGameObject.transform.parent);
                    parent.transform.position = meshWorldCenter;
                    selectGameObject.transform.SetParent(parent.transform);
                    Undo.RegisterCreatedObjectUndo(parent, "Create CF_Parent");
                }
            }

        }

        private Bounds tmpBounds = default;

        public override bool OnSceneGUI(SceneView sceneView)
        {
            if (selectObj == null)
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