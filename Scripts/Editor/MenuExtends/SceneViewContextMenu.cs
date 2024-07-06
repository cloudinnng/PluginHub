using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginHub.Editor;
using PluginHub.Runtime;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    // 场景视图上下文菜单
    // 添加场景视图任意空白位置的右键菜单
    public static class SceneViewContextMenu
    {
        //场景视图游标（跟blender学习），用于辅助其他功能
        public static Vector3 sceneViewCursor { get; private set; }
        public static Vector3 lastSceneViewCursor { get; private set; }
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
                // 无修饰键
                if(e.control || e.alt || e.shift || e.command)
                    return;

                // 被认为是右键单击的条件
                if ((e.mousePosition - mouseDownPosition).magnitude < 3 &&
                    EditorApplication.timeSinceStartup - mouseDownTime < 0.2f)
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
            menu.AddItem(new GUIContent("Selection To Ground"), false, Selection.gameObjects.Length > 0 ? () => SelectionObjToGround(false) : null);
            menu.AddItem(new GUIContent("The Material Here"), false, ()=>TheMaterialHere(out _,out _,out _));
            menu.AddItem(new GUIContent("The Material Here (Auto Extract)"), false, TheMaterialHereAutoExtract);
            menu.AddItem(new GUIContent("Copy Material Ref Here"), false, CopyMaterialReferenceHere);
            menu.AddItem(new GUIContent("Paste Material Ref Here"), false, GUIUtility.systemCopyBuffer.EndsWith(".mat") ? PasteMaterialReferenceHere : null);
            menu.AddItem(new GUIContent("Move Scene Cursor To Here"), false, () =>
            {
                if (MousePosToWorldPos(out Vector3 worldPos, out _))
                {
                    lastSceneViewCursor = sceneViewCursor;
                    sceneViewCursor = worldPos;
                    DebugEx.DebugPoint(sceneViewCursor, Color.green, 0.2f, 3f);
                }
            });
            menu.AddSeparator("");


            // 移动命令
            // menu.AddItem(new GUIContent("Move Selection To Ceiling"), false, () => MoveSelectionToCeiling());
            // menu.AddItem(new GUIContent("Move Selection To Ground"), false, () => MoveSelectionToGround());
            // menu.AddSeparator("");

            // 场景搭建相关命令
            menu.AddItem(new GUIContent("Bake Lighting"), false, () => Lightmapping.BakeAsync());
            menu.AddItem(new GUIContent("Cancel Bake"), false, () => Lightmapping.Cancel());
            menu.AddItem(new GUIContent("Create Light Here"), false, () => CreateNewGameObject<Light>());
            menu.AddItem(new GUIContent("More/Force Stop Bake"), false, () => Lightmapping.ForceStop());
            menu.AddItem(new GUIContent("More/Clear Baked Data"), false, () => Lightmapping.Clear());
            menu.AddItem(new GUIContent("More/Clear Disk Cache"), false, () => Lightmapping.ClearDiskCache());
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
            menu.AddItem(new GUIContent("Camera Draw Mode/BakedUVOverlap"), CameraShowModeModule.CurrDrawCameraModeIs(DrawCameraMode.BakedUVOverlap), () => CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.BakedUVOverlap));
            menu.AddSeparator("");


            // 显示一些信息
            if (Selection.gameObjects.Length > 0)
            {
                menu.AddSeparator("");
                Bounds b = SelectionModule.GetSelectionBounds();
                menu.AddItem(new GUIContent($"Selection Bounds: {b.size}"), false, null);
                menu.AddItem(new GUIContent("Selection Count: " + Selection.gameObjects.Length), false, null);
            }
            menu.AddItem(new GUIContent($"SceneView Cursor: {sceneViewCursor}"), false, null);
            menu.AddItem(new GUIContent($"Distance To Last Cursor: {Vector3.Distance(sceneViewCursor, lastSceneViewCursor)}"), false, null);


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
        private static void TheMaterialHere(out Renderer renderer,out Material material,out int indexOfMaterialInMesh)
        {
            renderer = null; material = null; indexOfMaterialInMesh = -1;
            Ray ray = HandleUtility.GUIPointToWorldRay(mouseDownPosition);
            if (RaycastWithoutCollider.Raycast(ray.origin, ray.direction,out RaycastWithoutCollider.HitResult result))
            {
                renderer = result.renderer;

                Mesh mesh = result.renderer.GetComponent<MeshFilter>().sharedMesh;
                indexOfMaterialInMesh = GetSubMeshIndex(mesh, result.triangleIndex);
                if (indexOfMaterialInMesh == -1)
                    return;

                Material targetMaterial = result.renderer.sharedMaterials[indexOfMaterialInMesh];

                Selection.objects = new Object[] { targetMaterial };
                Debug.Log($"选择了 {result.renderer.name} 第 {indexOfMaterialInMesh} 个材质。", result.renderer.gameObject);
                material = targetMaterial;
            }
            else
                Debug.Log("No mesh renderer hit");
        }

        // 选中鼠标指针处的材质，如果是无法修改的嵌入式材质，则自动提取后将Meshrenderer中的该材质替换为新的自由材质，并选中新的自由材质以便修改参数。
        // 该函数在搭建场景时非常有用
        private static void TheMaterialHereAutoExtract()
        {
            TheMaterialHere(out Renderer renderer, out Material material,out _);
            if (material == null)
                return;

            if (MaterialToolsModule.IsEmbeddedMaterial(material))
            {
                // 提取出来的自由材质
                Material newFreeMaterial = MaterialToolsModule.ExtractMaterial(material);
                // 替换MeshRenderer的材质
                for(int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == material)
                    {
                        Material[] materials = renderer.sharedMaterials;
                        materials[i] = newFreeMaterial;
                        renderer.sharedMaterials = materials;
                    }
                }
                //选中它
                Selection.objects = new Object[] { newFreeMaterial };
            }
            else
            {
                // 本身就是自由材质，直接选中即可，无需处理
                Selection.objects = new Object[] { material };
                Debug.Log("是自由材质，无需提取，直接选中", material);
            }

        }

        // 在鼠标位置提取材质路径并保存到剪贴板
        private static void CopyMaterialReferenceHere()
        {
            TheMaterialHere(out Renderer renderer, out Material material,out _);
            if (material == null)
                return;

            // 复制材质引用
            GUIUtility.systemCopyBuffer = AssetDatabase.GetAssetPath(material);
            Debug.Log($"已复制 {GUIUtility.systemCopyBuffer} 到剪贴板", material);
        }

        // 使用剪切板中的材质路径获取材质引用后，赋值到到鼠标当前位置的MeshRenderer中的对应位置材质栏
        // (需要用 CopyMaterialReferenceHere 复制自由材质后才能粘贴)
        // 此函数用例：场景中有多个相同的对象，调整好一个对象的材质后可以简单的将材质粘贴到其他相同对象上，
        // 可以确保使用最少的材质资产
        private static void PasteMaterialReferenceHere()
        {
            TheMaterialHere(out Renderer renderer, out Material material,out int indexOfMaterialInMesh);
            if (material == null)
                return;

            // 粘贴材质引用
            string path = GUIUtility.systemCopyBuffer;
            Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (newMaterial == null)
            {
                Debug.LogError($"无法加载材质：{path}");
                return;
            }
            Undo.RecordObject(renderer, "Paste Material Reference Here");
            // 将材质 放到MeshRenderer中
            Material[] materials = renderer.sharedMaterials;
            materials[indexOfMaterialInMesh] = newMaterial;
            renderer.sharedMaterials = materials;
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

        private static void MoveSelectionToGround()
        {
            foreach (var obj in Selection.gameObjects)
            {
                bool raycastResult = RaycastWithoutCollider.Raycast(obj.transform.position, Vector3.down, out RaycastWithoutCollider.HitResult result);
                if (raycastResult)
                {
                    Undo.RecordObject(obj.transform, "Move Selection To Ground");
                    obj.transform.position = new Vector3(obj.transform.position.x, result.hitPoint.y, obj.transform.position.z);
                }
            }
        }

        private static void MoveSelectionToCeiling()
        {
            foreach (var obj in Selection.gameObjects)
            {
                bool raycastResult = RaycastWithoutCollider.Raycast(obj.transform.position, Vector3.up, out RaycastWithoutCollider.HitResult result);
                if (raycastResult)
                {
                    Undo.RecordObject(obj.transform, "Move Selection To Ceiling");
                    obj.transform.position = new Vector3(obj.transform.position.x, result.hitPoint.y, obj.transform.position.z);
                }
            }
        }

        // 在鼠标位置创建新的GameObject
        private static void CreateNewGameObject<T>() where T : Behaviour
        {
            bool succee = MousePosToWorldPos(out Vector3 worldPos, out _);
            if (succee)
            {
                // 获取选中对象的父对象和同级索引
                Transform parent = (Selection.gameObjects!=null && Selection.gameObjects.Length>0) ? Selection.gameObjects[0].transform.parent : null;
                int siblingIndex = (Selection.gameObjects!=null && Selection.gameObjects.Length>0) ? Selection.gameObjects[0].transform.GetSiblingIndex() : 9999;

                GameObject newGameObject = new GameObject($"{typeof(T)}");
                Undo.RegisterCreatedObjectUndo(newGameObject, $"Create {typeof(T)}");
                newGameObject.AddComponent<T>();
                newGameObject.transform.position = worldPos;
                if (parent != null)
                    newGameObject.transform.SetParent(parent);
                newGameObject.transform.SetSiblingIndex(siblingIndex + 1);

                // 选中新创建的对象
                Selection.activeGameObject = newGameObject;
            }

        }

        #endregion


        #region Helper Functions

        //获取一个由[场景视图相机]到[鼠标位置]的射线
        // 使用 HandleUtility.GUIPointToWorldRay(mouseDownPosition); API代替
        // private static Ray SceneViewMouseRay()
        // {
        //     Vector2 mousePos = mouseDownPosition;
        //     Rect sceneViewRect = SceneView.lastActiveSceneView.position;
        //     // 0,0在左下角，1,1在右上角
        //     Vector2 viewPos = new Vector2(mousePos.x / sceneViewRect.width, 1 - mousePos.y / sceneViewRect.height);
        //     Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(viewPos);
        //     return ray;
        // }

        // 获取场景视图鼠标击中的Mesh的世界坐标
        private static bool MousePosToWorldPos(out Vector3 worldPos, out float distanceOut)
        {
            worldPos = Vector3.zero;
            distanceOut = 0;
            Ray ray = HandleUtility.GUIPointToWorldRay(mouseDownPosition);
            bool succeed = RaycastWithoutCollider.Raycast(ray.origin, ray.direction, out RaycastWithoutCollider.HitResult result);
            if (!succeed)
                return false;
            worldPos = result.hitPoint;
            distanceOut = result.distance;
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
            //MeshFilter meshFilter = obj.GetComponentInChildren<MeshFilter>();

            //选择轴心处于最低位置的collier
            Collider collider = FindLowestComponent<Collider>(obj);
            bool colliderIsAdd = false; //标记碰撞器是否是后来加上去的
            if (collider == null)
            {
                //如果没有碰撞器,添加碰撞器
                MeshRenderer meshRenderer = FindLowestComponent<MeshRenderer>(obj);
                if (meshRenderer != null)
                {
                    collider = meshRenderer.gameObject.AddComponent<BoxCollider>();
                }
                else //如果连网格都没有
                {
                    collider = obj.AddComponent<BoxCollider>();
                    ((BoxCollider)collider).size = Vector3.zero;
                }

                colliderIsAdd = true;
            }

            // collider.bounds位置会根据物体位置移动，但是不会旋转，三轴始终平行于世界坐标轴
            Bounds colliderBounds = collider.bounds;

            //原心点距离边框最下沿的距离
            float pivotToMinY = obj.transform.position.y - colliderBounds.min.y;

            Vector3 rayOrigin = collider.transform.position;
            if (detectFromTop)
                rayOrigin.y = 999999;

            obj.SetActive(false); //暂时隐藏物体,防止射线打到自己

            if(RaycastWithoutCollider.Raycast(rayOrigin, Vector3.down, out RaycastWithoutCollider.HitResult result))
            {

                obj.transform.position = new Vector3(obj.transform.position.x, result.hitPoint.y + pivotToMinY,
                    obj.transform.position.z);
            }
            else
            {
                Debug.LogWarning($"对象{obj.name}下方没检测到地面");
            }

            obj.SetActive(true);

            if (colliderIsAdd)
                GameObject.DestroyImmediate(collider);
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

        #endregion

    }
}