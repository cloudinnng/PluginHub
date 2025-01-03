using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PluginHub.Editor
{
    // 这是一个 SceneView Overlay条
    // 在场景视图中按～键（波浪线键）然后选择PH Scene Overlay 可以显示这个条
    [Overlay(typeof(SceneView), "PH Scene Overlay")]
    public class PHSceneOverlay : IMGUIOverlay
    {
        public override void OnGUI()
        {
            DrawSceneBookmark();

            DrawSelectionTools();
        }

        #region SceneBookmark

        private static CameraBookmarkUIRow _cameraBookmarkUIRow = new CameraBookmarkUIRow();
        private static GameObjectBookmarkUIRow _gameObjectBookmarkUIRow = new GameObjectBookmarkUIRow();
        private static AssetBookmarkUIRow _assetBookmarkUIRow = new AssetBookmarkUIRow();

        private void DrawSceneBookmark()
        {
            //当前场景路径
            string currScenePath = SceneManager.GetActiveScene().path;
            //所有场景书签
            List<SceneBookmarkGroup> bookmarkGroups = BookmarkAssetSO.Instance.bookmarkGroups;
            //找到当前场景的书签
            SceneBookmarkGroup sceneBookmarkGroup =  bookmarkGroups.Find(x => x.scenePath == currScenePath);
            if(sceneBookmarkGroup == null)
            {
                sceneBookmarkGroup = new SceneBookmarkGroup() { scenePath = currScenePath };
                bookmarkGroups.Add(sceneBookmarkGroup);
            }
            //相机书签--------------------------------------------------------------------------------
            _cameraBookmarkUIRow.DrawGUI(sceneBookmarkGroup);
            //游戏对象书签--------------------------------------------------------------------------------
            _gameObjectBookmarkUIRow.DrawGUI(sceneBookmarkGroup);
            //资产书签--------------------------------------------------------------------------------
            _assetBookmarkUIRow.DrawGUI(sceneBookmarkGroup);
        }



        #endregion

        #region SelectionTools
        private void DrawSelectionTools()
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

                        if (GUILayout.Button("!23"))
                        {
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
                Debug.Log($"对象{obj.name}已移动到{result.renderer.name}上", result.renderer);
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