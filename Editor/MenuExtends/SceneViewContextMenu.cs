using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginHub.Extends;
using PluginHub.Helper;
using PluginHub.Module;
using UnityEditor;
using UnityEngine;

namespace PluginHub.MenuExtend
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

            // 打开窗口和打开文件夹
            // 最近使用菜单。。。
            for(int i = recentOpenItems.Count - 1; i >= 0; i--)
            {
                var item = recentOpenItems[i];
                menu.AddItem(new GUIContent($"_{item.name} (Recent)"), false, () => ExecuteRecentOpenItem(item));
            }
            // 打开窗口
            menu.AddItem(new GUIContent("Open Window/Project Settings..."), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Project Settings","Edit/Project Settings...")));
            menu.AddItem(new GUIContent("Open Window/Package Manager"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Package Manager","Window/Package Manager")));
            menu.AddItem(new GUIContent("Open Window/Preferences..."), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Preferences","Edit/Preferences...")));
            menu.AddSeparator("Open Window/");
            menu.AddItem(new GUIContent("Open Window/Animation"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Animation","Window/Animation/Animation")));
            menu.AddItem(new GUIContent("Open Window/Timeline"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Timeline","Window/Sequencing/Timeline")));
            menu.AddSeparator("Open Window/");
            menu.AddItem(new GUIContent("Open Window/Lighting"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Lighting","Window/Rendering/Lighting")));
            menu.AddItem(new GUIContent("Open Window/Light Explorer"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Light Explorer","Window/Rendering/Light Explorer")));
            menu.AddItem(new GUIContent("Open Window/UV Inspector"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"UV Inspector","Window/nTools/UV Inspector")));
            menu.AddSeparator("Open Window/");
            menu.AddItem(new GUIContent("Open Window/Test Runner"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Window,"Test Runner","Window/General/Test Runner")));
            //打开文件夹
            menu.AddItem(new GUIContent("Open Folder/StreamingAssets"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Folder,"StreamingAssets",Application.streamingAssetsPath + "/")));
            menu.AddItem(new GUIContent("Open Folder/PersistentDataPath"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Folder,"PersistentDataPath",Application.persistentDataPath + "/")));
            menu.AddItem(new GUIContent("Open Folder/DataPath"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Folder,"DataPath",Application.dataPath + "/")));
            menu.AddSeparator("Open Folder/");
            menu.AddItem(new GUIContent("Open Folder/Build"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Folder,"Build",ProjectRootPath() +"Build/")));
            menu.AddItem(new GUIContent("Open Folder/Recordings"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Folder,"Recordings",ProjectRootPath() + "Recordings/")));
            menu.AddItem(new GUIContent("Open Folder/ExternalAssets"), false, () => ExecuteRecentOpenItem(new RecentOpenItem(RecentOpenType.Folder, "ExternalAssets", ProjectRootPath() + "ExternalAssets/")));
            menu.AddSeparator("");

            // 相机
            menu.AddItem(new GUIContent("Camera Orientation/Top Z+"), false, () => GoToCameraOrientation(CameraOrientation.TopZPositive));
            menu.AddItem(new GUIContent("Camera Orientation/Top Z-"), false, () => GoToCameraOrientation(CameraOrientation.TopZNegative));
            menu.AddItem(new GUIContent("Camera Orientation/Top X+"), false, () => GoToCameraOrientation(CameraOrientation.TopXPositive));
            menu.AddItem(new GUIContent("Camera Orientation/Top X-"), false, () => GoToCameraOrientation(CameraOrientation.TopXNegative));
            menu.AddItem(new GUIContent("Camera Draw Mode/Shaded"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.Normal), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Normal));
            menu.AddItem(new GUIContent("Camera Draw Mode/Wireframe"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.Wireframe), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Wireframe));
            menu.AddItem(new GUIContent("Camera Draw Mode/Shaded Wireframe"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.TexturedWire), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.TexturedWire));
            menu.AddItem(new GUIContent("Camera Draw Mode/Lightmap"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.BakedLightmap), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.BakedLightmap));
            menu.AddSeparator("");
            // 截图
            menu.AddItem(new GUIContent("Scene View Screenshot"), false, () => SceneGameScreenShot.ScreenShotSceneView());
            menu.AddItem(new GUIContent("Game View ScreenShot"), false, () => SceneGameScreenShot.ScreenShotGameView());

            menu.ShowAsContext();//显示菜单
        }

        #region Open Folder/Window 的最近使用功能
        private enum RecentOpenType
        {
            Folder,
            Window,
        }
        private class RecentOpenItem
        {
            public RecentOpenType type;
            public string name;
            public string path;
            public RecentOpenItem(RecentOpenType type, string name, string path)
            {
                this.name = name;
                this.type = type;
                this.path = path;
            }
        }

        private static List<RecentOpenItem> recentOpenItems = new List<RecentOpenItem>();

        private static void ExecuteRecentOpenItem(RecentOpenItem item)
        {
            // 执行
            switch (item.type)
            {
                case RecentOpenType.Folder:
                    if (!Directory.Exists(item.path))
                    {
                        Debug.Log($"{item.path} not exist, create it.");
                        Directory.CreateDirectory(item.path);
                    }
                    EditorUtility.RevealInFinder(item.path);
                    break;
                case RecentOpenType.Window:
                    EditorApplication.ExecuteMenuItem(item.path);
                    break;
            }
            // 记录最近使用
            if (recentOpenItems.Contains(item))
                recentOpenItems.Remove(item);
            recentOpenItems.Add(item);
            if (recentOpenItems.Count > 5)
                recentOpenItems.Remove(recentOpenItems.First());
        }

        // /Users/ttw/ProjectUnity/TestProject/
        private static string ProjectRootPath()
        {
            string path = Application.dataPath;
            path = path.Substring(0, path.Length - 6);
            return path;
        }

        #endregion


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
                DebugEx.DebugPointArrow(result.hitPoint, result.hitNormal, Color.red, 0.2f, 3f);
                Debug.Log($"选择了{result.meshRenderer.name},第{index}个材质。", result.meshRenderer.gameObject);
            }
            else
                Debug.Log("No mesh renderer hit");
        }

        private enum CameraOrientation
        {
            TopZPositive,
            TopZNegative,
            TopXPositive,
            TopXNegative,
        }
        // 移动场景视图相机到指定方向,类似于SceneView右上角的Orientation Gizmo工具功能。目前只提供几个没有的顶视图方向。
        private static void GoToCameraOrientation(CameraOrientation cameraOrientation)
        {
            bool hit = MousePosToWorldPos(out Vector3 worldPoint, out float distance);
            if (!hit)
                worldPoint = SceneView.lastActiveSceneView.pivot;
            switch (cameraOrientation)
            {
                case CameraOrientation.TopZPositive:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.LookRotation(Vector3.down, Vector3.forward));
                    break;
                case CameraOrientation.TopZNegative:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.LookRotation(Vector3.down, Vector3.back));
                    break;
                case CameraOrientation.TopXPositive:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.LookRotation(Vector3.down, Vector3.right));
                    break;
                case CameraOrientation.TopXNegative:
                    SceneCameraTween.GoTo(worldPoint, SceneView.lastActiveSceneView.size, Quaternion.LookRotation(Vector3.down, Vector3.left));
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