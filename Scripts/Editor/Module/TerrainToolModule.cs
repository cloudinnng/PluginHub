using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginHub.Editor;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PluginHub.Editor
{
//一个地形工具，请根据项目需求自行修改
    public class TerrainToolModule : PluginHubModuleBase
    {
        //layout paras
        private Vector3 scrollPos;
        private int selectTab;


        //所有工具公用的变量
        private int _globalTerrainCount;
        private Terrain[] _globalTerrains;

        public override ModuleType moduleType => ModuleType.Construction;
        //返回是否所有地形都已经赋值
        private bool AllGlobalTerrainVaild()
        {
            for (int i = 0; i < _globalTerrains.Length; i++)
            {
                if (_globalTerrains[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void DrawGuiContent()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            //画公用变量
            GUILayout.Label("公用变量");

            int oldTerrainCount = _globalTerrainCount;
            GUILayout.BeginHorizontal();
            {
                _globalTerrainCount = EditorGUILayout.IntField("地形数量", _globalTerrainCount);
                _globalTerrainCount = Mathf.Clamp(_globalTerrainCount, 0, 999);
                if (GUILayout.Button("自动拾取所有地形"))
                {
                    _globalTerrains = GameObject.FindObjectsOfType<Terrain>();
                    //按照层级顺序排序
                    _globalTerrains = _globalTerrains.OrderBy((terrain) => terrain.transform.GetSiblingIndex())
                        .ToArray();
                    _globalTerrainCount = _globalTerrains.Length;
                }
            }
            GUILayout.EndHorizontal();

            if (_globalTerrainCount != oldTerrainCount) //重新调整地形变量的数量
            {
                Terrain[] oldValues = new Terrain[oldTerrainCount];
                for (int i = 0; i < oldValues.Length; i++)
                {
                    oldValues[i] = _globalTerrains[i];
                }

                _globalTerrains = new Terrain[_globalTerrainCount];
                int count = Math.Min(_globalTerrains.Length, oldValues.Length);
                for (int i = 0; i < count; i++)
                {
                    _globalTerrains[i] = oldValues[i];
                }
            }

            if (_globalTerrains != null)
            {
                for (int i = 0; i < _globalTerrains.Length; i++)
                {
                    _globalTerrains[i] = (Terrain)EditorGUILayout.ObjectField($"Terrain 对象:{i}", _globalTerrains[i],
                        typeof(Terrain), true);
                }
            }

            //画工具栏
            selectTab = GUILayout.Toolbar(selectTab, new string[] { "地形信息", "地形对齐", "树", "高度图映射", "其他", "地形批量生成" });
            switch (selectTab)
            {
                case 0:
                    DrawTool0();
                    break;
                case 1:
                    DrawTool1();
                    break;
                case 2:
                    DrawTool2();
                    break;
                case 3:
                    DrawTool3();
                    break;
                case 4:
                    DrawTool4();
                    break;
                case 5:
                    DrawTool5();
                    break;
            }

            GUILayout.EndScrollView();
        }


        private Terrain _terrainForViewInfo; //用来查看信息的地形

        private void DrawTool0()
        {
            _terrainForViewInfo =
                (Terrain)EditorGUILayout.ObjectField($"Terrain 对象:", _terrainForViewInfo, typeof(Terrain), true);
            GUILayout.BeginVertical("Box");
            {
                if (_terrainForViewInfo != null)
                {
                    // GUILayout.Label(
                    //     $"_terrain.terrainData.alphamapLayers = {_terrainForViewInfo.terrainData.alphamapLayers}");
                    //
                    // GUILayout.BeginHorizontal();
                    // {
                    //     for (int i = 0; i < _terrainForViewInfo.terrainData.alphamapTextures.Length; i++)
                    //     {
                    //         GUILayout.BeginVertical();
                    //         EditorGUILayout.ObjectField(_terrainForViewInfo.terrainData.alphamapTextures[i], typeof(Texture2D), true,GUILayout.Width(70), GUILayout.Height(70));
                    //         GUILayout.Label($"alphamapTextures{i}");
                    //         GUILayout.EndVertical();
                    //     }
                    // }
                    // GUILayout.EndHorizontal();

                    GUILayout.Label($"基础信息：");
                    GUILayout.Label(
                        $"_terrainForViewInfo.heightmapPixelError={_terrainForViewInfo.heightmapPixelError}");
                    GUILayout.Label($"_terrainForViewInfo.basemapDistance={_terrainForViewInfo.basemapDistance}");
                    GUILayout.Label(
                        $"_terrainForViewInfo.terrainData.heightmapResolution={_terrainForViewInfo.terrainData.heightmapResolution}");
                    if (GUILayout.Button("将展示的地形的以上设置，设置给所有拖进来的地形"))
                    {
                        for (int i = 0; i < _globalTerrains.Length; i++)
                        {
                            _globalTerrains[i].heightmapPixelError = _terrainForViewInfo.heightmapPixelError;
                            _globalTerrains[i].basemapDistance = _terrainForViewInfo.basemapDistance;
                            _globalTerrains[i].terrainData.heightmapResolution =
                                _terrainForViewInfo.terrainData.heightmapResolution;
                        }

                        AssetDatabase.SaveAssets();
                    }


                    GUILayout.Label($"树信息：");
                    GUILayout.Label(
                        $"_terrain.terrainData.treeInstances.Length = {_terrainForViewInfo.terrainData.treeInstances.Length}");
                    if (_terrainForViewInfo.terrainData.treeInstances.Length > 0)
                    {
                        GUILayout.Label($"position = {_terrainForViewInfo.terrainData.treeInstances[0].position}");
                        GUILayout.Label($"color = {_terrainForViewInfo.terrainData.treeInstances[0].color}");
                        GUILayout.Label(
                            $"heightScale = {_terrainForViewInfo.terrainData.treeInstances[0].heightScale}");
                        GUILayout.Label(
                            $"lightmapColor = {_terrainForViewInfo.terrainData.treeInstances[0].lightmapColor}");
                        GUILayout.Label($"widthScale = {_terrainForViewInfo.terrainData.treeInstances[0].widthScale}");
                        GUILayout.Label(
                            $"prototypeIndex = {_terrainForViewInfo.terrainData.treeInstances[0].prototypeIndex}");
                    }

                    GUILayout.Label("树设置：");
                    GUILayout.Label($"drawTreesAndFoliage:{_terrainForViewInfo.drawTreesAndFoliage}");
                    GUILayout.Label($"bakeLightProbesForTrees:{_terrainForViewInfo.bakeLightProbesForTrees}");
                    GUILayout.Label($"deringLightProbesForTrees:{_terrainForViewInfo.deringLightProbesForTrees}");
                    GUILayout.Label($"preserveTreePrototypeLayers:{_terrainForViewInfo.preserveTreePrototypeLayers}");
                    GUILayout.Label($"detailObjectDistance:{_terrainForViewInfo.detailObjectDistance}");
                    GUILayout.Label($"detailObjectDensity:{_terrainForViewInfo.detailObjectDensity}");
                    GUILayout.Label($"treeDistance:{_terrainForViewInfo.treeDistance}");
                    GUILayout.Label($"treeBillboardDistance:{_terrainForViewInfo.treeBillboardDistance}");
                    GUILayout.Label($"treeCrossFadeLength:{_terrainForViewInfo.treeCrossFadeLength}");
                    GUILayout.Label($"treeMaximumFullLODCount:{_terrainForViewInfo.treeMaximumFullLODCount}");

                    if (GUILayout.Button("将展示的地形的树设置，设置给所有拖进来的地形"))
                    {
                        for (int i = 0; i < _globalTerrains.Length; i++)
                        {
                            _globalTerrains[i].drawTreesAndFoliage = _terrainForViewInfo.drawTreesAndFoliage;
                            _globalTerrains[i].bakeLightProbesForTrees = _terrainForViewInfo.bakeLightProbesForTrees;
                            _globalTerrains[i].deringLightProbesForTrees =
                                _terrainForViewInfo.deringLightProbesForTrees;
                            _globalTerrains[i].preserveTreePrototypeLayers =
                                _terrainForViewInfo.preserveTreePrototypeLayers;
                            _globalTerrains[i].detailObjectDistance = _terrainForViewInfo.detailObjectDistance;
                            _globalTerrains[i].detailObjectDensity = _terrainForViewInfo.detailObjectDensity;
                            _globalTerrains[i].treeDistance = _terrainForViewInfo.treeDistance;
                            _globalTerrains[i].treeBillboardDistance = _terrainForViewInfo.treeBillboardDistance;
                            _globalTerrains[i].treeCrossFadeLength = _terrainForViewInfo.treeCrossFadeLength;
                            _globalTerrains[i].treeMaximumFullLODCount = _terrainForViewInfo.treeMaximumFullLODCount;
                            _globalTerrains[i].terrainData.treePrototypes =
                                _terrainForViewInfo.terrainData.treePrototypes;

                        }
                    }

                    GUILayout.Label($"网格分辨率信息：");
                    GUILayout.Label($"_terrainForViewInfo.terrainData.size={_terrainForViewInfo.terrainData.size}");
                    if (GUILayout.Button("将展示的地形的以上设置，设置给所有拖进来的地形"))
                    {
                        for (int i = 0; i < _globalTerrains.Length; i++)
                        {
                            _globalTerrains[i].terrainData.size = _terrainForViewInfo.terrainData.size;
                        }
                    }

                    GUILayout.Label($"Texture Resolution：");
                    GUILayout.Label(
                        $"_terrainForViewInfo.terrainData.heightmapResolution={_terrainForViewInfo.terrainData.heightmapResolution}");
                    if (GUILayout.Button("将展示的地形的以上设置，设置给所有拖进来的地形"))
                    {
                        for (int i = 0; i < _globalTerrains.Length; i++)
                        {
                            _globalTerrains[i].terrainData.heightmapResolution =
                                _terrainForViewInfo.terrainData.heightmapResolution;
                        }
                    }

                }
                else
                {
                    GUILayout.Label("拖入地形对象以显示信息");
                }
            }
            GUILayout.EndVertical();
        }

        //tool1 paras
        private float alignOffset = 0; //地形向上与碰撞器对齐的时候的偏移距离
        private LayerMask _layerMask;
        private bool enableUndo = false; //是否启用撤销功能

        private void DrawTool1()
        {
            GUILayout.BeginVertical("Box");
            {
                _layerMask = EditorGUILayout.LayerField("检测的层", _layerMask);

                alignOffset = EditorGUILayout.Slider("对齐偏移距离(单位米)", alignOffset, -1, 1);

                enableUndo = EditorGUILayout.Toggle("启用撤销功能", enableUndo);

                EditorGUILayout.HelpBox(
                    "在地形每一个网格点位处竖直向上进行射线检测，若击中模型碰撞器，则设置地形高度到击中位置高度。将在检测到射线位置的高度基础上加上该偏移，正值地形会更高，负值地形会更矮。默认值为0，即使用检测到碰撞的高度作为地形高度。",
                    MessageType.Info);

                if (GUILayout.Button("地形向上射线检测与模型对齐") && _globalTerrainCount > 0 && _globalTerrains != null &&
                    AllGlobalTerrainVaild())
                {
                    foreach (var terrain in _globalTerrains)
                    {
                        RaycastTerrain2Model(terrain);
                    }
                }
            }
            GUILayout.EndVertical();
        }

        //地形向上射线检测与模型对齐
        private void RaycastTerrain2Model(Terrain terrain)
        {
            int highResolution = terrain.terrainData.heightmapResolution;
            Vector3[,] worldPos = HeightMapWorldPos(terrain);
            float[,] high2Set = terrain.terrainData.GetHeights(0, 0, highResolution, highResolution);

            bool oldQueriesHitBackfaces, oldColliderState;
            RaycastStart(terrain, out oldQueriesHitBackfaces, out oldColliderState);
            for (int x = 0; x < worldPos.GetLength(0); x++)
            {
                for (int z = 0; z < worldPos.GetLength(1); z++)
                {
                    Vector3 oldPos = worldPos[x, z];
                    oldPos.y = 0;

                    Ray ray = new Ray(oldPos, Vector3.up);
                    RaycastHit raycastHit;
                    if (Physics.Raycast(ray, out raycastHit, 99999, ~_layerMask, QueryTriggerInteraction.Ignore))
                    {
                        float yUse = raycastHit.point.y + alignOffset;
                        high2Set[z, x] = (yUse - terrain.GetPosition().y) / terrain.terrainData.size.y;
                    }
                }
            }

            RaycastEnd(terrain, oldQueriesHitBackfaces, oldColliderState);

            if (enableUndo)
                Undo.RecordObject(terrain.terrainData, "Terrain Align");
            terrain.terrainData.SetHeights(0, 0, high2Set);
        }

        private void RaycastStart(Terrain terrain, out bool oldQueriesHitBackfaces, out bool oldColliderState)
        {
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            oldColliderState = true;
            if (terrainCollider != null)
            {
                oldColliderState = terrainCollider.enabled;
                terrainCollider.enabled = false;
            }

            oldQueriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true; //开启检测背面碰撞
        }

        private void RaycastEnd(Terrain terrain, bool oldQueriesHitBackfaces, bool oldColliderState)
        {
            Physics.queriesHitBackfaces = oldQueriesHitBackfaces; //还原设置
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider != null)
            {
                terrainCollider.enabled = oldColliderState;
            }
        }



        //tool2 para


        //private Texture2D _placeTreeTex;//用于放置树木的纹理
        private Texture2D[] _placeTreeTexs;
        private float _treeRandomAmount = 0.001f; //种树位置随机量
        private int _protoIndex = 0;
        private float _heightScaleMin = .9f;
        private float _heightScaleMax = 1.1f;
        private float _widthScaleMin = .9f;
        private float _widthScaleMax = 1.1f;
        private float _treeColorRandomAmount = .3f; //种树颜色随机量
        private float _treeThreshold = 0; //种树阈值，像素颜色大于该值才会种树

        private void DrawTool2() //树
        {
            if (_placeTreeTexs == null || _placeTreeTexs.Length != _globalTerrainCount)
            {
                _placeTreeTexs = new Texture2D[_globalTerrainCount];
            }

            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("种树：");
                if (GUILayout.Button("Clear All Trees"))
                {
                    TreeInstance[] treeInstances = new TreeInstance[0];
                    for (int i = 0; i < _globalTerrains.Length; i++)
                    {
                        _globalTerrains[i].terrainData.SetTreeInstances(treeInstances, true);
                    }
                }
                // if (GUILayout.Button("Test"))
                // {
                //     TreeInstance[] treeInstances = _terrain.terrainData.treeInstances;
                //     //TreeInstance[] treeInstances = new TreeInstance[1];
                //     //treeInstances[0]=new TreeInstance();
                //     treeInstances[0].position = treeInstances[0].position +new Vector3(0,0.01f,0); //是一个0到1的值，在地形本地空间中
                //     _terrain.terrainData.SetTreeInstances(treeInstances,false);
                // }

                //_placeTreeTex = (Texture2D)EditorGUILayout.ObjectField($"树木纹理", _placeTreeTex, typeof(Texture2D), true);

                //画gui种树纹理插槽
                if (_placeTreeTexs != null)
                {
                    bool pair = true; //指示gui布局是否成功配对，用于控制布局
                    for (int i = 0; i < _placeTreeTexs.Length; i++)
                    {
                        //if(i == 0 || i == 4)
                        if (i % 4 == 0) //一排四个
                        {
                            GUILayout.BeginHorizontal();
                            pair = false;
                        }

                        //纹理插槽
                        _placeTreeTexs[i] = (Texture2D)EditorGUILayout.ObjectField(_placeTreeTexs[i], typeof(Texture2D),
                            false, GUILayout.Width(70), GUILayout.Height(70));
                        //if(i == 3 || i == 7)
                        if (i % 4 == 3)
                        {
                            GUILayout.EndHorizontal();
                            pair = true;
                        }
                    }

                    if (pair == false) //若未配对  为其配对上
                    {
                        GUILayout.EndHorizontal();
                        pair = true;
                    }
                }

                _treeRandomAmount = EditorGUILayout.Slider("种树位置随机量", _treeRandomAmount, 0f, 0.01f);
                _protoIndex = EditorGUILayout.IntField("种树原型索引", _protoIndex);

                _heightScaleMin = EditorGUILayout.FloatField("树高最小缩放", _heightScaleMin);
                _heightScaleMax = EditorGUILayout.FloatField("树高最大缩放", _heightScaleMax);
                _widthScaleMin = EditorGUILayout.FloatField("树宽最小缩放", _widthScaleMin);
                _widthScaleMax = EditorGUILayout.FloatField("树宽最大缩放", _widthScaleMax);
                _treeColorRandomAmount = EditorGUILayout.Slider("树颜色随机量", _treeColorRandomAmount, 0, .9f);
                _treeThreshold = EditorGUILayout.Slider("种树阈值", _treeThreshold, 0, 0.999f);

                //判断是否启用"放置树"按钮
                bool enablePlaceTree = true;
                if (_placeTreeTexs != null)
                {
                    for (int i = 0; i < _placeTreeTexs.Length; i++)
                    {
                        if (_placeTreeTexs[i] == null)
                        {
                            enablePlaceTree = false;
                            break;
                        }
                    }
                }
                else
                    enablePlaceTree = false;

                if (!enablePlaceTree) GUILayout.Label("拖入种树遮罩纹理以使用种树");
                if (enablePlaceTree && GUILayout.Button("放置树"))
                {
                    List<TreeInstance> treeInstancesList = new List<TreeInstance>();
                    for (int i = 0; i < _globalTerrains.Length; i++)
                    {
                        Color[] colors = _placeTreeTexs[i].GetPixels();

                        int texW = _placeTreeTexs[i].width;
                        int texH = _placeTreeTexs[i].height;
                        int index = -1;
                        foreach (var color in colors)
                        {
                            index++;
                            //if (color.r > 0) //满足要求就添加树
                            if (color.r > _treeThreshold)
                            {
                                int x = index % texW;
                                int z = index / texW;
                                float highScale = Random.Range(_heightScaleMin, _heightScaleMax);
                                float widthScale = Random.Range(_widthScaleMin, _widthScaleMax);
                                treeInstancesList.Add(new TreeInstance()
                                {
                                    position = new Vector3(
                                        (float)x / texW + Random.Range(-_treeRandomAmount, _treeRandomAmount), 0,
                                        (float)z / texH + Random.Range(-_treeRandomAmount, _treeRandomAmount)),
                                    heightScale = highScale,
                                    widthScale = widthScale,
                                    color = new Color32((byte)(255f * (1 - Random.Range(0f, _treeColorRandomAmount))),
                                        (byte)(255f * (1 - Random.Range(0f, _treeColorRandomAmount))),
                                        (byte)(255f * (1 - Random.Range(0f, _treeColorRandomAmount))), 255),
                                    lightmapColor = Color.white,
                                    prototypeIndex = _protoIndex, rotation = 0,
                                });
                            }
                        }

                        _globalTerrains[i].terrainData.SetTreeInstances(treeInstancesList.ToArray(), true);
                        treeInstancesList.Clear(); //
                    }

                    //_terrains.terrainData.SetTreeInstances(treeInstancesList.ToArray(),true);
                    //_terrain.terrainData.drit
                }

                if (GUILayout.Button("处理树"))
                {
                    for (int i = 0; i < _globalTerrains.Length; i++)
                    {
                        Terrain terrain = _globalTerrains[i];

                        List<TreeInstance> treeInstancesList = terrain.terrainData.treeInstances.ToList();
                        for (int j = 0; j < treeInstancesList.Count; j++)
                        {
                            TreeInstance treeInstance = treeInstancesList[j];

                            treeInstance.heightScale = Random.Range(_heightScaleMin, _heightScaleMax);
                            treeInstance.widthScale = Random.Range(_widthScaleMin, _widthScaleMax);
                            Color color = new Color32((byte)(255f * (1 - Random.Range(0f, _treeColorRandomAmount))),
                                (byte)(255f * (1 - Random.Range(0f, _treeColorRandomAmount))),
                                (byte)(255f * (1 - Random.Range(0f, _treeColorRandomAmount))), 255);
                            treeInstance.color = color;
                            treeInstance.lightmapColor = color;
                            //树原型索引
                            treeInstance.prototypeIndex = Random.Range(0, terrain.terrainData.treePrototypes.Length);
                            treeInstance.rotation = Random.Range(0, 360);

                            treeInstancesList[j] = treeInstance;
                        }

                        terrain.terrainData.SetTreeInstances(treeInstancesList.ToArray(), true);
                    }

                }
            }
            GUILayout.EndVertical();
        }

        //tool3 paras
        private Rect dropRect;
        private Terrain terrain;
        private Texture2D heightMap;
        private float _adjustmentScale = 1;

        private void DrawTool3()
        {
            EditorGUILayout.BeginVertical("box");

            terrain = (Terrain)EditorGUILayout.ObjectField("Terrain Object", terrain, typeof(Terrain), true);
            heightMap = (Texture2D)EditorGUILayout.ObjectField("Height Map Texture", heightMap, typeof(Texture2D),
                false);

            if (terrain == null || heightMap == null)
                EditorGUILayout.HelpBox(
                    "You need both a terrain object and height map texutre set before you can map the two.",
                    MessageType.Warning);
            else if (GUILayout.Button("Map"))
                ApplyHeightmap(heightMap, terrain.terrainData);

            EditorGUILayout.EndVertical();


            EditorGUILayout.LabelField("地形高度缩放功能：-------------------↓");
            //将会对目前地形的高度做_adjustmentScale倍数调整
            _adjustmentScale = EditorGUILayout.FloatField("高度缩放：", _adjustmentScale);

            if (GUILayout.Button("调整高度"))
            {
                if (_globalTerrains != null && _globalTerrains.Length > 0)
                {
                    foreach (var terrain in _globalTerrains)
                    {
                        if (terrain == null) continue;
                        int highResolution = terrain.terrainData.heightmapResolution;
                        float[,] highData = terrain.terrainData.GetHeights(0, 0, highResolution, highResolution);
                        //对高度进行缩放
                        for (int y = 0; y < highResolution; y++)
                        {
                            for (int x = 0; x < highResolution; x++)
                            {
                                highData[y, x] = highData[y, x] * _adjustmentScale;
                            }
                        }

                        terrain.terrainData.SetHeights(0, 0, highData);
                    }
                }
            }



            //从磁盘载入Texture2D
            // if (GUILayout.Button("从磁盘载入Texture2D"))
            // {
            //     for (int i = 0; i < _terrains.Length; i++)
            //     {
            //         Terrain terrain = _terrains[i];
            //         string name = terrain.name;
            //         //terraindata_0_02
            //         int x = int.Parse(name.Substring(12,1));
            //         int y = int.Parse(name.Substring(14,2));
            //         
            //         string filePath =
            //             $@"D:\unityproject2019\YarlungZangboRiverWaterVideo\Assets\Terrain\YJ\highTiles\high_{x}_{y:00}.jpg";
            //         
            //         Debug.Log(filePath);
            //         byte[] fileData;
            //         if (File.Exists(filePath))     {
            //             fileData = File.ReadAllBytes(filePath);
            //             heightMap = new Texture2D(2, 2);
            //             heightMap.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            //             heightMap.Apply(false);
            //             ApplyHeightmap(heightMap, terrain.terrainData);
            //             // terrain.terrainData.heightmapResolution = 513;
            //             // terrain.terrainData.heightmapResolution = 2049;
            //             
            //         }
            //     }
            // }
        }

        private Texture2D texture2Deal;


        private void DrawTool4()
        {
            texture2Deal = (Texture2D)EditorGUILayout.ObjectField("Texture", texture2Deal, typeof(Texture2D), false);

            if (GUILayout.Button("处理"))
            {
                Texture2D tex = new Texture2D(texture2Deal.width, texture2Deal.height, TextureFormat.RGB24, false);

                Color[] colors = texture2Deal.GetPixels();

                for (int i = 0; i < colors.Length; i++)
                {
                    Color color = colors[i];
                    if (color.r < 0.25f)
                    {
                        colors[i] = Color.red;
                    }
                    else if (color.r < 0.5f)
                    {
                        colors[i] = Color.green;
                    }
                    else if (color.r < 0.75f)
                    {
                        colors[i] = Color.black;
                    }
                    else
                    {
                        colors[i] = Color.blue;
                    }
                }

                tex.SetPixels(colors);
                tex.Apply(false);
                File.WriteAllBytes(
                    @"D:\unityproject2020\unity-vfx\Assets/TerrainAssets/RealTerrainTextures/testsplatmap.tga",
                    tex.EncodeToTGA());
            }

        }

        //tool5 paramater
        //单个地形tile尺寸
        private Vector3 _terrainSize = new Vector3(1000, 40, 1000);

        private void DrawTool5()
        {
            _terrainSize = EditorGUILayout.Vector3Field("Terrain Size :", _terrainSize);

            if (GUILayout.Button("批量生成Terrain"))
            {
                //记得修改行列数
                int colCount = 3;
                int rowCount = 3;

                GameObject terrainRootObj = new GameObject("TerrainRoot");

                int tileIndex = 0;
                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        GameObject terrainObj = new GameObject($"Terrain{row}_{col}"); //地形的名称
                        Terrain terrainComponent = terrainObj.AddComponent<Terrain>();
                        terrainComponent.terrainData = new TerrainData();
                        terrainComponent.allowAutoConnect = true;
                        //给地形赋予默认地形材质
                        terrainComponent.materialTemplate =
                            UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Terrain-Diffuse.mat");

                        terrainObj.AddComponent<TerrainCollider>().terrainData = terrainComponent.terrainData;
                        //Terrain 对象的位置
                        terrainObj.transform.position = new Vector3(col * _terrainSize.z, 0,
                            -row * _terrainSize.x - _terrainSize.z);

                        //映射高层图
                        terrainComponent.terrainData.heightmapResolution = 513; // 2049;//513 高图分辨率，根据需要设置
                        terrainComponent.terrainData.size = _terrainSize;
                        //高度图的引用
                        string highMapPath =
                            $@"Assets/TerrainAssets/High_Cropped/images/my_high_{tileIndex + 1:d2}.png";

                        Texture2D heightTex = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(highMapPath);
                        Debug.Log(highMapPath);
                        Debug.Log(heightTex);
                        ApplyHeightmap(heightTex, terrainComponent.terrainData);

                        //生成TerrainLayer
                        TerrainLayer terrainLayer = new TerrainLayer();
                        //颜色图的引用
                        Texture2D colorTex =
                            (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(
                                $"Assets/TerrainAssets/Color_Cropped/cropped_{tileIndex}({row}_{col}).jpg");
                        //Debug.Log(texture2D);
                        terrainLayer.diffuseTexture = colorTex;
                        terrainLayer.tileSize = new Vector2(_terrainSize.x, _terrainSize.z);
                        //TerrainLayer保存位置
                        AssetDatabase.CreateAsset(terrainLayer,
                            $"Assets/TerrainAssets/TerrainData/TerrainLayer{row}_{col}.terrainlayer");
                        terrainComponent.terrainData.SetTerrainLayersRegisterUndo(new TerrainLayer[] { terrainLayer },
                            "UndoName");


                        //TerrainData 保存到资产
                        AssetDatabase.CreateAsset(terrainComponent.terrainData,
                            $"Assets/TerrainAssets/TerrainData/TerrainData{row}_{col}.asset");

                        terrainObj.transform.parent = terrainRootObj.transform;
                        tileIndex++;
                    }
                }

                AssetDatabase.SaveAssets();
            }

        }

        // private void OnDrawGizmosSelected()
        // {
        //     if (!drawGizmos)
        //     {
        //         return;
        //     }
        //     Vector3[,] worldPos = HeightMapWorldPos(terrain);
        //
        //     foreach (var VARIABLE in worldPos)
        //     {    
        //         Gizmos.DrawSphere(VARIABLE, gizmosSize);
        //     }
        // }

        //获取地形所有高度图的世界位置 [heightmapResolution,heightmapResolution]
        public Vector3[,] HeightMapWorldPos(Terrain terrain)
        {
            Vector3 terrainPos = terrain.GetPosition();
            int highResolution = terrain.terrainData.heightmapResolution;
            float stepX = terrain.terrainData.size.x / (highResolution - 1);
            float stepZ = terrain.terrainData.size.z / (highResolution - 1);
            Vector3[,] heightMapWorldPos = new Vector3[highResolution, highResolution];
            float[,] heights = terrain.terrainData.GetHeights(0, 0, highResolution, highResolution);
            for (int x = 0; x < highResolution; x++)
            {
                for (int z = 0; z < highResolution; z++)
                {
                    Vector3 worldPos = new Vector3(x * stepX, heights[z, x] * terrain.terrainData.size.y, z * stepZ);
                    worldPos += terrainPos;
                    heightMapWorldPos[x, z] = worldPos;
                }
            }

            return heightMapWorldPos;
        }

        //使用灰度图高度信息映射到地形数据
        public static void ApplyHeightmap(Texture2D heightmap, TerrainData terrain, int rotateCount = 0)
        {
            if (heightmap == null)
            {
                Debug.LogError("heightmap was null");
                return;
            }

            int w = heightmap.width;
            int h = heightmap.height;
            int heightmapResolution = terrain.heightmapResolution;
            float[,] heightmapData = terrain.GetHeights(0, 0, heightmapResolution, heightmapResolution);
            Color[] mapColors = heightmap.GetPixels(); //高图图片上的颜色像素
            Color[] map = new Color[heightmapResolution * heightmapResolution];
            if (heightmapResolution != w || h != w)
            {
                Debug.LogWarning("Resize Texture");
                // Resize using nearest-neighbor scaling if texture has no filtering 如果纹理没有过滤，则使用最近邻缩放来调整大小
                if (heightmap.filterMode == FilterMode.Point)
                {
                    float dx = (float)w / (float)heightmapResolution;
                    float dy = (float)h / (float)heightmapResolution;
                    for (int y = 0; y < heightmapResolution; y++)
                    {
                        if (y % 20 == 0)
                        {
                            EditorUtility.DisplayProgressBar("Resize", "Calculating texture",
                                Mathf.InverseLerp(0.0f, heightmapResolution, y));
                        }

                        int thisY = Mathf.FloorToInt(dy * y) * w;
                        int yw = y * heightmapResolution;
                        for (int x = 0; x < heightmapResolution; x++)
                        {
                            map[yw + x] = mapColors[Mathf.FloorToInt(thisY + dx * x)];
                        }
                    }
                }
                // Otherwise resize using bilinear filtering
                else
                {
                    float ratioX = (1.0f / ((float)heightmapResolution / (w - 1)));
                    float ratioY = (1.0f / ((float)heightmapResolution / (h - 1)));
                    for (int y = 0; y < heightmapResolution; y++)
                    {
                        if (y % 20 == 0)
                        {
                            EditorUtility.DisplayProgressBar("Resize", "Calculating texture",
                                Mathf.InverseLerp(0.0f, heightmapResolution, y));
                        }

                        int yy = Mathf.FloorToInt(y * ratioY);
                        int y1 = yy * w;
                        int y2 = (yy + 1) * w;
                        int yw = y * heightmapResolution;
                        for (int x = 0; x < heightmapResolution; x++)
                        {
                            int xx = Mathf.FloorToInt(x * ratioX);
                            Color bl = mapColors[y1 + xx];
                            Color br = mapColors[y1 + xx + 1];
                            Color tl = mapColors[y2 + xx];
                            Color tr = mapColors[y2 + xx + 1];
                            float xLerp = x * ratioX - xx;
                            map[yw + x] = Color.Lerp(Color.Lerp(bl, br, xLerp), Color.Lerp(tl, tr, xLerp),
                                y * ratioY - (float)yy);
                        }
                    }
                }

                EditorUtility.ClearProgressBar();
            }
            else
            {
                // Use original if no resize is needed
                map = mapColors;
            }

            // Assign texture data to heightmap
            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    heightmapData[y, x] = map[y * heightmapResolution + x].grayscale;
                }
            }

            // for (int i = 0; i < rotateCount; i++)
            // {
            //     heightmapData = RotateMatrix(heightmapData, heightmapResolution);
            // }

            terrain.SetHeights(0, 0, heightmapData);
        }

        //将一个方形二维数组矩阵中的值 旋转90度。n是矩阵边长
        private static float[,] RotateMatrix(float[,] matrix, int n)
        {
            float[,] ret = new float[n, n];

            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    ret[i, j] = matrix[n - j - 1, i];
                }
            }

            return ret;
        }

    }
}