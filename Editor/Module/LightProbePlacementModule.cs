using System.Collections;
using System.Collections.Generic;
using PluginHub.Helper;
using PluginHub.ModuleScripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace PluginHub.Module
{
    public class LightProbePlacementModule : PluginHubModuleBase
    {
        public override string moduleName
        {
            get { return "光探头摆放"; }
        }

        private MeshRenderer _mrToPlaceLP;
        private LightProbeGroup _lightProbeGroup;
        private Vector3[] _lightProbePos; //待添加的光探头位置预览
        private float gizmosPreviewSize = 0.2f; //待添加的光探头预览用的球球大小
        private float distanceMultiplier = 1f;
        private bool zTestEnable = false;
        private Vector3 placeOffset = new Vector3(); //放置偏移
        private Vector3 axisScale = Vector3.one; //轴向缩放
        private float placeMinDistance = .5f; //放置的最近距离
        private bool autoUseSelectObject = false; //自动使用选择的对象当作目标
        private bool hideDefaultHandle = false; //隐藏默认的操作手柄

        protected override void DrawGuiContent()
        {
            //PluginHubFunc.TextBox("为小物体摆放8个光探头到指定组中");

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    autoUseSelectObject = GUILayout.Toggle(autoUseSelectObject, "自动使用选择的对象当作目标");
                    if (autoUseSelectObject && Selection.gameObjects != null && Selection.gameObjects.Length > 0)
                    {
                        MeshRenderer meshRenderer = Selection.gameObjects[0].GetComponent<MeshRenderer>();
                        if (meshRenderer != null && meshRenderer != _mrToPlaceLP)
                        {
                            //对象无ContributeGI静态标志
                            if ((GameObjectUtility.GetStaticEditorFlags(meshRenderer.gameObject) &
                                 StaticEditorFlags.ContributeGI) == 0)
                            {
                                _mrToPlaceLP = meshRenderer;
                            }
                        }
                    }

                    //Tools.hidden = true 这将隐藏当前选定游戏对象的默认移动、旋转和调整大小工具，它不会干扰脚本添加的自定义 Gizmo 或手柄。
                    hideDefaultHandle = GUILayout.Toggle(hideDefaultHandle, "Hide default transform handle");
                    Tools.hidden = hideDefaultHandle;
                }
                GUILayout.EndHorizontal();

                _mrToPlaceLP = PluginHubFunc.LableWithObjectFiled<MeshRenderer>("Target Object", _mrToPlaceLP);

                _lightProbeGroup =
                    PluginHubFunc.LableWithObjectFiled<LightProbeGroup>("Light Probe Group", _lightProbeGroup);

                if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
                {
                    LightProbeGroup lightProbeGroup = Selection.gameObjects[0].GetComponent<LightProbeGroup>();
                    if (lightProbeGroup != null && lightProbeGroup != _lightProbeGroup)
                    {
                        if (GUILayout.Button("使用选中对象"))
                        {
                            _lightProbeGroup = lightProbeGroup;
                        }
                    }
                }

                gizmosPreviewSize = PluginHubFunc.LabelWithSlider("Gizmos预览显示大小", gizmosPreviewSize, 0f, .5f);

                zTestEnable = PluginHubFunc.LabelWithToggle("Enable Gizmos zTest", zTestEnable);

                distanceMultiplier = PluginHubFunc.LabelWithSlider("距离乘数", distanceMultiplier, .5f, 3);

                placeOffset = PluginHubFunc.LabelWithVector3Field("放置偏移", placeOffset);

                if (GUILayout.Button("Reset"))
                {
                    placeOffset = Vector3.zero;
                }

                axisScale = PluginHubFunc.LabelWithVector3Field("轴向缩放", axisScale);

                if (GUILayout.Button("Reset"))
                {
                    axisScale = Vector3.one;
                }

                placeMinDistance = PluginHubFunc.LabelWithSlider("放置光探头的最近距离", placeMinDistance, .01f, 3);

                GUI.enabled = _mrToPlaceLP && _lightProbeGroup;
                if (GUILayout.Button("在LightProbes中放置这8个点", GUILayout.Height(30)))
                {
                    Vector3[] old = _lightProbeGroup.probePositions;
                    //距离检测  太近了 不放
                    List<Vector3> posToPlace = new List<Vector3>();
                    for (int j = 0; j < _lightProbePos.Length; j++)
                    {
                        Vector3 pos = _lightProbePos[j];
                        float minDistance = 9999;
                        for (int i = 0; i < old.Length; i++)
                        {
                            float dis = Vector3.Distance(old[i], pos);
                            if (minDistance > dis)
                            {
                                minDistance = dis;
                            }
                        }

                        //找出最近的距离   最近距离满足最小距离要求 允许放置
                        if (minDistance >= placeMinDistance)
                            posToPlace.Add(pos);
                    }

                    List<Vector3> list = new List<Vector3>();
                    list.AddRange(old);
                    list.AddRange(posToPlace);
                    _lightProbeGroup.probePositions = list.ToArray();
                    Debug.Log($"已执行放置{posToPlace.Count}");
                }

                GUI.enabled = true;

                GUILayout.Space(30);

                if (GUILayout.Button("使用体积摆放方式,添加LightProbesVolumePlaceHelper组件"))
                {
                    if (Selection.gameObjects.Length > 0)
                    {
                        GameObject obj = Selection.gameObjects[0];
                        LightProbeGroup lightProbeGroup = obj.GetComponent<LightProbeGroup>();
                        if (lightProbeGroup != null)
                            obj.AddComponent<LightProbesVolumePlaceHelper>();
                        else
                            Debug.LogWarning($"{obj.name}没有LightProbeGroup组件");
                    }
                }
            }
            GUILayout.EndVertical();
        }

        //绘制光探头工具的场景视图GUI
        private void DrawLightProbeToolsSceneGUI()
        {
            //更新预览位置
            if (_mrToPlaceLP != null)
            {
                _lightProbePos = new Vector3[8];

                Vector3 center = _mrToPlaceLP.bounds.center + placeOffset;
                Vector3 extents = new Vector3(_mrToPlaceLP.bounds.extents.x * axisScale.x,
                    _mrToPlaceLP.bounds.extents.y * axisScale.y, _mrToPlaceLP.bounds.extents.z * axisScale.z);

                _lightProbePos[0] = center + new Vector3(+extents.x, +extents.y, +extents.z) * distanceMultiplier;
                _lightProbePos[1] = center + new Vector3(+extents.x, +extents.y, -extents.z) * distanceMultiplier;
                _lightProbePos[2] = center + new Vector3(+extents.x, -extents.y, +extents.z) * distanceMultiplier;
                _lightProbePos[3] = center + new Vector3(+extents.x, -extents.y, -extents.z) * distanceMultiplier;
                _lightProbePos[4] = center + new Vector3(-extents.x, +extents.y, +extents.z) * distanceMultiplier;
                _lightProbePos[5] = center + new Vector3(-extents.x, +extents.y, -extents.z) * distanceMultiplier;
                _lightProbePos[6] = center + new Vector3(-extents.x, -extents.y, +extents.z) * distanceMultiplier;
                _lightProbePos[7] = center + new Vector3(-extents.x, -extents.y, -extents.z) * distanceMultiplier;

                //使用场景视图中的位置手柄，移动放置偏移

                Vector3 handlePos = _mrToPlaceLP.transform.position + placeOffset;
                Quaternion handleRotation = Quaternion.identity;

                switch (Tools.current)
                {
                    case Tool.Move:
                        //绘制移动手柄
                        Vector3 newHandlePosition = Handles.PositionHandle(handlePos, handleRotation);
                        placeOffset = newHandlePosition - _mrToPlaceLP.transform.position;
                        break;
                    case Tool.Scale:
                        //绘制缩放手柄
                        float size = HandleUtility.GetHandleSize(handlePos);
                        axisScale = Handles.ScaleHandle(axisScale, handlePos, handleRotation, size);
                        break;
                }
            }
            else
            {
                _lightProbePos = null;
            }


            //绘制光照探头即将放置的位置预览
            if (_lightProbePos != null && _lightProbePos.Length > 0)
            {
                //save
                CompareFunction oldValue = Handles.zTest;
                Handles.zTest = zTestEnable ? CompareFunction.LessEqual : CompareFunction.Always;
                Color colorTmp = Handles.color;

                for (int i = 0; i < _lightProbePos.Length; i++)
                {
                    Vector3 pos = _lightProbePos[i];

                    Handles.color = ValidLightProbePosition(pos) ? Color.green : Color.red;

                    Handles.SphereHandleCap(i, pos, Quaternion.identity,
                        HandleUtility.GetHandleSize(pos) * gizmosPreviewSize, EventType.Repaint);
                }

                //restore
                Handles.color = colorTmp;
                Handles.zTest = oldValue;
            }
        }



        //画场景GUi
        public override bool OnSceneGUI(SceneView sceneView)
        {
            // if (currSelectTabIndex == 1)
            // {
            DrawLightProbeToolsSceneGUI();
            // }
            return true;
        }

        //当前坐标是否是一个合适的光照探头摆放位置，光探头不要摆放在模型内部，那样是无意义的
        //注意为场景物体添加网格碰撞器再用此方法检测
        public static bool ValidLightProbePosition(Vector3 worldPos, float detectDistance = .2f)
        {
            //附近一段距离内 不要检测到模型的反面,即为合适的位置
            Ray[] fromCenterRays = new[]
            {
                new Ray(worldPos, Vector3.up),
                new Ray(worldPos, Vector3.down),
                new Ray(worldPos, Vector3.left),
                new Ray(worldPos, Vector3.right),
                new Ray(worldPos, Vector3.forward),
                new Ray(worldPos, Vector3.back),
            };
            Ray[] fromExternalRays = new[]
            {
                new Ray(worldPos + Vector3.down * detectDistance, Vector3.up),
                new Ray(worldPos + Vector3.up * detectDistance, Vector3.down),
                new Ray(worldPos + Vector3.right * detectDistance, Vector3.left),
                new Ray(worldPos + Vector3.left * detectDistance, Vector3.right),
                new Ray(worldPos + Vector3.back * detectDistance, Vector3.forward),
                new Ray(worldPos + Vector3.forward * detectDistance, Vector3.back),
            };

            for (int i = 0; i < fromExternalRays.Length; i++)
            {
                Ray ray = fromExternalRays[i];
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, detectDistance))
                {
                    MeshRenderer meshRenderer = hit.transform.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                        return false;
                }
            }

            return true;
        }
    }
}