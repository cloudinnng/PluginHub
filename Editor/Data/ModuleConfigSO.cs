using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


namespace PluginHub.Data
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ModuleConfigSO))]
    public class ModuleConfigSOEditor : UnityEditor.Editor
    {
        private ModuleConfigSO targetScript => (ModuleConfigSO)target;
        //模块前缀
        private string moduleFolder = "Packages/com.hellottw.pluginhub/Editor/Module/";
        private string moduleFillterStr = "";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(20);
            
            DrawModuleAddTool();

            GUILayout.Space(20);

            CheckConfigIssue();

            GUILayout.Space(20);

            DrawBottomButton();
        }

        //绘制模块添加工具。
        //显示所有模块，并显示是否添加。并有按钮可以添加到指定的Tab页
        private void DrawModuleAddTool()
        {
            //所有已添加的模块
            string[] addedModules = targetScript.tabConfigs.Select(x =>
            {
                return x.moduleList.Select(y => y.name).ToArray();
            }).SelectMany(x => x).ToArray();
            //所有模块
            string[] moduleFiles = System.IO.Directory.GetFiles(moduleFolder, "*.cs", System.IO.SearchOption.AllDirectories);
            //过滤掉非模块文件, 无法将非模块类作为模块添加
            moduleFiles = moduleFiles.Where(x => x.EndsWith("Module.cs")).ToArray();

            //绘制模块搜索输入框
            moduleFillterStr = EditorGUILayout.TextField("模块搜索：", moduleFillterStr);

            GUILayout.Label($"共有{moduleFiles.Length}个模块，{moduleFiles.Length - addedModules.Length}个未添加。");
            moduleFiles = moduleFiles.Where(x => x.ToLower().Contains(moduleFillterStr.ToLower())).ToArray();
            //绘制所有未添加的模块
            for (int i = 0; i < moduleFiles.Length; i++)
            {
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(moduleFiles[i]);

                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<MonoScript>(moduleFiles[i]),
                        typeof(MonoScript), false);
                    GUI.enabled = true;

                    if (targetScript.tabConfigs.Count > 0)//如果有tab
                    {
                        //模块是否已添加
                        bool alreadyAdded = addedModules.Contains(fileNameWithoutExtension);
                        GUI.enabled = !alreadyAdded;//如果已添加，按钮不可用。模块不可重复添加
                        GUILayout.Label("添加到Tab：", GUILayout.ExpandWidth(false));
                        for (int j = 0; j < targetScript.tabConfigs.Count; j++)
                        {
                            if (GUILayout.Button($"{j}", GUILayout.ExpandWidth(false)))
                            {
                                targetScript.tabConfigs[j].moduleList
                                    .Add(AssetDatabase.LoadAssetAtPath<MonoScript>(moduleFiles[i]));
                            }
                        }
                        GUI.enabled = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        //检查配置的问题
        private void CheckConfigIssue()
        {
            //检查是否有重复添加的模块
            List<MonoScript> monoList = new List<MonoScript>();
            HashSet<MonoScript> monoScripts = new HashSet<MonoScript>();
            for (int i = 0; i < targetScript.tabConfigs.Count; i++)
            {
                ModuleTabConfig tabConfig = targetScript.tabConfigs[i];
                for (int j = 0; j < tabConfig.moduleList.Count; j++)
                {
                    MonoScript monoScript = tabConfig.moduleList[j];
                    monoList.Add(monoScript);
                    monoScripts.Add(monoScript);
                }
            }
            if (monoList.Count != monoScripts.Count)//利用HashSet的特性，如果有重复的，数量会不一样
            {
                EditorGUILayout.HelpBox("存在重复添加的模块！", MessageType.Warning);
            }
        }

        //绘制底部按钮
        private void DrawBottomButton()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("清空配置"))
                {
                    targetScript.tabConfigs.Clear();
                    MakeConfigDirtyAndSave();
                    PluginHubWindow.RestartWindow();
                }
                if (GUILayout.Button("载入最小模块配置"))
                {
                    MakeMinimalModuleConfig();
                    MakeConfigDirtyAndSave();
                    PluginHubWindow.RestartWindow();
                }
                if (GUILayout.Button("载入默认模块配置"))
                {
                    MakeDefaultModuleConfig();
                    MakeConfigDirtyAndSave();
                    PluginHubWindow.RestartWindow();
                }
            }
            GUILayout.EndHorizontal();


            if (GUILayout.Button("重启 PluginHubWindow", GUILayout.Height(30)))
            {
                MakeConfigDirtyAndSave();
                PluginHubWindow.RestartWindow();
            }
        }


        private void MakeConfigDirtyAndSave()
        {
            //make dirty
            EditorUtility.SetDirty(targetScript);
            AssetDatabase.SaveAssets();
        }

        #region 两个预设配置
        //最小模块配置
        private void MakeMinimalModuleConfig()
        {
            targetScript.tabConfigs.Clear();

            targetScript.tabConfigs.Add(new ModuleTabConfig()
            {
                tabName = "快捷方式",
                moduleList = new List<MonoScript>()
                {
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}NavigationBarModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}SelectionModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}SceneModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}CameraShowModeModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}CommonComponentModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}BuildModule.cs"),
                }
            });
            targetScript.tabConfigs.Add(new ModuleTabConfig()
            {
                tabName = "工具",
                moduleList = new List<MonoScript>()
                {
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}ShaderDebuggerModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}ReferenceFinderModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}AlignModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}TextureProcessModule.cs"),
                }
            });
        }

        //默认模块配置
        private void MakeDefaultModuleConfig()
        {
            targetScript.tabConfigs.Clear();

            targetScript.tabConfigs.Add(new ModuleTabConfig()
            {
                tabName = "快捷方式",
                moduleList = new List<MonoScript>()
                {
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}NavigationBarModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}CommonComponentModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}SelectionModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}SceneModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}CameraShowModeModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}MaterialInspectModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}BuildModule.cs"),
                }
            });
            targetScript.tabConfigs.Add(new ModuleTabConfig()
            {
                tabName = "工具",
                moduleList = new List<MonoScript>()
                {
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}BuildInIconGalleryModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}LightProbePlacementModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}RotateAroundAnimationMakerModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}DistributionModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}MeshToHeightModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}Base64TextureConvertModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}ShaderDebuggerModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}ReferenceFinderModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}AlignModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}ColorConvertModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}ConvertHeightToNormalModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}TextureMetaDataBatchModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}GaiaTerrainModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}RedeemCodeModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}TextureProcessModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}NormalMapMakerModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}TerrainToolModule.cs"),
                }
            });
            targetScript.tabConfigs.Add(new ModuleTabConfig()
            {
                tabName = "优化",
                moduleList = new List<MonoScript>()
                {
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}MaterialOptimizationModule.cs"),
                }
            });
            targetScript.tabConfigs.Add(new ModuleTabConfig()
            {
                tabName = "问题分析",
                moduleList = new List<MonoScript>()
                {
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}ProblemAnalysisModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}MaterialReplaceModule.cs"),
                }
            });
        }
        #endregion
    }

#endif

    //代表一个Tab页的数据
    [System.Serializable]
    public class ModuleTabConfig
    {
        public string tabName = "TabName";
        public List<MonoScript> moduleList = new List<MonoScript>();
    }

    //ScriptableObject资产文件，用于存储哪些模块被添加到哪些Tab页
    [CreateAssetMenu(fileName = "ModuleConfigSO", menuName = "ScriptableObjects/ModuleConfigSO", order = 1)]
    public class ModuleConfigSO : ScriptableObject
    {
        public List<ModuleTabConfig> tabConfigs = new List<ModuleTabConfig>();

        // public Object moduleFolder;
    }
}