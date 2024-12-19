using System.Collections.Generic;
using PluginHub.Editor;
using PluginHub.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    public class LightingModule : PluginHubModuleBase
    {
        public override ModuleType moduleType => ModuleType.Construction;
        public override string moduleDescription => "为场景搭建提供灯光的摆放与制作辅助";

        //是否显示所有灯光的位置
        public bool isShowAllLightPosition
        {
            get => EditorPrefs.GetBool($"{moduleIdentifyPrefix}_isShowAllLightPosition", false);
            set => EditorPrefs.SetBool($"{moduleIdentifyPrefix}_isShowAllLightPosition", value);
        }

        //是否自动设置灯光名字
        public bool autoSetLightName
        {
            get => EditorPrefs.GetBool($"{moduleIdentifyPrefix}_autoSetLightName", false);
            set => EditorPrefs.SetBool($"{moduleIdentifyPrefix}_autoSetLightName", value);
        }

        //
        private float lightPlaceOffsetY
        {
            get => EditorPrefs.GetFloat($"{moduleIdentifyPrefix}_lightPlaceOffsetY", -0.05f);
            set => EditorPrefs.SetFloat($"{moduleIdentifyPrefix}_lightPlaceOffsetY", value);
        }


        //场景中所有的灯光对象
        private List<Light> allLightObjects = new List<Light>();

        // 阵列放置的两个点
        private Vector3 placementFirstPosition
        {
            get
            {
                string[] pos = EditorPrefs.GetString($"{moduleIdentifyPrefix}_placementFirstPosition", "0,0,0")
                    .Split(',');
                return new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
            }
            set
            {
                EditorPrefs.SetString($"{moduleIdentifyPrefix}_placementFirstPosition",
                    $"{value.x:F3},{value.y:F3},{value.z:F3}");
            }
        }

        private Vector3 placementSecondPosition
        {
            get
            {
                string[] pos = EditorPrefs.GetString($"{moduleIdentifyPrefix}_placementSecondPosition", "0,0,0")
                    .Split(',');
                return new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
            }
            set
            {
                EditorPrefs.SetString($"{moduleIdentifyPrefix}_placementSecondPosition",
                    $"{value.x:F3},{value.y:F3},{value.z:F3}");
            }
        }

        private int placementCount
        {
            get => EditorPrefs.GetInt($"{moduleIdentifyPrefix}_placementCount", 1);
            set => EditorPrefs.SetInt($"{moduleIdentifyPrefix}_placementCount", value);
        }

        // 放置的灯光的参数
        private LightType placementLightType = LightType.Point;
        private float placementIntensity = 1;
        private float placementRange = 10;

        // 是否在场景中显示放置的gizmo （handle和放置位置预览）
        private bool showPlacementGizmoInScene
        {
            get => EditorPrefs.GetBool($"{moduleIdentifyPrefix}_showPlacementGizmoInScene", true);
            set => EditorPrefs.SetBool($"{moduleIdentifyPrefix}_showPlacementGizmoInScene", value);
        }


        protected override void DrawGuiContent()
        {
            isShowAllLightPosition = EditorGUILayout.Toggle("显示所有灯光位置", isShowAllLightPosition);
            autoSetLightName = EditorGUILayout.Toggle("自动设置灯光名字", autoSetLightName);

            lightPlaceOffsetY = EditorGUILayout.Slider("灯光放置竖直偏移Y", lightPlaceOffsetY, -0.1f, 0.1f);
            if (GUILayout.Button("将选中的灯光对象向上移动到最近的'天花板'"))
            {
                MoveSelectionToCeiling();
            }

            DrawSplitLine("阵列放置");

            showPlacementGizmoInScene = EditorGUILayout.Toggle("显示 Gizmo", showPlacementGizmoInScene);

            GUILayout.BeginHorizontal();
            {
                placementFirstPosition = EditorGUILayout.Vector3Field("第一个点", placementFirstPosition);
                GUILayout.BeginVertical(GUILayout.Width(100));
                {
                    if (GUILayout.Button("到场景游标"))
                        placementFirstPosition = PHSceneContextMenu.sceneViewCursor;
                    if (GUILayout.Button("向上吸附"))
                    {
                        Vector3? pos = CalculateToCeilingPosition(placementFirstPosition);
                        if (pos != null)
                            placementFirstPosition = pos.Value;
                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(100));
                {
                    if (GUILayout.Button("寻找"))
                        SceneCameraTween.RotateTo(placementFirstPosition);
                    if (GUILayout.Button("到这去"))
                        SceneCameraTween.GoTo(placementFirstPosition);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            {
                placementSecondPosition = EditorGUILayout.Vector3Field("第二个点", placementSecondPosition);
                GUILayout.BeginVertical(GUILayout.Width(100));
                {
                    if (GUILayout.Button("到场景游标"))
                        placementSecondPosition = PHSceneContextMenu.sceneViewCursor;
                    if (GUILayout.Button("向上吸附"))
                    {
                        Vector3? pos = CalculateToCeilingPosition(placementSecondPosition);
                        if (pos != null)
                            placementSecondPosition = pos.Value;
                    }
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(100));
                {
                    if (GUILayout.Button("寻找"))
                        SceneCameraTween.RotateTo(placementSecondPosition);
                    if (GUILayout.Button("到这去"))
                        SceneCameraTween.GoTo(placementSecondPosition);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();


            GUILayout.Label("放置的灯光：");
            placementLightType = (LightType)EditorGUILayout.EnumPopup("灯光类型", placementLightType);
            placementIntensity = EditorGUILayout.FloatField("强度", placementIntensity);
            placementRange = EditorGUILayout.FloatField("范围", placementRange);

            placementCount = EditorGUILayout.IntField("放置数量", placementCount);
            placementCount = Mathf.Max(placementCount, 1);


            GUI.enabled = Selection.gameObjects.Length > 0;
            if (GUILayout.Button("执行放置"))
            {
                Vector3[] positions = CalculatePlacementPositions();
                for (int i = 0; i < placementCount; i++)
                {
                    Light light = new GameObject("Light").AddComponent<Light>();
                    Undo.RegisterCreatedObjectUndo(light.gameObject, "放置物体");
                    light.type = placementLightType;
                    light.intensity = placementIntensity;
                    light.range = placementRange;
                    light.transform.parent = Selection.gameObjects[0].transform;
                    light.transform.position = positions[i];
                    light.transform.eulerAngles = new Vector3(90, 0, 0);
                }
            }

            GUI.enabled = true;


            // DrawSplitLine("设置静态");

            // GameObject[] gameObjects = Selection.gameObjects;
            // if (gameObjects.Length > 0)
            // {
            //     MeshRenderer meshRenderer = gameObjects[0].GetComponent<MeshRenderer>();
            //     if (meshRenderer != null)
            //     {
            //         // meshrenderer 的 bounds 会被 scale 影响
            //         GUILayout.Label($"{meshRenderer.bounds.size}");
            //     }
            // }
            //
            // if (GUILayout.Button("设置选中的物体为静态"))
            // {
            //     foreach (var light in Selection.gameObjects)
            //     {
            //         GameObjectUtility.SetStaticEditorFlags(light,
            //             StaticEditorFlags.ContributeGI |
            //             StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
            //             StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
            //             StaticEditorFlags.ReflectionProbeStatic |
            //             StaticEditorFlags.NavigationStatic | StaticEditorFlags.OffMeshLinkGeneration
            //             );
            //     }
            // }
        }

        private Vector3[] CalculatePlacementPositions()
        {
            Vector3[] positions = new Vector3[placementCount];
            Vector3 direction = (placementSecondPosition - placementFirstPosition).normalized;
            float distance = Vector3.Distance(placementFirstPosition, placementSecondPosition);
            for (int i = 0; i < placementCount; i++)
                positions[i] = placementFirstPosition + direction * (distance / (placementCount - 1) * i);

            return positions;
        }

        private bool DrawHandlePlacement()
        {
            if (!showPlacementGizmoInScene)
                return false;

            placementFirstPosition = Handles.PositionHandle(placementFirstPosition, Quaternion.identity);
            placementSecondPosition = Handles.PositionHandle(placementSecondPosition, Quaternion.identity);

            Handles.BeginGUI();
            {
                DrawSceneViewText(placementFirstPosition, "第一个点", new Vector2(0, 30));
                DrawSceneViewText(placementSecondPosition, "第二个点", new Vector2(0, 30));
            }
            Handles.EndGUI();

            // 预览放置位置
            Vector3[] positions = CalculatePlacementPositions();
            foreach (var position in positions)
            {
                float size = HandleUtility.GetHandleSize(position) * 0.1f;
                Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
            }

            return true;
        }

        //将位置向上移动到最近的天花板
        private Vector3? CalculateToCeilingPosition(Vector3 worldPos)
        {
            bool recastResult = RaycastWithoutCollider.Raycast(worldPos + new Vector3(0, -0.05f, 0), Vector3.up,
                out RaycastWithoutCollider.HitResult result);
            if (recastResult)
                return result.hitPoint + Vector3.up * lightPlaceOffsetY;
            else
                return null;
        }

        private void MoveSelectionToCeiling()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogError("没有选中的灯光");
                return;
            }

            //将选中的灯光移动到最近的天花板
            foreach (var light in Selection.gameObjects)
            {
                Vector3? pos = CalculateToCeilingPosition(light.transform.position);
                if (pos != null)
                    light.transform.position = pos.Value;
            }
        }

        private void DistributeSelectionOnXZPlane()
        {
            foreach (var light in Selection.gameObjects)
            {
                bool recastResultXPositive = RaycastWithoutCollider.Raycast(light.transform.position, Vector3.right,
                    out RaycastWithoutCollider.HitResult resultXPositive);
                bool recastResultXNegative = RaycastWithoutCollider.Raycast(light.transform.position, Vector3.left,
                    out RaycastWithoutCollider.HitResult resultXNegative);
                bool recastResultZPositive = RaycastWithoutCollider.Raycast(light.transform.position, Vector3.forward,
                    out RaycastWithoutCollider.HitResult resultZPositive);
                bool recastResultZNegative = RaycastWithoutCollider.Raycast(light.transform.position, Vector3.back,
                    out RaycastWithoutCollider.HitResult resultZNegative);

                int resultCount = 0;
                if (recastResultXPositive)
                    resultCount++;
                if (recastResultXNegative)
                    resultCount++;
                if (recastResultZPositive)
                    resultCount++;
                if (recastResultZNegative)
                    resultCount++;

                light.transform.position =
                    (resultXPositive.hitPoint + resultXNegative.hitPoint + resultZPositive.hitPoint +
                     resultZNegative.hitPoint) / resultCount;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            // }
            // public override void RefreshData()
            // {
            //     base.RefreshData();

            //获取所有灯光对象
            allLightObjects.Clear();
            allLightObjects.AddRange(Object.FindObjectsOfType<Light>());

            //自动设置灯光名字
            if (autoSetLightName)
            {
                for (int i = 0; i < allLightObjects.Count; i++)
                {
                    Light light = allLightObjects[i];
                    if (light == null)
                        continue;
                    string newName =
                        $"{GetLightTypeName(light.type)} {light.lightmapBakeType} {light.intensity} {light.bounceIntensity}";
                    allLightObjects[i].name = newName;
                }
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        private string GetLightTypeName(LightType lightType)
        {
            switch (lightType)
            {
#if UNITY_6000_0_OR_NEWER
                case LightType.Rectangle:
                    return "Rectangle";
#else
                case LightType.Area:
                    return "Area";
#endif
                case LightType.Directional:
                    return "Dir";
                case LightType.Disc:
                    return "Disc";
                case LightType.Point:
                    return "Point";
                case LightType.Spot:
                    return "Spot";
                default:
                    break;
            }

            return "";
        }

        private bool DrawLightingPosition()
        {
            if (!isShowAllLightPosition)
                return false;

            //绘制所有灯光的位置
            foreach (var light in allLightObjects)
            {
                if (light == null)
                    continue;
                //面光
                if (light.type == LightType.Rectangle)
                {
                    HandlesEx.DrawRect(light.transform.position, light.areaSize.x, light.areaSize.y,
                        light.transform.rotation.eulerAngles);
                }
                //点光源
                else if (light.type == LightType.Point)
                {
                    HandlesEx.DrawSphere(light.transform.position, light.range);
                }
                //聚光灯
                else if (light.type == LightType.Spot)
                {
                    Handles.DrawLine(light.transform.position,
                        light.transform.position + light.transform.forward * light.range);
                }
            }

            return isShowAllLightPosition;
        }


        protected override bool OnSceneGUI(SceneView sceneView)
        {
            return DrawLightingPosition() || DrawHandlePlacement();
        }
    }
}