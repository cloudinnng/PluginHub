using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    public class CommonTools
    {
        private static Vector2 _iconBtnSize = new Vector2(20, 20);
        private static GUIStyle _iconBtnStyle;
        private static GUIStyle iconBtnStyle
        {
            get
            {
                if (_iconBtnStyle == null)
                {
                    _iconBtnStyle = new GUIStyle(GUI.skin.button);
                    _iconBtnStyle.border = new RectOffset(0, 0, 0, 0);
                    _iconBtnStyle.padding = new RectOffset(1, 1, 0, 0);
                    _iconBtnStyle.margin = new RectOffset(3, 3, 0, 0);
                }
                return _iconBtnStyle;
            }
        }

        public static void DrawTools()
        {
            GUILayout.BeginHorizontal();
            {
                // 检查选中物体
                if (Selection.activeGameObject != null)
                {
                    var selectedObject = Selection.activeGameObject;
                    if (selectedObject.GetComponent<MeshRenderer>())
                    {
                        if (GUILayout.Button(PluginHubFunc.GuiContent("↓","放到地上"),iconBtnStyle, GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                        {
                            SelectionObjToGround(false);
                        }
                    }
                }

                if (GUILayout.Button(PluginHubFunc.IconContent("d_SceneViewCamera","","移动到Main相机视图"), iconBtnStyle,GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    if(SceneView.lastActiveSceneView != null && Camera.main != null)
                        ViewTweenInitializeOnLoad.GotoCamera(Camera.main, SceneView.lastActiveSceneView);
                }

                GUI.color = PHSceneShiftMenu.NoNeedShift ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("Button Icon","","右键菜单不需要shift,这会使得SceneView中的右键单击直接显示PH菜单，而Unity的菜单将不会显示。"), iconBtnStyle,GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    PHSceneShiftMenu.NoNeedShift = !PHSceneShiftMenu.NoNeedShift;
                }
                GUI.color = Color.white;

                GUI.color = PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay ? PluginHubFunc.SelectedColor : Color.white;
                if (GUILayout.Button(PluginHubFunc.IconContent("d_PhysicsRaycaster Icon","","使用新的方法获取SceneView中的鼠标射线，当旧方法获取的射线不正确时可以使用"), iconBtnStyle,GUILayout.Width(_iconBtnSize.x), GUILayout.Height(_iconBtnSize.y)))
                {
                    PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay = !PHSceneViewMenu.UseNewMethodGetSceneViewMouseRay;
                }
                GUI.color = Color.white;

            }
            GUILayout.EndHorizontal();
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