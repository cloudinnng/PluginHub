using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PluginHub.Module;
using PluginHub.Module.ModuleScripts;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class AlignModule : PluginHubModuleBase
    {
        public override ModuleType moduleType => ModuleType.Construction;

        private Vector3 placeOffset = Vector3.zero; //摆放的偏移
        private Vector3 rayOffset = Vector3.zero; //射线原点偏移

        private float moveAmount = 0; //集体移动量

        //移动量数组
        private string[] moveAmountArray = new string[]
            { "0.01", "0.02", "0.05", "0.1", "0.1", "0.2", "0.5", "1", "1", "2", "5", "10" };

        private int selectAxis = -1;//选择的对齐方向


        protected override void DrawGuiContent()
        {
            GUILayout.BeginVertical("Box");
            {
                placeOffset = EditorGUILayout.Vector3Field("摆放偏移：", placeOffset);
                rayOffset = EditorGUILayout.Vector3Field("射线原点偏移：", rayOffset);

                GUILayout.Label("选择方向：");

                GUILayout.BeginHorizontal();
                {
                    DrawAxisLikeButtonGroup(
                        "Y+", "Y-", "X-", "X+", "Z+", "Z-",
                        () => selectAxis = 0,
                        () => selectAxis = 1,
                        () => selectAxis = 2,
                        () => selectAxis = 3,
                        () => selectAxis = 4,
                        () => selectAxis = 5,
                        ref selectAxis,
                        false
                    );

                    GUILayout.BeginVertical();
                    {
                        EditorGUILayout.HelpBox("提示：模块会在场景视图绘制您选择的对齐方向。在对齐之前查看此方向是否为您所希望的方向。",MessageType.Info);
                        GUI.enabled = selectAxis != -1 && Selection.gameObjects != null && Selection.gameObjects.Length > 0;
                        if (GUILayout.Button("对齐所选对象",GUILayout.Height(30)))
                        {
                            SelectionObjTo();
                        }
                        GUI.enabled = true;
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("所选对象集体移动:");

                GUILayout.BeginHorizontal();
                {
                    int btnState = -1;
                    GUI.enabled = Selection.gameObjects != null && Selection.gameObjects.Length > 0 && moveAmount != 0;
                    DrawAxisLikeButtonGroup(
                        "↑", "↓", "←", "→", "↗", "↙",
                        () => MoveSelectionByWorldAxis(Vector3.up, moveAmount),
                        () => MoveSelectionByWorldAxis(Vector3.down, moveAmount),
                        () => MoveSelectionByWorldAxis(Vector3.left, moveAmount),
                        () => MoveSelectionByWorldAxis(Vector3.right, moveAmount),
                        () => MoveSelectionByWorldAxis(Vector3.forward, moveAmount),
                        () => MoveSelectionByWorldAxis(Vector3.back, moveAmount),
                        ref btnState,
                        false
                    );
                    GUI.enabled = true;
                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
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

        private Vector3 GetDir()
        {
            switch (selectAxis)
            {
                case 0: return Vector3.up;
                case 1: return Vector3.down;
                case 2: return Vector3.left;
                case 3: return Vector3.right;
                case 4: return Vector3.forward;
                case 5: return Vector3.back;
                default:
                    Debug.LogError("未选择对齐方向");
                    return Vector3.zero;
            }
        }

        public void SelectionObjTo()
        {
            Vector3 dir = GetDir();

            GameObject[] gameObjects = Selection.gameObjects;
            Undo.RecordObjects(gameObjects.Select((o) => o.transform).ToArray(), "SelectionObjToGroundObj");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                MoveGameObjectTo(gameObjects[i], dir);
            }
        }

        protected override bool OnSceneGUI(SceneView sceneView)
        {
            if (selectAxis != -1 && Selection.gameObjects != null && Selection.gameObjects.Length > 0
                &&Selection.gameObjects[0]!=null)
            {
                Vector3 position = Selection.gameObjects[0].transform.position;

                //draw a arrow to show the direction
                Color oldColor = Handles.color;
                Handles.color = Color.white;

                float size = HandleUtility.GetHandleSize(position) + 1;
                Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(GetDir()), size, EventType.Repaint);
                // Handles.DrawLine(position, position + GetDir() * size);


                Handles.color = oldColor;

                return true;
            }
            return false;
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
            Ray ray = new Ray(obj.transform.position + rayOffset, rayDir);
            bool rcresult = RaycastWithoutCollider.RaycastMeshRenderer(ray.origin, ray.direction, out RaycastWithoutCollider.RaycastResult result);
            if (rcresult)
            {
                obj.transform.position = result.hitPoint + placeOffset;
                Debug.Log($"天花板名称：{result.meshRenderer.name}");
            }
            else
            {
                Debug.LogWarning($"对象{obj.name}对齐方向无碰撞体");
            }
        }


        private readonly Vector2 axisLikeBtnSize = new Vector2(30, 30);
        //saveStatus:按钮是否保持按下状态的颜色，作为一个状态按钮。
        public void DrawAxisLikeButtonGroup(
            string upBtnName,string downBtnName,string leftBtnName,string rightBtnName, string forwardBtnName,string backBtnName,
            Action upBtn,Action downBtn,Action leftBtn,Action rightBtn,Action forwardBtn,Action backBtn,
            ref int axisBtnState, bool hasInitializedState = true)
        {
            if(hasInitializedState)
                axisBtnState = -1;
            GUILayout.BeginVertical("Box", GUILayout.Width(102), GUILayout.Height(90));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUI.color = axisBtnState == 0 ? PluginHubFunc.SelectedColor : Color.white;
                    if (GUILayout.Button(upBtnName, GUILayout.Width(axisLikeBtnSize.x), GUILayout.Height(axisLikeBtnSize.y)))
                    {
                        upBtn?.Invoke();
                        axisBtnState = 0;
                    }
                    GUI.color = Color.white;

                    GUI.color = axisBtnState == 4 ? PluginHubFunc.SelectedColor : Color.white;
                    if (GUILayout.Button(forwardBtnName,GUILayout.Width(axisLikeBtnSize.x), GUILayout.Height(axisLikeBtnSize.y)))
                    {
                        forwardBtn?.Invoke();
                        axisBtnState = 4;
                    }
                    GUI.color = Color.white;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUI.color = axisBtnState == 2 ? PluginHubFunc.SelectedColor : Color.white;
                    if (GUILayout.Button(leftBtnName, GUILayout.Width(axisLikeBtnSize.x), GUILayout.Height(axisLikeBtnSize.y)))
                    {
                        leftBtn?.Invoke();
                        axisBtnState = 2;
                    }
                    GUI.color = Color.white;

                    GUILayout.FlexibleSpace();

                    GUI.color = axisBtnState == 3 ? PluginHubFunc.SelectedColor : Color.white;
                    if (GUILayout.Button(rightBtnName, GUILayout.Width(axisLikeBtnSize.x), GUILayout.Height(axisLikeBtnSize.y)))
                    {
                        rightBtn?.Invoke();
                        axisBtnState = 3;
                    }
                    GUI.color = Color.white;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUI.color = axisBtnState == 5 ? PluginHubFunc.SelectedColor : Color.white;
                    if (GUILayout.Button(backBtnName, GUILayout.Width(axisLikeBtnSize.x), GUILayout.Height(axisLikeBtnSize.y)))
                    {
                        backBtn?.Invoke();
                        axisBtnState = 5;
                    }
                    GUI.color = Color.white;

                    GUI.color = axisBtnState == 1 ? PluginHubFunc.SelectedColor : Color.white;
                    if (GUILayout.Button(downBtnName, GUILayout.Width(axisLikeBtnSize.x), GUILayout.Height(axisLikeBtnSize.y)))
                    {
                        downBtn?.Invoke();
                        axisBtnState = 1;
                    }
                    GUI.color = Color.white;

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

    }
}