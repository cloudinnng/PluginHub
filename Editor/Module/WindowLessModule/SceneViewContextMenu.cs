using System.Collections.Generic;
using System.IO;
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
        public static Vector2 mouseDownPosition { get; private set; }
        public static Vector2 mouseCurrPosition { get; private set; }
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
            mouseCurrPosition = e.mousePosition;
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
            menu.AddItem(new GUIContent("Go To Mouse Pos"), false, GoToMousePos);
            menu.AddItem(new GUIContent("Move Selection To Here"), false, Selection.gameObjects.Length > 0 ? MoveSelectionToHere : null);
            menu.AddItem(new GUIContent("The Material Here"), false, TheMaterialHere);
            menu.AddSeparator("");
            // 命令
            menu.AddItem(new GUIContent("Bake Lighting"), false, () => Lightmapping.BakeAsync());
            menu.AddSeparator("");
            // 打开窗口
            menu.AddItem(new GUIContent("Open Window/Project Settings..."), false, () => EditorApplication.ExecuteMenuItem("Edit/Project Settings..."));
            menu.AddItem(new GUIContent("Open Window/Package Manager"), false, () => EditorApplication.ExecuteMenuItem("Window/Package Manager"));
            menu.AddItem(new GUIContent("Open Window/Preferences..."), false, () => EditorApplication.ExecuteMenuItem("Edit/Preferences..."));
            menu.AddSeparator("Open Window/");
            menu.AddItem(new GUIContent("Open Window/Animation"), false, () => EditorApplication.ExecuteMenuItem("Window/Animation/Animation"));
            menu.AddItem(new GUIContent("Open Window/Timeline"), false, () => EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline"));
            menu.AddSeparator("Open Window/");
            menu.AddItem(new GUIContent("Open Window/Lighting"), false, () => EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting"));
            menu.AddItem(new GUIContent("Open Window/Light Explorer"), false, () => EditorApplication.ExecuteMenuItem("Window/Rendering/Light Explorer"));
            menu.AddSeparator("Open Window/");
            menu.AddItem(new GUIContent("Open Window/Test Runner"), false, () => EditorApplication.ExecuteMenuItem("Window/General/Test Runner"));
            //打开文件夹
            menu.AddItem(new GUIContent("Open Folder/StreamingAssets"), false, () => EditorUtility.RevealInFinder(Application.streamingAssetsPath + "/"));
            menu.AddItem(new GUIContent("Open Folder/PersistentDataPath"), false, () => EditorUtility.RevealInFinder(Application.persistentDataPath + "/"));
            menu.AddItem(new GUIContent("Open Folder/DataPath"), false, () => EditorUtility.RevealInFinder(Application.dataPath + "/"));
            menu.AddSeparator("Open Folder/");
            menu.AddItem(new GUIContent("Open Folder/Packages"), false, () => EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "../Packages/")));
            menu.AddItem(new GUIContent("Open Folder/ProjectSettings"), false, () => EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "../ProjectSettings/")));
            menu.AddItem(new GUIContent("Open Folder/Logs"), false, () => EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "../Logs/")));
            menu.AddSeparator("Open Folder/");
            menu.AddItem(new GUIContent("Open Folder/Build"), false, () => EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "../Build/")));
            menu.AddItem(new GUIContent("Open Folder/Recordings"), false, () => EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "../Recordings/")));
            menu.AddItem(new GUIContent("Open Folder/ExternalAssets"), false, () => EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "../ExternalAssets/")));
            menu.AddSeparator("");
            // 相机
            menu.AddItem(new GUIContent("Camera Orientation/Top South ▽"), false, () => GoToCameraOrientation(CameraOrientation.TopSouth));
            menu.AddItem(new GUIContent("Camera Orientation/Top North △"), false, () => GoToCameraOrientation(CameraOrientation.TopNorth));
            menu.AddItem(new GUIContent("Camera Orientation/Top East ▷"), false, () => GoToCameraOrientation(CameraOrientation.TopEast));
            menu.AddItem(new GUIContent("Camera Orientation/Top West ◁"), false, () => GoToCameraOrientation(CameraOrientation.TopWest));
            menu.AddItem(new GUIContent("Camera Draw Mode/Shaded"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.Normal), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Normal));
            menu.AddItem(new GUIContent("Camera Draw Mode/Wireframe"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.Wireframe), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Wireframe));
            menu.AddItem(new GUIContent("Camera Draw Mode/Shaded Wireframe"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.TexturedWire), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.TexturedWire));
            menu.AddItem(new GUIContent("Camera Draw Mode/Lightmap"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.BakedLightmap), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.BakedLightmap));

            menu.ShowAsContext();//显示菜单
        }

        #region Menu Command

        private static void GoToMousePos()
        {
            if (MousePosToWorldPos(out Vector3 worldPos, out float distance))
                SceneCameraTween.GoTo(worldPos, distance / 12 > 0.5f ? distance / 12 : 0.5f, SceneView.lastActiveSceneView.rotation);
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

        private enum CameraOrientation
        {
            TopSouth,
            TopNorth,
            TopEast,
            TopWest,
        }
        // 移动场景视图相机到指定方向,类似于SceneView右上角的Orientation Gizmo工具功能。目前只提供几个没有的顶视图方向。
        private static void GoToCameraOrientation(CameraOrientation cameraOrientation)
        {
            bool hit = MousePosToWorldPos(out Vector3 worldPoint, out float distance);
            if (!hit)
                worldPoint = SceneView.lastActiveSceneView.pivot;
            switch (cameraOrientation)
            {
                case CameraOrientation.TopSouth:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.Euler(90, 180, 0));
                    break;
                case CameraOrientation.TopNorth:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.Euler(90, 0, 0));
                    break;
                case CameraOrientation.TopEast:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.Euler(90, 90, 0));
                    break;
                case CameraOrientation.TopWest:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.Euler(90, 270, 0));
                    break;
            }
        }

        #endregion


        #region Helper Functions

        //获取一个由[场景视图相机]到[鼠标位置]的射线
        private static Ray SceneViewMouseRay()
        {
            Vector2 mousePos = mouseDownPosition;
            Rect sceneViewRect = SceneView.lastActiveSceneView.position;
            // 0,0在左下角，1,1在右上角
            Vector2 viewPos = new Vector2(mousePos.x / sceneViewRect.width, 1 - mousePos.y / sceneViewRect.height);
            Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(viewPos);
            return ray;
        }

        // 获取场景视图鼠标击中的Mesh的世界坐标
        private static bool MousePosToWorldPos(out Vector3 worldPos, out float distanceOut)
        {
            worldPos = Vector3.zero;
            distanceOut = 0;
            Ray ray = SceneViewMouseRay();
            bool succeed = RaycastWithoutCollider.Raycast(ray.origin, ray.direction, out RaycastWithoutCollider.RaycastResult result);
            if (!succeed)
                return false;
            worldPos = result.hitPoint;
            distanceOut = result.distance;
            DebugEx.DebugPointArrow(result.hitPoint,result.hitNormal,Color.red,0.2f,3f);
            return true;
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