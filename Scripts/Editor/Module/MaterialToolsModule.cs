using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PluginHub.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PluginHub.Editor
{

    //扫描依据
    internal enum ScanType
    {
        SameName,
        SameMainTex,
        SimilarName,
    }

    /// <summary>
    /// MeshRenderer材质索引信息
    /// </summary>
    internal struct MRMatIndexInfos
    {
        public MeshRenderer meshRenderer;
        public List<int> matIndexList; //该meshrender中的材质索引  （例，0表示meshRenderer中的第0个材质）
    }

    public class MaterialToolsModule : PluginHubModuleBase
    {
        private Material oldMat;
        private Material newMat;

        //扫描结果字典  <key,materiallist>
        private Dictionary<string, List<Material>> matDic = new Dictionary<string, List<Material>>();

        private Material searchMat; //
        private List<MRMatIndexInfos> meshRenderersTmpList = new List<MRMatIndexInfos>();
        private Material globalSlotMat; //全局槽材质
        private List<Material> similarMatList = new List<Material>(); //用于存储结果
        private List<MRMatIndexInfos> mrRefList = new List<MRMatIndexInfos>(); //用于存储结果
        private bool showAllMaterial = false;


        protected override void DrawGuiContent()
        {
            DrawSplitLine("搜索与提取");

            //DrawSimgleSearchModule
            globalSlotMat = DrawMaterialRow("材质槽", globalSlotMat);

            GUILayout.BeginHorizontal();
            {
                GUI.enabled = IsSelectNewMaterial(globalSlotMat);
                if (GUILayout.Button("材质槽使用选中的材质"))
                {
                    globalSlotMat = (Material)Selection.objects[0];
                }

                GUI.enabled = globalSlotMat != null && IsEmbeddedMaterial(globalSlotMat);
                if (GUILayout.Button("提取材质", GUILayout.Width(100)))
                {
                    Material materialE = ExtractMaterial(globalSlotMat);
                    if (materialE != null) //提取材质成功
                    {
                        oldMat = globalSlotMat;
                        newMat = materialE;
                    }
                }
                GUI.enabled = true;

                GUI.enabled = Selection.objects != null && Selection.objects.Length > 0 && Selection.objects[0] is Material &&
                             IsEmbeddedMaterial(Selection.objects[0] as Material);
                if (GUILayout.Button("提取选中材质（可多选）", GUILayout.ExpandWidth(false)))
                {
                    Object[] selecteds = Selection.objects;
                    if(selecteds != null && selecteds.Length > 0)
                    {
                        for (int i = 0; i < selecteds.Length; i++)
                        {
                            Material mat = selecteds[i] as Material;
                            if(mat != null)
                                ExtractMaterial(mat);
                        }
                    }
                }
                GUI.enabled = true;

                // if (GUILayout.Button("提取所选模型所有材质"))
                // {
                //
                // }
            }
            GUILayout.EndHorizontal();

            bool enableTmp = GUI.enabled;
            GUI.enabled = globalSlotMat != null;

            string showName = globalSlotMat == null ? "NoName" : globalSlotMat.name;
            GUILayout.BeginVertical("Box");
            GUILayout.Label($"搜索与 {showName} 相似材质：{similarMatList.Count}个结果");
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.GuiContent("名称相同", "搜索项目中与之名称相同的材质")))
                {
                    similarMatList.Clear();
                    similarMatList = SearchSimilarMaterials(globalSlotMat, ScanType.SameName);
                }

                if (GUILayout.Button("主纹理相同"))
                {
                    similarMatList.Clear();
                    similarMatList = SearchSimilarMaterials(globalSlotMat, ScanType.SameMainTex);
                }

                if (GUILayout.Button("名称相似"))
                {
                    similarMatList.Clear();
                    similarMatList = SearchSimilarMaterials(globalSlotMat, ScanType.SimilarName);
                }

                if (GUILayout.Button("Clear Result"))
                {
                    similarMatList.Clear();
                }
            }
            GUILayout.EndHorizontal();
            //绘制搜索结果
            for (int i = 0; i < similarMatList.Count; i++)
            {
                Material mat = similarMatList[i];
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.ObjectField("", mat, typeof(Material), false);

                    DrawMaterialTypeLabel(mat);

                    if (GUILayout.Button(PluginHubFunc.GuiContent("替换为该材质", "将所有引用材质槽中材质的Meshrenderer替换为对该材质的引用"),
                            GUILayout.ExpandWidth(false)))
                    {
                        ReplaceMatRef(globalSlotMat, mat);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Label($"搜索场景中的引用：{mrRefList.Count}个结果");
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("搜索"))
                {
                    mrRefList = QuerySceneObject(globalSlotMat);
                }

                if (GUILayout.Button("Clear Result", GUILayout.ExpandWidth(false)))
                {
                    mrRefList.Clear();
                }
            }
            GUILayout.EndHorizontal();
            //绘制搜索结果
            for (int i = 0; i < mrRefList.Count; i++)
            {
                MRMatIndexInfos matIndexInfos = mrRefList[i];
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.ObjectField("", matIndexInfos.meshRenderer.gameObject, typeof(Material), false);

                    if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                    {
                        Selection.objects = new Object[] { matIndexInfos.meshRenderer.gameObject };
                    }

                    if (GUILayout.Button("Independence", GUILayout.ExpandWidth(false)))
                    {
                        bool userSelect = EditorUtility.DisplayDialog("注意",
                            $"将创建{globalSlotMat.name}材质的拷贝，并将该拷贝赋予该对象之前对{globalSlotMat.name}材质的引用槽，以将该材质独立出来，以便您可以单独调节该材质参数。",
                            "OK", "cancel");
                        if (userSelect) //用户选择了ok按钮
                        {
                            string oldPath = AssetDatabase.GetAssetPath(globalSlotMat);
                            string newPath = Path.Combine(Path.GetDirectoryName(oldPath),
                                Path.GetFileNameWithoutExtension(oldPath) + " 1") + Path.GetExtension(oldPath);
                            //Debug.Log(oldPath);
                            //Debug.Log(newPath);
                            AssetDatabase.CopyAsset(oldPath, newPath);
                            //
                            Material copy = AssetDatabase.LoadAssetAtPath<Material>(newPath);
                            //Debug.Log(copy);
                            for (int j = 0; j < matIndexInfos.matIndexList.Count; j++)
                            {
                                Material[] materials = matIndexInfos.meshRenderer.sharedMaterials;
                                materials[matIndexInfos.matIndexList[j]] = copy;
                                matIndexInfos.meshRenderer.sharedMaterials = materials;
                            }
                        }
                    }

                    string s = GetStringRepresentationOfAnArray(matIndexInfos.matIndexList);
                    GUILayout.Label($"引用位置：{s}");
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUI.enabled = enableTmp;


            DrawSplitLine("引用替换");

            //DrawReplaceMatRefModule
            GUILayout.BeginVertical("Box");
            GUILayout.Label("把场景中对此材质的所有引用：");
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Old", GUILayout.Width(50));
                oldMat = (Material)EditorGUILayout.ObjectField("", oldMat, typeof(Material), false);
                DrawMaterialTypeLabel(oldMat);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("替换成此材质：");
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("New", GUILayout.Width(50));
                newMat = (Material)EditorGUILayout.ObjectField("", newMat, typeof(Material), false);
                DrawMaterialTypeLabel(newMat);
            }
            GUILayout.EndHorizontal();

            GUI.enabled = oldMat != null && newMat != null;
            if (GUILayout.Button("执行"))
            {
                ReplaceMatRef(oldMat, newMat);
            }

            GUI.enabled = true;
            GUILayout.EndVertical();

            DrawSplitLine("场景材质扫描");

            //DrawScanningModule
            GUILayout.BeginVertical("Box");
            GUILayout.Label("场景扫描：");
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("名称相同"))
            {
                matDic.Clear();
                matDic = ScanningSceneMaterials(ScanType.SameName);
            }

            if (GUILayout.Button("主纹理相同"))
            {
                matDic.Clear();
                matDic = ScanningSceneMaterials(ScanType.SameMainTex);
            }

            // if (GUILayout.Button("名称相似"))
            // {
            //     matDic.Clear();
            //     matDic = EditorWindowHelper.ScanningSceneMaterials(EditorWindowHelper.ScanByType.SimilarName);
            // }
            if (GUILayout.Button("清空结果"))
            {
                searchMat = null;
                matDic.Clear();
                meshRenderersTmpList.Clear();
            }

            GUILayout.EndHorizontal();

            //绘制
            GUILayout.Label($"该列表有{matDic.Keys.Count.ToString()}组");
            if (matDic != null && matDic.Count > 0)
            {
                string[] keys = matDic.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    string key = keys[i];
                    List<Material> matList = matDic[key];
                    GUILayout.Label($"{i}:{key}");
                    Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(key); //找到这个纹理引用
                    EditorGUILayout.ObjectField(tex, typeof(Texture), true); //画出这个纹理

                    if (GUILayout.Button("ignore"))
                    {
                        matDic.Remove(key);
                        break;
                    }

                    for (int j = 0; j < matList.Count; j++)
                    {
                        Material material = matList[j];

                        GUILayout.BeginHorizontal(); //一行
                        {
                            GUILayout.Label($"[{i}] {j}", GUILayout.ExpandWidth(false));

                            EditorGUILayout.ObjectField(material, typeof(Material), true); //材质域

                            DrawMaterialTypeLabel(material);
                            //🔍 Search icon
                            GUIContent searchGC = PluginHubFunc.Icon("Search On Icon", "");
                            searchGC.tooltip = "在场景中搜索所有引用该材质的Meshrender";

                            if (GUILayout.Button(searchGC, GUILayout.Width(30),
                                    GUILayout.Height(18)))
                            {
                                meshRenderersTmpList.Clear();
                                meshRenderersTmpList = QuerySceneObject(material);
                                searchMat = material;
                            }

                            //download icon
                            GUIContent replaceGC = PluginHubFunc.Icon("Download-Available", "");
                            replaceGC.tooltip = "将前一个按钮的搜索结果的材质引用替换成这一行列出的材质";

                            if (GUILayout.Button(replaceGC, GUILayout.Width(30),
                                    GUILayout.Height(18)))
                            {
                                int counter = 0;
                                for (int k = 0; k < meshRenderersTmpList.Count; k++)
                                {
                                    MRMatIndexInfos mrMatIndexInfos = meshRenderersTmpList[k];
                                    for (int l = 0; l < mrMatIndexInfos.matIndexList.Count; l++)
                                    {
                                        Material[] materials = mrMatIndexInfos.meshRenderer.sharedMaterials;
                                        materials[mrMatIndexInfos.matIndexList[l]] = material;
                                        mrMatIndexInfos.meshRenderer.sharedMaterials = materials;
                                        counter++;
                                    }
                                }

                                Debug.Log($"{counter} replaced");
                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            }

                            //将此组中所有Meshrender的引用替换成这一行列出的材质。
                            if (GUILayout.Button(
                                    PluginHubFunc.GuiContent("replace all", "将此组中所有Meshrender的引用替换成这一行列出的材质。（谨慎使用）"),
                                    GUILayout.Width(100),
                                    GUILayout.Height(18)))
                            {
                                //搜索结果
                                List<List<MRMatIndexInfos>>
                                    matIntListInfos = new List<List<MRMatIndexInfos>>(); //wocao 
                                for (int k = 0; k < matList.Count; k++)
                                {
                                    Material mat = matList[k];
                                    matIntListInfos.Add(QuerySceneObject(mat));
                                }

                                //进行替换所有
                                for (int k = 0; k < matIntListInfos.Count; k++)
                                {
                                    List<MRMatIndexInfos> matInfoList = matIntListInfos[k];

                                    for (int m = 0; m < matInfoList.Count; m++)
                                    {
                                        MRMatIndexInfos mrMatIndexInfos = matInfoList[m];
                                        for (int l = 0; l < mrMatIndexInfos.matIndexList.Count; l++)
                                        {
                                            Material[] materials = mrMatIndexInfos.meshRenderer.sharedMaterials;
                                            materials[mrMatIndexInfos.matIndexList[l]] = material; //替换成这个材质
                                            mrMatIndexInfos.meshRenderer.sharedMaterials = materials;
                                        }
                                    }
                                }

                                //替换完了就删除吧
                                matDic.Remove(key);
                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            }
                        }
                        GUILayout.EndHorizontal();

                        //展示引用搜索结果
                        if (searchMat == material)
                        {
                            GUILayout.BeginVertical("Box");
                            GUILayout.Label($"引用该材质的对象: {meshRenderersTmpList.Count}");
                            if (meshRenderersTmpList != null && meshRenderersTmpList.Count > 0)
                            {
                                for (int k = 0; k < meshRenderersTmpList.Count; k++)
                                {
                                    MRMatIndexInfos mrMatIndexInfos = meshRenderersTmpList[k];
                                    EditorGUILayout.ObjectField(mrMatIndexInfos.meshRenderer, typeof(Material), true);
                                }
                            }

                            GUILayout.EndVertical();
                        }
                    }
                }
            }

            GUILayout.EndVertical();

            DrawSplitLine("场景材质列表");

            GUILayout.BeginVertical("Box");
            {
                showAllMaterial = EditorGUILayout.Foldout(showAllMaterial, "All Materials In Scene");
                if (showAllMaterial)
                {
                    List<Material> matList = AllMaterialsInScene();
                    for (int i = 0; i < matList.Count; i++)
                    {
                        Material material = matList[i];

                        DrawMaterialRow(i.ToString(), material);
                    }
                }
            }
            GUILayout.EndVertical();
        }

        #region Query Functions

        //在场景中查找引用了matToQuery材质的所有网格渲染器，且记录引用的索引
        private static List<MRMatIndexInfos> QuerySceneObject(Material matToQuery)
        {
            List<MRMatIndexInfos> result = new List<MRMatIndexInfos>();
            MeshRenderer[] meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = meshRenderers[i];

                Material[] materials = meshRenderer.sharedMaterials;
                List<int> indexList = new List<int>();

                for (int j = 0; j < materials.Length; j++)
                {
                    if (matToQuery == materials[j]) //这个对象中有一个材质是选中的材质
                    {
                        indexList.Add(j);
                    }
                }

                if (indexList.Count > 0)
                    result.Add(new MRMatIndexInfos() { meshRenderer = meshRenderer, matIndexList = indexList });
            }

            //排序 按照meshrender大小排序
            result = result.OrderByDescending(mrIndexs =>
            {
                Vector3 v = mrIndexs.meshRenderer.bounds.size;
                return Mathf.Max(v.x, v.y, v.z);
            }).ToList();
            return result;
        }

        //获取场景中用到的所有的材质
        public static List<Material> AllMaterialsInScene()
        {
            //思路：收集场景中所有Meshrenderer上的材质，然后再去重
            List<Material> materialList = new List<Material>();
            GameObject.FindObjectsOfType<MeshRenderer>().ToList().ForEach((mr) =>
            {
                Material[] sharedMaterials = mr.sharedMaterials;
                materialList.AddRange(sharedMaterials);
            });
            materialList = materialList.Distinct().ToList(); //去重
            return materialList;
        }

        //获取资产中所有材质
        //路径是相对于project目录的  Assets/xxx
        public static List<string> AllMaterialsInAsset(bool includeEmbeddedMaterial)
        {
            // PerformanceTest.Start();
            HashSet<string> set = new HashSet<string>(); //hashset会自动去重
            //会包括package中的材质
            AssetDatabase.FindAssets("t:Material").ToList().ForEach((guid) =>
            {
                string pathRelativeProject = AssetDatabase.GUIDToAssetPath(guid);
                //排除嵌入式材质
                if (includeEmbeddedMaterial)
                {
                    set.Add(pathRelativeProject);
                }
                else
                {
                    if (!IsEmbeddedMaterial(pathRelativeProject))
                    {
                        set.Add(pathRelativeProject);
                    }
                }
            });
            // PerformanceTest.End();
            // Debug.Log(set.Count);
            return set.ToList();
        }

        //搜索相似的材质
        private static List<Material> SearchSimilarMaterials(Material searchTarget, ScanType type)
        {
            List<string> similarMatList = new List<string>();
            similarMatList.AddRange(AllMaterialsInAsset(false));
            //排除搜索目标本身
            similarMatList = similarMatList.Where((m) => m != AssetDatabase.GetAssetPath(searchTarget)).ToList();
            //排序,自由材质放前面
            similarMatList = similarMatList.OrderBy((m) => IsEmbeddedMaterial(m) ? 1 : 0).ToList();

            switch (type)
            {
                case ScanType.SameName: //名字相同
                    similarMatList = similarMatList.Where((foreachMatPath) =>
                    {
                        string matName = Path.GetFileNameWithoutExtension(foreachMatPath);
                        return searchTarget.name.Equals(foreachMatPath);
                    }).ToList();
                    break;
                case ScanType.SameMainTex: //拥有相同主纹理
                    //主纹理的guid
                    string mainTexGuid =
                        AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(searchTarget.mainTexture));
                    similarMatList = similarMatList.Where((foreachMatPath) =>
                    {
                        string matPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), foreachMatPath);
                        // Debug.Log(matPath);
                        //查看材质的文本内容包不包含主纹理guid
                        //TODO  注意这里只是查找了是否有纹理的引用，并不一定是引用在主纹理位置
                        return Regex.IsMatch(File.ReadAllText(matPath), mainTexGuid);
                    }).ToList();
                    break;
                case ScanType.SimilarName: //名字相似
                    similarMatList = similarMatList.Where((foreachMatPath) =>
                    {
                        string matName = Path.GetFileNameWithoutExtension(foreachMatPath);
                        return IsSimilarName(searchTarget.name, matName);
                    }).ToList();
                    break;
            }

            List<Material> similarMatListReturn = new List<Material>();
            //load material instance
            similarMatList.ForEach((foreachMatPath) =>
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(foreachMatPath);
                similarMatListReturn.Add(mat);
            });

            return similarMatListReturn;
        }

        //扫描场景中的材质
        private static Dictionary<string, List<Material>> ScanningSceneMaterials(ScanType type)
        {
            Dictionary<string, List<Material>> matDic = new Dictionary<string, List<Material>>();
            //场景中所有材质
            List<Material> materialList = AllMaterialsInScene();

            for (int i = 0; i < materialList.Count; i++)
            {
                Material materialRef = materialList[i];

                switch (type)
                {
                    case ScanType.SameName:
                        if (matDic.ContainsKey(materialRef.name))
                            matDic[materialRef.name].Add(materialRef);
                        else
                            matDic.Add(materialRef.name, new List<Material>() { materialRef });
                        break;
                    case ScanType.SameMainTex:
                        if (materialRef.HasProperty("_MainTex")) //所用shader有这个属性
                        {
                            string texturePath = AssetDatabase.GetAssetPath(materialRef.mainTexture);
                            if (!string.IsNullOrWhiteSpace(texturePath)) //主纹理不为空
                            {
                                if (matDic.ContainsKey(texturePath))
                                    matDic[texturePath].Add(materialRef);
                                else
                                    matDic.Add(texturePath, new List<Material>() { materialRef });
                            }
                        }

                        break;
                    // case ScanByType.SimilarName:
                    //     IsSimilarName()
                    //         
                    //     if (matDic.ContainsKey(materialRef.name))
                    //         matDic[materialRef.name].Add(materialRef);
                    //     else
                    //         matDic.Add(materialRef.name, new List<Material>() {materialRef});
                    //     break;
                }
            }

            //过滤掉只有一个材质的组，这不属于我们的优化方案,我们显示出多于一个引用了相同贴图的材质
            matDic = matDic.Where((kvp => kvp.Value.Count > 1)).ToDictionary((keyValuePair) => keyValuePair.Key,
                (keyValuePair) => keyValuePair.Value);

            //排序  自由材质放前面
            List<string> keys = new List<string>(matDic.Keys);
            for (int i = 0; i < matDic.Count; i++)
            {
                matDic[keys[i]] = matDic[keys[i]].OrderBy((m) => IsEmbeddedMaterial(m) ? 1 : 0).ToList();
            }

            return matDic;
        }


        #endregion



        #region Draw

        public static Material DrawMaterialRow(string text, Material material)
        {
            Material returnMat;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(text);

                returnMat = (Material)EditorGUILayout.ObjectField(material, typeof(Material), true);

                DrawMaterialTypeLabel(material);
            }
            GUILayout.EndHorizontal();
            return returnMat;
        }

        //画材质类型标签(嵌入式材质还是自由材质)
        public static void DrawMaterialTypeLabel(Material material)
        {
            string matType = "";
            if (material == null)
                matType = "";
            else if (IsEmbeddedMaterial(material))
                matType = "Embedded"; //嵌入式材质
            else
                matType = "Free"; //自由材质
            GUILayout.Label(matType, GUILayout.MaxWidth(50));
        }

        #endregion



        #region Copmplex Helper Function

        //把场景中MeshRenderer对oldMat材质的所有引用,替换成newMat材质
        public static void ReplaceMatRef(Material oldMat, Material newMat)
        {
            List<MRMatIndexInfos> list = QuerySceneObject(oldMat);
            int counter = 0;
            for (int i = 0; i < list.Count; i++)
            {
                MRMatIndexInfos mrMatIndexInfos = list[i];

                for (int j = 0; j < mrMatIndexInfos.matIndexList.Count; j++)
                {
                    int index = mrMatIndexInfos.matIndexList[j];

                    Material[] materials = mrMatIndexInfos.meshRenderer.sharedMaterials;
                    materials[index] = newMat;
                    mrMatIndexInfos.meshRenderer.sharedMaterials = materials;
                    counter++;
                }
            }
            Debug.Log($"替换了{counter}个");
        }


        //提取单个材质到同目录Materials文件夹内
        public static Material ExtractMaterial(Material embeddedMat)
        {
            //原理是复制该材质，存为材质资产，因此不会丢失原嵌入式材质
            //一般在检视面板都是提取整个fbx的材质，这个是提取单个材质
            string savePath = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(embeddedMat)), $"Materials/{embeddedMat.name}.mat");

            if (File.Exists(savePath))
            {
                Debug.Log($"材质已存在{savePath}，无需提取。");
                return AssetDatabase.LoadAssetAtPath<Material>(savePath);
            }
            string folderPath = Path.GetDirectoryName(savePath);

            if (Directory.Exists(folderPath) == false)
            {
                Debug.Log($"创建文件夹{folderPath}");
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                Material newMat = Object.Instantiate(embeddedMat);
                AssetDatabase.CreateAsset(newMat, savePath);
                Debug.Log($"已提取，点击定位。{savePath}。", newMat);
                // Selection.objects = new[] {newMat};//选中
                return newMat;
            }

            return null;
        }

        #endregion


        #region Simple Helper Function
        //返回一个材质是否是一个嵌入式材质
        public static bool IsEmbeddedMaterial(Material material)
        {
            return IsEmbeddedMaterial(AssetDatabase.GetAssetPath(material));
        }

        public static bool IsEmbeddedMaterial(string materialPath)
        {
            //不是.mat结尾，即为嵌入式材质
            return !materialPath.EndsWith(".mat");
        }

        //是否选中了新的材质
        public static bool IsSelectNewMaterial(Material oldMaterial)
        {
            if (Selection.objects == null)
                return false;
            if (Selection.objects.Length <= 0)
                return false;
            Material select = Selection.objects[0] as Material;
            if (select == null)
                return false;
            if (select == oldMaterial)
                return false;
            return true;
        }

        //输入材质名称  返回是否是相似的名称
        //下面是一些用例
        //Material #20100 1
        //Material #20100
        //WADC-ZY0006 1
        //WADC-ZY0006 2
        //20210916HX14 1
        //20210916HX14
        //WADC-ZY0021
        //WADC-ZY0171
        public static bool IsSimilarName(string name0, string name1)
        {
            if (name0.Equals(name1)) //相同必定相似
                return true;

            // 其中一个名字去掉最后一个空格后面部分与另一个名字相同
            int spaceIndex0 = name0.LastIndexOf(' ');
            int spaceIndex1 = name1.LastIndexOf(' ');
            if (spaceIndex0 > 0 && name1.Trim().Equals(name0.Substring(0, spaceIndex0).Trim()))
                return true;
            if (spaceIndex1 > 0 && name0.Trim().Equals(name1.Substring(0, spaceIndex1).Trim()))
                return true;
            //两个名字分别去掉最后一个空格后面的部分后相同,且后面的部分是纯数字
            if (spaceIndex0 > 0 && spaceIndex1 > 0 &&
                name0.Substring(0, spaceIndex0).Trim().Equals(name1.Substring(0, spaceIndex1).Trim())
                && name0.Substring(spaceIndex0).IsInt() && name1.Substring(spaceIndex1).IsInt())
                return true;

            return false;
        }


        //获取数组的字符串表达形式
        private static string GetStringRepresentationOfAnArray(IEnumerable array)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in array)
            {
                sb.Append(item.ToString());
                sb.Append(",");
            }

            return sb.ToString();
        }

        #endregion

        #region 模块信息
        public override ModuleType moduleType => ModuleType.Construction;
        public override string moduleName { get { return "材质工具"; } }
        public override string moduleDescription => "材质综合工具";
        #endregion

    }
}