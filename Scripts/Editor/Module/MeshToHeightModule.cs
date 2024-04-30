using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

namespace PluginHub.Editor
{
    public class MeshToHeightModule : PluginHubModuleBase
    {
        public override ModuleType moduleType => ModuleType.Tool;
        public override string moduleName { get; } = "网格转高度图";
        public override string moduleDescription => "";
        private BoxCollider _boxCollider;
        private bool useBoxColliderYRange = false;
        private Vector2 heightMapResolutionSize = Vector2.zero; //生成的高度图的分辨率
        private string heightMapPath; //高度图存储的路径
        private float worldStep = 1f;
        private float halfWorldStep;
        private float quarterWorldStep;
        private int layerMask = -1; //default -1 for all layers
        private Color backgroundColor = Color.gray;
        private float raycastOffsetY = 0;
        private Texture2D texPreview;

        protected override void DrawGuiContent()
        {
            //get init
            if (string.IsNullOrWhiteSpace(heightMapPath))
            {
                heightMapPath = Path.Combine(Application.dataPath, "HeightMap");
                heightMapPath = heightMapPath.Replace("\\", "/");
            }

            layerMask = EditorGUILayout.MaskField($"采样层", layerMask, GetAllLayerNames());
            backgroundColor = EditorGUILayout.ColorField("背景色", backgroundColor);
            _boxCollider = EditorGUILayout.ObjectField("盒子碰撞器", _boxCollider, typeof(BoxCollider), true) as BoxCollider;
            useBoxColliderYRange =
                EditorGUILayout.Toggle(
                    PluginHubFunc.GuiContent("使用盒子碰撞器的Y范围",
                        "若选中，则盒体碰撞器的底部将作为高度图中的黑色，顶部将作为高度图中的白色。若不勾选，则最黑最白范围将由地形最低点和最高点决定。"), useBoxColliderYRange);
            raycastOffsetY = EditorGUILayout.FloatField("射线起始点Y偏移", raycastOffsetY);
            heightMapPath = EditorGUILayout.TextField("高度图保存路径", heightMapPath);
            worldStep = EditorGUILayout.Slider("世界步长", worldStep, 0.001f, 5f);
            halfWorldStep = worldStep * 0.5f;
            quarterWorldStep = halfWorldStep * 0.5f;

            if (_boxCollider != null)
            {
                _boxCollider.name = $"MTH_Box{_boxCollider.size}";

                //强制_boxCollider不旋转 不缩放
                _boxCollider.transform.localScale = Vector3.one;
                _boxCollider.transform.rotation = Quaternion.identity;
                _boxCollider.isTrigger = true;
                //计算生成的高度图分辨率
                heightMapResolutionSize.Set(_boxCollider.size.x / worldStep, _boxCollider.size.z / worldStep);
            }

            GUILayout.Label($"生成分辨率： {(int)heightMapResolutionSize.x} x {(int)heightMapResolutionSize.y}");

            if (GUILayout.Button("为场景对象添加MeshCollider"))
            {
                int counter = 0;

                MeshRenderer[] meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    MeshRenderer meshRenderer = meshRenderers[i];
                    MeshCollider meshCollider = meshRenderer.gameObject.GetComponent<MeshCollider>();
                    if (meshRenderer.enabled && meshCollider == null)
                    {
                        meshRenderer.gameObject.AddComponent<MeshCollider>();
                        counter++;
                    }
                }

                Debug.Log($"添加了{counter}个MeshCollider");
            }

            EditorGUILayout.HelpBox("记得给网格添加MeshCollider，且设置正确的LayerMask", MessageType.Info);


            if (GUILayout.Button("生成高度图"))
            {
                if (_boxCollider == null)
                {
                    Debug.LogError("请先设置盒子碰撞器");
                    return;
                }

                if (heightMapResolutionSize.x > 2048 || heightMapResolutionSize.y > 2048)
                {
                    //show yes no dialog
                    if (!EditorUtility.DisplayDialog("Info", "高度图分辨率超过2048，可能会导致生成时间过长，是否继续？", "是", "否"))
                    {
                        return;
                    }
                }

                if (!Directory.Exists(heightMapPath))
                {
                    Directory.CreateDirectory(heightMapPath);
                }

                Color[] colors = new Color[(int)heightMapResolutionSize.x * (int)heightMapResolutionSize.y];
                // Debug.Log(colors.Length);

                //射线检测赋值colors
                Vector3[] raycastPoint = GetRaycastPoint();
                // Debug.Log(raycastPoint.Length);

                float minH = 9999, maxH = -9999, highDiff = 0;
                //记录高度图的最大最小值
                for (int i = 0; i < raycastPoint.Length; i++)
                {
                    Vector3 pos = raycastPoint[i];
                    bool result = GetRaycastHeight(pos, out float height);
                    if (result) //有碰撞
                    {
                        if (height < minH)
                        {
                            minH = height;
                        }

                        if (height > maxH)
                        {
                            maxH = height;
                        }

                        colors[i] = new Color(height, 1, 0); //高度暂时存在r值中 ,有没有碰撞暂时存在g值中
                    }
                    else
                    {
                        colors[i] = new Color(0, 0, 0);
                    }
                }

                if (useBoxColliderYRange)
                {
                    Vector2 pos = GetBoxColliderMinMaxY(_boxCollider);
                    minH = pos.x;
                    maxH = pos.y;
                }

                highDiff = maxH - minH;
                Debug.Log($"最低高度：{minH}，最高高度：{maxH}，高度差：{highDiff}");

                for (int i = 0; i < colors.Length; i++)
                {
                    //使用高度差计算颜色
                    float grayValue = Mathf.Lerp(0, 1, (colors[i].r - minH) / highDiff);
                    if (colors[i].g == 0) //没有碰撞的像素
                        colors[i] = backgroundColor;
                    else
                        colors[i] = new Color(grayValue, grayValue, grayValue);
                    // Debug.Log(grayValue);
                }

                //开始生成高度图
                Texture2D heightMap = new Texture2D((int)heightMapResolutionSize.x, (int)heightMapResolutionSize.y);
                heightMap.SetPixels(colors);
                heightMap.Apply(false);
                string path = Path.Combine(heightMapPath, $"HeightMap.png");
                File.WriteAllBytes(path, heightMap.EncodeToPNG());
                GameObject.DestroyImmediate(heightMap);
                AssetDatabase.Refresh();

                //选择这个纹理
                path = path.Substring(path.IndexOf("Assets"));
                texPreview = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            if (texPreview != null)
            {
                EditorGUILayout.ObjectField(texPreview, typeof(Texture2D), false);
            }
        }

        //获取项目设置中的所有层
        public string[] GetAllLayerNames()
        {
            List<string> layerNames = new List<string>();
            for (int i = 0; i <= 31; i++) //user defined layers start with layer 8 and unity supports 31 layers
            {
                string layerN = LayerMask.LayerToName(i); //get the name of the layer
                if (!string.IsNullOrWhiteSpace(
                        layerN)) //only add the layer if it has been named (comment this line out if you want every layer)
                    layerNames.Add(layerN);
            }

            return layerNames.ToArray();
        }

        public Vector2 GetBoxColliderMinMaxY(BoxCollider boxCollider)
        {
            Vector2 minMaxY = new Vector2(
                boxCollider.transform.position.y + boxCollider.center.y - boxCollider.size.y * 0.5f,
                boxCollider.transform.position.y + boxCollider.center.y + boxCollider.size.y * 0.5f);
            return minMaxY;
        }


        protected override bool OnSceneGUI(SceneView sceneView)
        {
            if (_boxCollider != null)
            {
                //绘制盒子碰撞器
                Handles.DrawWireCube(_boxCollider.transform.position + _boxCollider.center, _boxCollider.size);

                //画一个矩形 表示射线发射的起点高度平面
                Color oldColor1 = Handles.color;
                Handles.color = Color.cyan;
                Handles.DrawLine(
                    new Vector3(_boxCollider.bounds.min.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.min.z),
                    new Vector3(_boxCollider.bounds.max.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.min.z));
                Handles.DrawLine(
                    new Vector3(_boxCollider.bounds.min.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.min.z),
                    new Vector3(_boxCollider.bounds.min.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.max.z));
                Handles.DrawLine(
                    new Vector3(_boxCollider.bounds.max.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.min.z),
                    new Vector3(_boxCollider.bounds.max.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.max.z));
                Handles.DrawLine(
                    new Vector3(_boxCollider.bounds.min.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.max.z),
                    new Vector3(_boxCollider.bounds.max.x, _boxCollider.bounds.max.y + raycastOffsetY,
                        _boxCollider.bounds.max.z));
                Handles.color = oldColor1;


                if (moduleDebug)
                {
                    //绘制射线起始点位置可视化网格
                    List<Vector3> pointList = new List<Vector3>();
                    for (int i = 0; i < heightMapResolutionSize.x; i++)
                    {
                        float x = _boxCollider.bounds.min.x + i * worldStep;
                        float y = _boxCollider.bounds.max.y + raycastOffsetY;
                        float z = _boxCollider.bounds.min.z;

                        pointList.Add(new Vector3(x, y, z));
                        pointList.Add(new Vector3(x, y, z + _boxCollider.bounds.size.z));
                    }

                    for (int i = 0; i < heightMapResolutionSize.y; i++)
                    {
                        float x = _boxCollider.bounds.min.x;
                        float y = _boxCollider.bounds.max.y + raycastOffsetY;
                        float z = _boxCollider.bounds.min.z + i * worldStep;

                        pointList.Add(new Vector3(x, y, z));
                        pointList.Add(new Vector3(x + _boxCollider.bounds.size.x, y, z));
                    }

                    Handles.DrawLines(pointList.ToArray());

                    //由于低效  数量多，不绘制该点位
                    //绘制的每一个球都是射线检测点位
                    if ((int)heightMapResolutionSize.x * (int)heightMapResolutionSize.y < 1000)
                    {
                        Vector3[] point = GetRaycastPoint();
                        foreach (var worldPos in point)
                        {
                            DrawScenePoint(worldPos, quarterWorldStep);
                        }
                    }

                    if (useBoxColliderYRange)
                    {
                        CompareFunction oldCmpFunc = Handles.zTest;
                        Handles.zTest = CompareFunction.LessEqual;
                        Color oldColor = Handles.color;
                        Handles.color = Color.red;

                        Vector3 drawSize = _boxCollider.size;
                        Vector2 minMaxY = GetBoxColliderMinMaxY(_boxCollider);
                        drawSize.y = minMaxY.y - minMaxY.x;
                        Handles.DrawWireCube(_boxCollider.transform.position + _boxCollider.center, drawSize);

                        Handles.color = oldColor;
                        Handles.zTest = oldCmpFunc;
                    }

                }

                return true;
            }

            return false;
        }

        private void DrawScenePoint(Vector3 worldPos, float size = 0.05f)
        {
            // Handles.DrawWireCube(worldPos, Vector3.one * size);
            Handles.SphereHandleCap(0, worldPos, Quaternion.identity, size, EventType.Repaint);
        }

        private bool GetRaycastHeight(Vector3 worldPos, out float height)
        {
            height = 0;
            Ray ray = new Ray(worldPos, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, float.MaxValue, layerMask, QueryTriggerInteraction.Ignore))
            {
                height = hit.point.y;
                return true;
            }

            return false;
        }

        //获取所有射线检测的起点
        private Vector3[] GetRaycastPoint()
        {
            List<Vector3> pointList = new List<Vector3>();
            int horizontallCount = (int)heightMapResolutionSize.x;
            int verticalCount = (int)heightMapResolutionSize.y;

            for (int i = 0; i < verticalCount; i++)
            {
                for (int j = 0; j < horizontallCount; j++)
                {
                    float x = _boxCollider.bounds.min.x + j * worldStep + halfWorldStep;
                    float y = _boxCollider.bounds.max.y + raycastOffsetY;
                    float z = _boxCollider.bounds.min.z + i * worldStep + halfWorldStep;

                    pointList.Add(new Vector3(x, y, z));
                }
            }

            return pointList.ToArray();
        }

    }
}