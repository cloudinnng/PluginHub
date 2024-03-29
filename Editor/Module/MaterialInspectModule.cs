using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginHub.Helper;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
//检视一个目录中的所有材质，便于挑选
    public class MaterialInspectModule : PluginHubModuleBase
    {
        public override string moduleName { get; } = "材质检视";

        public override string moduleDescription => "将材质统一放在一个位置,便于赋值";

        //这里将存储文件夹路径的key添加了项目名称，这样每一个项目的目录都不会冲突
        private string key = $"{PluginHubFunc.ProjectUniquePrefix}_SeamlessMatPath_{Application.companyName}_{Application.productName}";

        //要检视的文件夹路径
        private string folderPath
        {
            get { return EditorPrefs.GetString(key, @"Assets\Materials\Common"); }
            set { EditorPrefs.SetString(key, value); }
        }

        //要检视的文件夹对象
        private Object folderObj;

        //是否检视纹理
        private bool inspectTexture = false;

        private bool showSceneMaterials = false;
        private Material[] sceneMaterials;

        public override void OnEnable()
        {
            base.OnEnable();
            InitRecordableObjects();
        }

        protected override void DrawGuiContent()
        {
            if (moduleDebug)
            {
                GUILayout.Label(key);
            }

            //绘制收藏的材质
            GUILayout.Label("Favorites : ");


            for (int i = RecordableObjects.Count - 1; i >= 0; i--)
            {
                var obj = RecordableObjects[i];
                GUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(obj, typeof(Object), true);
                if (GUILayout.Button("X", GUILayout.Width(28)))
                {
                    RemoveRecordableObject(obj);
                }

                GUILayout.EndHorizontal();
            }

            GUI.enabled = PluginHubFunc.IsSelectMaterial();
            if (GUILayout.Button("添加选中的材质到收藏夹"))
            {
                AddRecordableObject(Selection.objects[0]);
            }

            GUI.enabled = true;



            //存在路径，文件夹对象没加载，就加载出文件夹对象
            if (!string.IsNullOrWhiteSpace(folderPath) && folderObj == null)
            {
                folderObj = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("文件夹拖入这里");
                EditorGUI.BeginChangeCheck();
                folderObj = EditorGUILayout.ObjectField(folderObj, typeof(Object), true);
                if (EditorGUI.EndChangeCheck())
                {
                    folderPath = AssetDatabase.GetAssetPath(folderObj);
                    if (moduleDebug)
                        Debug.Log("Change");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField($"文件夹为: {folderPath}");
                GUILayout.FlexibleSpace();
                EditorGUI.BeginChangeCheck();
                inspectTexture = GUILayout.Toggle(inspectTexture, PluginHubFunc.GuiContent("检视纹理", "会有一定内存消耗"));
                if (EditorGUI.EndChangeCheck())
                {
                    //取消勾选时，调用一下GC
                    if (inspectTexture == false)
                        PluginHubFunc.GC();
                }
            }
            GUILayout.EndHorizontal();

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                string[] matGUIDs = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
                GUILayout.Label($"材质个数: {matGUIDs.Length}");
                for (int i = 0; i < matGUIDs.Length; i++)
                {
                    string matPath = AssetDatabase.GUIDToAssetPath(matGUIDs[i]);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

                    DrawMaterialRow(mat);
                }
            }


            showSceneMaterials = EditorGUILayout.Foldout(showSceneMaterials, "Materials in scene");
            if (showSceneMaterials)
            {
                for (int i = 0; i < sceneMaterials.Length; i++)
                {
                    DrawMaterialRow(sceneMaterials[i]);
                }
            }

        }

        private void DrawMaterialRow(Material mat)
        {
            GUILayout.BeginHorizontal();
            {
                string text = mat.name;
                GUILayout.Label(text, GUILayout.Width(150));
                EditorGUILayout.ObjectField(mat, typeof(Object), true);

                //检视纹理
                if (inspectTexture)
                {
                    Object texObject = null;
                    if (mat.HasProperty("_MainTex"))
                        texObject = mat.mainTexture;
                    EditorGUILayout.ObjectField(texObject, typeof(Texture), true);
                }

                //收藏按钮
                if (GUILayout.Button(PluginHubFunc.Icon("d_Favorite", ""), GUILayout.ExpandWidth(false)))
                {
                    AddRecordableObject(mat);
                }
            }
            GUILayout.EndHorizontal();
        }

        public override void RefreshData()
        {
            base.RefreshData();

            Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
            List<Material> materials = new List<Material>();

            for (int i = 0; i < renderers.Length; i++)
            {
                materials.Add(renderers[i].sharedMaterial);
            }

            sceneMaterials = materials.Where(m => m != null).ToArray();
        }
    }
}