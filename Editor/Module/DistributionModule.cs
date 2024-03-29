using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class DistributionModule : PluginHubModuleBase
    {
        public override string moduleName {
            get
            {
                return "分布放置对象";
            }
        }

        public override string moduleDescription => "";

        //用于放置的对象
        private GameObject placeGameObject;
        private Transform axisRootTransform; //放置的轴的根节点
        private bool destroyOriginObjOnPlaced = true; //放置后是否销毁原始对象
        private Transform[] posToPlace; //待放置的位置

        protected override void DrawGuiContent()
        {
            placeGameObject =
                (GameObject)EditorGUILayout.ObjectField("待放置对象", placeGameObject, typeof(GameObject), false);

            axisRootTransform =
                (Transform)EditorGUILayout.ObjectField("放置轴根节点", axisRootTransform, typeof(Transform), true);

            if (axisRootTransform != null)
            {
                posToPlace = axisRootTransform.GetComponentsInChildren<Transform>();

                posToPlace = posToPlace.Where(t => !t.Equals(axisRootTransform)).ToArray();
                GUILayout.Label($"{posToPlace.Length}个放置点");
            }

            destroyOriginObjOnPlaced = GUILayout.Toggle(destroyOriginObjOnPlaced, "放置后销毁原始对象");

            GUI.enabled = placeGameObject != null && axisRootTransform != null;
            if (GUILayout.Button("Place"))
            {
                foreach (var trans in posToPlace)
                {
                    GameObject newObj = PrefabUtility.InstantiatePrefab(placeGameObject) as GameObject;
                    newObj.transform.position = trans.position;
                }

                //放置完后销毁
                if (destroyOriginObjOnPlaced && placeGameObject != null)
                {
                    GameObject.DestroyImmediate(placeGameObject);
                    placeGameObject = null;
                }
            }

            GUI.enabled = true;

        }

    }
}