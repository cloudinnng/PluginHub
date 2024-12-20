using PluginHub.Runtime;
using UnityEditor.Actions;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    // Unity6以后，场景视图有了自己的右键菜单，可以使用CONTEXT/GameObjectToolContext/Go To Mouse Pos这种方式添加菜单项
    // 操作方式有变化：
    // 1. 右键点击场景视图，选择Go To Mouse Pos菜单
    // 2. 在场景视图中单击，会自动移动到点击位置
    // 3. 按ESC键可以终止操作
    public class GoToMousePosEA : EditorAction
    {

        private Vector3 targetPosition;
        private bool hasValidPosition;

        [MenuItem("CONTEXT/GameObjectToolContext/Go To Mouse Pos")]
        static void Init()
        {
            EditorAction.Start<GoToMousePosEA>();
        }

        public override void OnSceneGUI(SceneView view)
        {
            var evt = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);

            // 添加ESC键检测
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                Debug.Log("已终止Go To Mouse Pos操作");
                evt.Use();
                Finish(EditorActionResult.Canceled);
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            hasValidPosition = RaycastWithoutCollider.Raycast(ray.origin, ray.direction, out RaycastWithoutCollider.HitResult result);
            if (hasValidPosition)
            {
                targetPosition = result.hitPoint;
                HandleUtility.AddControl(id, 0);
                
                Handles.color = Color.green;
                Handles.DrawWireCube(targetPosition, Vector3.one * 0.3f);
            }

            if (evt.type == EventType.MouseDown && evt.button == 0 && evt.modifiers == EventModifiers.None)
            {
                if (hasValidPosition)
                {
                    float distance = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, targetPosition);
                    SceneCameraTween.GoTo(targetPosition, distance / 12, SceneView.lastActiveSceneView.rotation);
                    Debug.Log(result.renderer.name, result.renderer.gameObject);
                    evt.Use();
                }
                else
                {
                    Debug.Log("没有检测到可用的网格");
                    evt.Use();
                }
            }
        }
    }
}