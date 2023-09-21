using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PluginHub.Module;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class AlignModule : PluginHubModuleBase
    {
        private Vector3 placeOffset = Vector3.zero; //摆放的偏移
        private Vector3 rayOffset = Vector3.zero; //射线原点偏移

        private float moveAmount = 0; //集体移动量

        //移动量数组
        private string[] moveAmountArray = new string[]
            { "0.01", "0.02", "0.05", "0.1", "0.1", "0.2", "0.5", "1", "1", "2", "5", "10" };

        private string[] selectGridName = new string[] { "+X", "+Y", "+Z", "-X", "-Y", "-Z" };
        private int selectAxis = 0; //选择的对齐方向

        protected override void DrawGuiContent()
        {
            GUILayout.BeginVertical("Box");
            {
                placeOffset = EditorGUILayout.Vector3Field("摆放偏移：", placeOffset);
                rayOffset = EditorGUILayout.Vector3Field("射线原点偏移：", rayOffset);

                GUILayout.Label("选择方向：");
                //选择对齐方向
                selectAxis = GUILayout.SelectionGrid(selectAxis, selectGridName, 3);

                if (GUILayout.Button("对齐所选对象"))
                {
                    SelectionObjTo();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("所选对象集体移动:");
                Vector2 buttonSize = new Vector2(30, 30);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical("Box", GUILayout.Width(102), GUILayout.Height(90));
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("↑", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                            {
                                MoveSelectionByWorldAxis(Vector3.up, moveAmount);
                            }

                            if (GUILayout.Button("↗", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                            {
                                MoveSelectionByWorldAxis(Vector3.forward, moveAmount);
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("←", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                            {
                                MoveSelectionByWorldAxis(Vector3.left, moveAmount);
                            }

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("→", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                            {
                                MoveSelectionByWorldAxis(Vector3.right, moveAmount);
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("↙", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                            {
                                MoveSelectionByWorldAxis(Vector3.back, moveAmount);
                            }

                            if (GUILayout.Button("↓", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                            {
                                MoveSelectionByWorldAxis(Vector3.down, moveAmount);
                            }

                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("移动量:");
                        moveAmount = EditorGUILayout.FloatField("", moveAmount, GUILayout.MinWidth(50));
                        int selectIndex = GUILayout.SelectionGrid(-1, moveAmountArray, 4);
                        if (selectIndex != -1)
                        {
                            moveAmount = float.Parse(moveAmountArray[selectIndex]);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();


        }

        public void SelectionObjTo()
        {
            Vector3 dir = Vector3.zero;
            switch (selectAxis)
            {
                case 0:
                    dir = Vector3.right;
                    break;
                case 1:
                    dir = Vector3.up;
                    break;
                case 2:
                    dir = Vector3.forward;
                    break;
                case 3:
                    dir = Vector3.left;
                    break;
                case 4:
                    dir = Vector3.down;
                    break;
                case 5:
                    dir = Vector3.back;
                    break;
            }

            GameObject[] gameObjects = Selection.gameObjects;
            Undo.RecordObjects(gameObjects.Select((o) => o.transform).ToArray(), "SelectionObjToGroundObj");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                MoveGameObjectTo(gameObjects[i], dir);
            }
        }

        private void MoveSelectionByWorldAxis(Vector3 sceneCamDir, float moveAmount)
        {
            GameObject[] gameObjects = Selection.gameObjects;
            if (gameObjects != null && gameObjects.Length > 0)
            {
                for (int i = 0; i < gameObjects.Length; i++)
                {
                    MoveGameObjectByWorldAxis(gameObjects[i].transform, sceneCamDir, moveAmount);
                }
            }
        }

        //移动游戏对象，根据场景相机决定移动方向，且将该方向吸附到世界坐标轴方向上
        //例如，若要将对象向场景相机前方移动，会将场景相机前方向量吸附到世界坐标系最接近的方向上，以该方向进行移动
        private void MoveGameObjectByWorldAxis(Transform gameobject, Vector3 sceneCamDir, float moveAmount)
        {
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            //将场景相机的方向吸附到世界坐标系的方向上面
            Vector3 moveDir = sceneCamera.transform.position +
                sceneCamera.transform.TransformDirection(sceneCamDir.normalized) - sceneCamera.transform.position;

            moveDir = FindNearstWorldDir(moveDir);
            gameobject.position += moveDir.normalized * moveAmount;
        }

        //给出一个世界坐标系下的方向向量，在世界坐标系6个方向中(世界坐标系向上、下、左、右、前、后)返回一个最接近该方向的方向。
        private Vector3 FindNearstWorldDir(Vector3 worldDirInput)
        {
            //点乘越大，越类似
            Vector3[] worldCoordinates = new[]
                { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
            int maxIndex = 0;
            float maxDot = -1;
            for (int i = 0; i < worldCoordinates.Length; i++)
            {
                float dotResult = Vector3.Dot(worldCoordinates[i], worldDirInput);
                if (maxDot < dotResult)
                {
                    maxDot = dotResult;
                    maxIndex = i;
                }
            }

            return worldCoordinates[maxIndex];
        }

        private void MoveGameObjectTo(GameObject obj, Vector3 rayDir)
        {
            RaycastHit hitInfo;
            Ray ray = new Ray(obj.transform.position + rayOffset, rayDir);

            if (Physics.Raycast(ray, out hitInfo))
            {
                obj.transform.position = hitInfo.point + placeOffset;
                Debug.Log($"天花板名称：{hitInfo.collider.name}");
            }
            else
            {
                Debug.LogWarning($"对象{obj.name}对齐方向碰撞体");
            }
        }
    }
}