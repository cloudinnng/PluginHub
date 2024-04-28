using System.Collections.Generic;
using PluginHub.Extends;
using PluginHub.Helper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.SceneManagement;

namespace PluginHub.Module
{
    public class LightingModule : PluginHubModuleBase
    {
        public override ModuleType moduleType => ModuleType.Construction;
        public override string moduleDescription => "辅助灯光的摆放与制作";

        //是否显示所有灯光的位置
        public bool isShowAllLightPosition {
            get => EditorPrefs.GetBool($"{moduleIdentifyPrefix}_isShowAllLightPosition", false);
            set => EditorPrefs.SetBool($"{moduleIdentifyPrefix}_isShowAllLightPosition", value);
        }

        //是否自动设置灯光名字
        public bool autoSetLightName {
            get => EditorPrefs.GetBool($"{moduleIdentifyPrefix}_autoSetLightName", false);
            set => EditorPrefs.SetBool($"{moduleIdentifyPrefix}_autoSetLightName", value);
        }

        //
        private float lightPlaceOffsetY {
            get => EditorPrefs.GetFloat($"{moduleIdentifyPrefix}_lightPlaceOffsetY", -0.02f);
            set => EditorPrefs.SetFloat($"{moduleIdentifyPrefix}_lightPlaceOffsetY", value);
        }


        //场景中所有的灯光对象
        private List<Light> allLightObjects = new List<Light>();

        protected override void DrawGuiContent()
        {
            isShowAllLightPosition = EditorGUILayout.Toggle("显示所有灯光位置", isShowAllLightPosition);
            autoSetLightName = EditorGUILayout.Toggle("自动设置灯光名字", autoSetLightName);

            lightPlaceOffsetY = EditorGUILayout.Slider("灯光向上偏移Y", lightPlaceOffsetY, -0.1f, 0.1f);
            if (GUILayout.Button("将选中的灯光对象向上移动到最近的'天花板'"))
            {
                MoveSelectionToCeiling();
            }

            // if (GUILayout.Button("将选中的灯光在XZ平面上均匀分布(室内环境)"))
            // {
            //     DistributeSelectionOnXZPlane();
            // }
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
                bool recastResult = RaycastWithoutCollider.RaycastMeshRenderer(light.transform.position, Vector3.up,out RaycastWithoutCollider.RaycastResult result);

                if (recastResult)
                {
                    light.transform.position = result.hitPoint + Vector3.up * lightPlaceOffsetY;
                }
            }
        }

        private void DistributeSelectionOnXZPlane()
        {
            foreach (var light in Selection.gameObjects)
            {
                bool recastResultXPositive = RaycastWithoutCollider.RaycastMeshRenderer(light.transform.position, Vector3.right,
                    out RaycastWithoutCollider.RaycastResult resultXPositive);
                bool recastResultXNegative = RaycastWithoutCollider.RaycastMeshRenderer(light.transform.position, Vector3.left,
                    out RaycastWithoutCollider.RaycastResult resultXNegative);
                bool recastResultZPositive = RaycastWithoutCollider.RaycastMeshRenderer(light.transform.position, Vector3.forward,
                    out RaycastWithoutCollider.RaycastResult resultZPositive);
                bool recastResultZNegative = RaycastWithoutCollider.RaycastMeshRenderer(light.transform.position, Vector3.back,
                    out RaycastWithoutCollider.RaycastResult resultZNegative);

                int resultCount = 0;
                if (recastResultXPositive)
                    resultCount++;
                if (recastResultXNegative)
                    resultCount++;
                if (recastResultZPositive)
                    resultCount++;
                if (recastResultZNegative)
                    resultCount++;

                light.transform.position = (resultXPositive.hitPoint + resultXNegative.hitPoint + resultZPositive.hitPoint + resultZNegative.hitPoint) / resultCount;
            }
        }

        public override void RefreshData()
        {
            base.RefreshData();

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
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private string GetLightTypeName(LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Area:
                    return "Area";
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

        protected override bool OnSceneGUI(SceneView sceneView)
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
                    HandlesEx.DrawRect(light.transform.position,light.areaSize.x,light.areaSize.y,light.transform.rotation.eulerAngles);
                }
                //点光源
                else if (light.type == LightType.Point)
                {
                    HandlesEx.DrawSphere(light.transform.position,light.range);
                }
                //聚光灯
                else if (light.type == LightType.Spot)
                {
                    Handles.DrawLine(light.transform.position,light.transform.position + light.transform.forward * light.range);
                }
            }
            return isShowAllLightPosition;
        }
    }
}