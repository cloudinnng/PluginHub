using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class MaterialReplaceModule : PluginHubModuleBase
    {
        public override string moduleName { get; } = "材质替换助手";


        private GameObject[] originRootGameObjectsInScene; //场景中包含MeshRenderer的所有根物体，（原材质的原始物体）
        private GameObject replacedObjRoot; //替换后的物体根


        public override void OnEnable()
        {
            base.OnEnable();
            InitRecordableObjects();
        }

        protected override void DrawGuiContent()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label($"材质库：{RecordableObjects.Count}");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("添加选中的材质"))
                {
                    if (Selection.objects.Length > 0)
                    {
                        Material selectMat = Selection.objects[0] as Material;
                        if (selectMat != null && !RecordableObjectsContain(selectMat))
                        {
                            AddRecordableObject(selectMat);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            //画
            for (int i = RecordableObjects.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();
                {
                    Object obj = RecordableObjects[i];
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);

                    GUI.enabled = replacedObjRoot == null;
                    if (GUILayout.Button("替换"))
                    {
                        Replace(obj as Material);
                    }

                    GUI.enabled = true;

                    if (GUILayout.Button("x"))
                    {
                        RemoveRecordableObject(obj);
                    }
                }
                GUILayout.EndHorizontal();
            }


            if (replacedObjRoot != null)
            {
                if (replacedObjRoot.activeInHierarchy)
                {
                    if (GUILayout.Button("还原"))
                    {
                        replacedObjRoot.SetActive(false);
                        foreach (var obj in originRootGameObjectsInScene)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("显示"))
                    {
                        replacedObjRoot.SetActive(true);
                        foreach (var obj in originRootGameObjectsInScene)
                        {
                            obj.SetActive(false);
                        }
                    }
                }

                if (GUILayout.Button("移除"))
                {
                    //显示原对象
                    foreach (var obj in originRootGameObjectsInScene)
                    {
                        obj.SetActive(true);
                    }

                    //移除替换对象
                    GameObject.DestroyImmediate(replacedObjRoot);
                    replacedObjRoot = null;
                }
            }
        }

        private void Replace(Material replaceToMat)
        {
            MeshRenderer[] meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();

            //获取所有包含MeshRenderer的根物体
            HashSet<GameObject> rootGameObjects = new HashSet<GameObject>();
            foreach (var meshRenderer in meshRenderers)
            {
                rootGameObjects.Add(meshRenderer.gameObject.transform.root.gameObject);
            }

            originRootGameObjectsInScene = rootGameObjects.ToArray();

            // Debug.Log(rootGameObjectsInScene.Length);
            replacedObjRoot = new GameObject("__EditorCreate_ReplacedObjRoot");
            //复制一份后放进这个根物体
            foreach (var rootGameObject in originRootGameObjectsInScene)
            {
                GameObject.Instantiate(rootGameObject, replacedObjRoot.transform);
            }

            //替换材质

            //新物体里面所有的子MeshRenderer都要替换材质
            meshRenderers = replacedObjRoot.transform.FindAllsByType<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                Material[] materials = meshRenderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = replaceToMat;
                }

                meshRenderer.sharedMaterials = materials;
            }

            //隐藏原材质的游戏对象
            foreach (var rootGameObject in originRootGameObjectsInScene)
            {
                rootGameObject.SetActive(false);
            }
        }
    }

    public static class TransformEx
    {
        public static T[] FindAllsByType<T>(this Transform parent) where T : Component
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            List<T> returnList = new List<T>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (objs[i].GetType() == typeof(T) && parent.IsMyChild(objs[i].transform))
                    {
                        returnList.Add(objs[i]);
                    }
                }
            }

            return returnList.OrderBy((item) => item.transform.GetSiblingIndex()).ToArray();
        }

        //若A是B的父亲，返回真
        //返回参数transform是否是调用者的直系孩子，或非直系孩子。（只要是孩子就返回真）
        public static bool IsMyChild(this Transform A, Transform B)
        {
            if (B.parent == null)
                return false;
            Transform tmpParent = B.parent;
            while (tmpParent != null)
            {
                if (tmpParent == A)
                    return true;
                tmpParent = tmpParent.parent;
            }

            return false;
        }
    }
}