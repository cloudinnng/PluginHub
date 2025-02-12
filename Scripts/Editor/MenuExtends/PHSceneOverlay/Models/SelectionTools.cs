using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{

    public class SelectionTools
    {
        public static void DrawSelectionTools()
        {
            // 检查选中物体
            if (Selection.activeGameObject != null)
            {
                var selectedObject = Selection.activeGameObject;
                if (selectedObject.GetComponent<MeshRenderer>())
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("放到地上"))
                        {
                            SelectionObjToGround(false);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private static void SelectionObjToGround(bool detectFromTop)
        {
            GameObject[] gameObjects = Selection.gameObjects;
            Undo.RecordObjects(gameObjects.Select((o) => o.transform).ToArray(), "SelectionObjToGroundObj");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                MoveGameObjectToGround(gameObjects[i], detectFromTop);
            }
        }

        private static void MoveGameObjectToGround(GameObject obj, bool detectFromTop)
        {
            Vector3 origin = detectFromTop? obj.transform.position + Vector3.up * 1000 : obj.transform.position;
            bool raycastResult = RaycastWithoutCollider.Raycast(origin, Vector3.down, out RaycastWithoutCollider.HitResult result);
            if (raycastResult)
            {
                Undo.RecordObject(obj.transform, "Move Selection To Ground");
                obj.transform.position = new Vector3(obj.transform.position.x, result.hitPoint.y, obj.transform.position.z);
            }else
            {
                Debug.LogError("未检测到地面");
            }
        }
        /// <summary>
        /// 找出一个对象身上的所有组件，并在其中找世界坐标Y最矮的一个返回
        /// </summary>
        private static T FindLowestComponent<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponentsInChildren<T>();
            int minIndex = 0;
            float minY = 999999;
            for (int i = 0; i < components.Length; i++)
            {
                if (minY > components[i].transform.position.y)
                {
                    minY = components[i].transform.position.y;
                    minIndex = i;
                }
            }

            return (components == null || components.Length == 0) ? null : components[minIndex];
        }
    }
}