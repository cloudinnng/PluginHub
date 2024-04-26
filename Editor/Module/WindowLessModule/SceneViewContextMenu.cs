using System.Collections.Generic;
using System.Linq;
using PluginHub.Extends;
using PluginHub.Helper;
using PluginHub.Module.ModuleScripts;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module.WindowLessModule
{
    // 添加场景视图任意空白位置的右键菜单
    public static class SceneViewContextMenu
    {
        private static Vector2 mouseDownPosition;
        private static double mouseDownTime;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView obj)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                // 记录鼠标按下的位置
                mouseDownPosition = e.mousePosition;
                mouseDownTime = EditorApplication.timeSinceStartup;
            }else if (e.type == EventType.MouseUp && e.button == 1)
            {
                // 被认为是右键单击的条件
                if ((e.mousePosition - mouseDownPosition).magnitude < 3 &&
                    EditorApplication.timeSinceStartup - mouseDownTime < 0.3f)
                {
                    ShowContextMenu(e);
                }
            }
        }

        // 显示右键上下文菜单
        private static void ShowContextMenu(Event e)
        {
            GenericMenu menu = new GenericMenu();
            // 添加菜单
            menu.AddItem(new GUIContent("Go To Mouse Pos"), false, GoToMousePos);
            // 添加菜单
            if(Selection.gameObjects.Length > 0)
                menu.AddItem(new GUIContent("Move Selection To Here"), false, MoveSelectionToHere);
            else
                menu.AddDisabledItem(new GUIContent("Move Selection To Here"));
            // 添加菜单
            menu.AddItem(new GUIContent("The Material Here"), false, TheMaterialHere);

            menu.ShowAsContext();
        }

        #region Menu Command

        private static void GoToMousePos()
        {
            if (MousePosToWorldPos(out Vector3 worldPos, out float distance))
                SceneCameraGoTo(worldPos, distance);
            else
                Debug.Log("No mesh renderer hit");
        }

        private static void MoveSelectionToHere()
        {
            if (MousePosToWorldPos(out Vector3 worldPos, out float distance))
            {
                foreach (var obj in Selection.gameObjects)
                {
                    Undo.RecordObject(obj.transform, "Move Selection To Here");
                    obj.transform.position = worldPos;
                }
            }
            else
                Debug.Log("No mesh renderer hit");
        }

        //选中鼠标指针处的材质
        private static void TheMaterialHere()
        {
            Ray ray = SceneViewMouseRay();
            if (RaycastWithoutCollider.RaycastMeshRenderer(ray.origin, ray.direction,out RaycastWithoutCollider.RaycastResult result))
            {
                Mesh mesh = result.meshRenderer.GetComponent<MeshFilter>().sharedMesh;
                int index = GetSubMeshIndex(mesh, result.triangleIndex);
                if (index == -1)
                    return;

                Selection.objects = new Object[] { result.meshRenderer.sharedMaterials[index] };
                Debug.Log($"选择了{result.meshRenderer.name},第{index}个材质。", result.meshRenderer.gameObject);
            }
            else
                Debug.Log("No mesh renderer hit");
        }

        #endregion


        #region Helper Functions

        //获取一个由场景视图相机到鼠标位置的射线
        private static Ray SceneViewMouseRay()
        {
            Vector2 mousePos = mouseDownPosition;
            mousePos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mousePos.y;
            Ray ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);
            return ray;
        }

        // 获取场景视图鼠标击中的模型的世界坐标
        private static bool MousePosToWorldPos(out Vector3 worldPos, out float distanceOut)
        {
            worldPos = Vector3.zero;
            distanceOut = 0;
            Ray ray = SceneViewMouseRay();
            bool succeed = RaycastWithoutCollider.RaycastMeshRenderer(ray.origin, ray.direction, out RaycastWithoutCollider.RaycastResult result);
            if (!succeed)
                return false;
            worldPos = result.hitPoint;
            distanceOut = result.distance;
            return true;
        }

        private static void SceneCameraGoTo(Vector3 worldPos, float distance)
        {
            SceneCameraTween tween = new SceneCameraTween(worldPos, distance / 12 > 0.5f ? distance / 12 : 0.5f);
            tween.PlayTween(SceneView.lastActiveSceneView);
        }

        //用三角形索引号，获取子网格索引（材质索引），默认认为子mesh中三角形索引不会重复。
        private static int GetSubMeshIndex(Mesh mesh, int triangleIndex)
        {
            int triangleCounter = 0;
            //遍历Mesh的所有子网格
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                int indexCount = mesh.GetSubMesh(subMeshIndex).indexCount;
                triangleCounter += indexCount / 3;
                if (triangleIndex < triangleCounter)
                {
                    return subMeshIndex;
                }
            }
            Debug.LogError($"Failed to find triangle with index {triangleIndex} in mesh '{mesh.name}'. Total triangle count: {triangleCounter}", mesh);
            return -1;
        }
        #endregion

    }
}