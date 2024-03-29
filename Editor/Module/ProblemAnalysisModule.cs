using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class ProblemAnalysisModule : PluginHubModuleBase
    {
        public override string moduleName
        {
            get { return "问题分析"; }
        }
        public override string moduleDescription => "";
        private Material matObject;


        protected override void DrawGuiContent()
        {
            matObject = (Material)EditorGUILayout.ObjectField(matObject, typeof(Material), false);

            if (GUILayout.Button("打印空引用材质槽（如有）"))
            {
                List<MeshRenderer> mrList = new List<MeshRenderer>();
                GameObject.FindObjectsOfType<MeshRenderer>().ToList().ForEach((mr) =>
                {
                    Material[] smat = mr.sharedMaterials;
                    smat.ToList().ForEach((m) =>
                    {
                        if (m == null)
                        {
                            Debug.Log($"{mr.name}对象有空材质槽", mr);
                            mrList.Add(mr);
                        }
                    });
                });
                Debug.Log($"{mrList.Count}个紫色物体");
                Selection.objects = mrList.Select((mr) => mr.gameObject).ToArray();
            }

            GUI.enabled = matObject != null;
            if (GUILayout.Button("使用上面的材质解决所有的空材质紫色问题"))
            {
                GameObject.FindObjectsOfType<MeshRenderer>().ToList().ForEach((mr) =>
                {
                    Material[] smat = mr.sharedMaterials;
                    bool hasNull = false;

                    for (int i = 0; i < smat.Length; i++)
                    {
                        Material material = smat[i];
                        if (material == null)
                        {
                            hasNull = true;
                            smat[i] = matObject;
                        }
                    }

                    if (hasNull)
                    {
                        mr.sharedMaterials = smat;
                    }

                });
            }

            GUI.enabled = true;
        }
    }
}