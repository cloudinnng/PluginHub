using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PluginHub.Helper;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class GaiaTerrainModule : PluginHubModuleBase
    {
        public override ModuleType moduleType => ModuleType.Construction;
        private Terrain terrainComponent;
        private List<TerrainData> terrainDatas = new List<TerrainData>();

        public override string moduleDescription => "";
        private int terrainTileCountX
        {
            get { return EditorPrefs.GetInt($"{PluginHubFunc.ProjectUniquePrefix}_{GetType()}_GaiaTerrainTileCountX", 4); }
            set { EditorPrefs.SetInt($"{PluginHubFunc.ProjectUniquePrefix}_{GetType()}_GaiaTerrainTileCountX", value); }
        }

        private int terrainTileCountZ
        {
            get { return EditorPrefs.GetInt($"{PluginHubFunc.ProjectUniquePrefix}_{GetType()}_GaiaTerrainTileCountZ", 4); }
            set { EditorPrefs.SetInt($"{PluginHubFunc.ProjectUniquePrefix}_{GetType()}_GaiaTerrainTileCountZ", value); }
        }


        private int xCount;
        private int zCount;
        private int useIndex;

        private bool autoUseSelectedTerrain = true;


        private string colorMapPathTemplate = @"Assets/TerrainData/zaodu/ColorTiles/Color_{0}.jpg";

        private string layerSavePathTemplate =
            @"Assets/TerrainData/zaodu/TerrainLayer/layer_{0}_{1:00}.terrainlayer";

        private string highMapPathTemplate = @"Assets/TerrainData/zaodu/HighTiles/High_{0}.png";

        private float terrainHeight = 100;

        private float heightScale
        {
            get { return EditorPrefs.GetFloat($"{PluginHubFunc.ProjectUniquePrefix}_TerrainModule_HeightScale", 1); }
            set { EditorPrefs.SetFloat($"{PluginHubFunc.ProjectUniquePrefix}_TerrainModule_HeightScale", value); }
        }

        private float heightOffset
        {
            get { return EditorPrefs.GetFloat($"{PluginHubFunc.ProjectUniquePrefix}_TerrainModule_HeightOffset", 0); }
            set { EditorPrefs.SetFloat($"{PluginHubFunc.ProjectUniquePrefix}_TerrainModule_HeightOffset", value); }
        }

        private int rotateCount
        {
            get { return EditorPrefs.GetInt($"{PluginHubFunc.ProjectUniquePrefix}_TerrainModule_RotateCount", 0); }
            set { EditorPrefs.SetInt($"{PluginHubFunc.ProjectUniquePrefix}_TerrainModule_RotateCount", value); }
        }



        protected override void DrawGuiContent()
        {
            GUILayout.BeginHorizontal();
            {
                terrainComponent =
                    (Terrain)EditorGUILayout.ObjectField("Terrain", terrainComponent, typeof(Terrain), true);
                if (autoUseSelectedTerrain || GUILayout.Button("拾取选中的地形", GUILayout.ExpandWidth(false)))
                {
                    GameObject[] terrainObjs = Selection.gameObjects;
                    if (terrainObjs.Length > 0 && terrainObjs[0].GetComponent<Terrain>())
                    {
                        terrainComponent = terrainObjs[0].GetComponent<Terrain>();
                    }
                }

                autoUseSelectedTerrain =
                    GUILayout.Toggle(autoUseSelectedTerrain, "自动使用选中的地形", GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                terrainTileCountX = EditorGUILayout.IntField("地形切片X数量", terrainTileCountX);
                terrainTileCountZ = EditorGUILayout.IntField("地形切片Z数量", terrainTileCountZ);
            }
            GUILayout.EndHorizontal();

            if (terrainComponent != null)
            {
                //regex
                Regex re = new Regex("Terrain_(.{1,2})_(.{1,2})-");
                Match match = re.Match(terrainComponent.name);
                // GUILayout.Label(match.Groups[0].Value + " " + match.Groups[1].Value);

                //Gaia 瓦片编号转换为Unity地形的瓦片编号
                xCount = int.Parse(match.Groups[1].Value);
                zCount = terrainTileCountZ - int.Parse(match.Groups[2].Value) - 1;
                useIndex = zCount * terrainTileCountZ + xCount + 1;

                GUILayout.Label($"xCount:{xCount}  zCount:{zCount}");
                GUILayout.Label($"useIndex:{useIndex}");
            }

            //高度----------------------------------------------------------------------------------------------------

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("地形高：");
                terrainHeight = EditorGUILayout.FloatField(terrainHeight);
            }
            GUILayout.EndHorizontal();

            highMapPathTemplate = EditorGUILayout.TextField("高程图路径模板", highMapPathTemplate);
            string highMapPath = string.Format(highMapPathTemplate, useIndex);
            DrawRow("高程图路径", highMapPath);
            bool highMapExist = File.Exists(highMapPath); //高图是否存在

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("高度图旋转次数（单次旋转90度）", GUILayout.MaxWidth(150));
                rotateCount = GUILayout.Toolbar(rotateCount, new string[] { "0", "1", "2", "3" });
            }
            GUILayout.EndHorizontal();

            heightOffset = EditorGUILayout.Slider("高程偏移", heightOffset, -1, 1);
            heightScale = EditorGUILayout.Slider("高程缩放", heightScale, 0, 1.5f);

            GUI.enabled = highMapExist;
            if (GUILayout.Button("应用高度图"))
            {
                ApplyHigh(terrainComponent.terrainData, highMapPath);
            }

            GUI.enabled = true;

            //颜色----------------------------------------------------------------------------------------------------
            colorMapPathTemplate = EditorGUILayout.TextField("颜色图路径模板", colorMapPathTemplate);
            string colorMapPath = string.Format(colorMapPathTemplate, useIndex);
            DrawRow("颜色图路径", colorMapPath);
            bool colorMapExist = File.Exists(colorMapPath);

            layerSavePathTemplate = EditorGUILayout.TextField("图层保存路径模板", layerSavePathTemplate);
            string layerSavePath = string.Format(layerSavePathTemplate, zCount, xCount);
            DrawRow("图层保存路径", layerSavePath);


            GUI.enabled = highMapExist;
            if (GUILayout.Button("创建并应用颜色图"))
            {
                ApplyColor(terrainComponent.terrainData, colorMapPath, layerSavePath);
            }

            GUI.enabled = true;


            if (GUILayout.Button("一起应用", GUILayout.Height(50)))
            {
                ApplyHigh(terrainComponent.terrainData, highMapPath);
                ApplyColor(terrainComponent.terrainData, colorMapPath, layerSavePath);
            }


            //批处理----------------------------------------------------------------------------------------------------
            GUILayout.Label("批处理：");

            GUILayout.Label($"terrainDatas: {terrainDatas.Count}");
            if (GUILayout.Button("拾取terrainDatas"))
            {
                terrainDatas.Clear();
                terrainDatas.AddRange(GameObject.FindObjectsOfType<Terrain>().Select(c => c.terrainData).ToList());
            }

            GUI.enabled = terrainDatas.Count > 0;
            if (GUILayout.Button("批量处理"))
            {
                int index = 0;
                for (int x = 0; x < 4; x++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        xCount = x;
                        zCount = 2 - z;

                        useIndex = 1 + zCount * 4 + xCount;
                        Debug.Log(useIndex);


                        TerrainData terrainData = terrainDatas[index++];
                        highMapPath = string.Format(highMapPathTemplate, useIndex);
                        ApplyHigh(terrainData, highMapPath);
                        colorMapPath = string.Format(colorMapPathTemplate, useIndex);
                        layerSavePath = string.Format(layerSavePathTemplate, zCount, xCount);
                        ApplyColor(terrainData, colorMapPath, layerSavePath);
                        //show process bar
                        // Debug.Log(index/(float)terrainDatas.Count);
                    }
                }
            }

            GUI.enabled = true;
        }

        private void ApplyHigh(TerrainData terrainData, string highMapPath)
        {
            //设置地形高度
            terrainData.size = new Vector3(terrainData.size.x, terrainHeight, terrainData.size.z);
            Texture2D heightTex = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(highMapPath);
            //映射高层图到地形上
            ApplyHeightmap(heightTex, terrainData, rotateCount);
        }

        private void ApplyColor(TerrainData terrainData, string colorMapPath, string layerSavePath)
        {
            //生成TerrainLayer
            TerrainLayer terrainLayer = new TerrainLayer();
            //颜色图的引用
            Texture2D colorTex = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(colorMapPath);
            terrainLayer.diffuseTexture = colorTex;
            terrainLayer.tileSize = new Vector2(terrainData.size.x, terrainData.size.z);
            //TerrainLayer保存位置
            AssetDatabase.CreateAsset(terrainLayer, layerSavePath);
            terrainData.SetTerrainLayersRegisterUndo(new TerrainLayer[] { terrainLayer }, "UndoName");

            //把splatmap设置为一张，透明度为1，以便地形纹理可见
            float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 1];
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                for (int x = 0; x < terrainData.alphamapWidth; x++)
                {
                    map[x, y, 0] = 1; //设置透明度为1
                }
            }

            terrainData.SetAlphamaps(0, 0, map);
        }


        //使用灰度图高度信息映射到地形数据
        public void ApplyHeightmap(Texture2D heightmap, TerrainData terrain, int rotateCount = 0)
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
                    heightmapData[y, x] = map[y * heightmapResolution + x].grayscale * heightScale + heightOffset;
                }
            }

            for (int i = 0; i < rotateCount; i++)
            {
                heightmapData = RotateMatrix(heightmapData, heightmapResolution);
            }

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