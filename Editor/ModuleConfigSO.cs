using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


namespace PluginHub
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ModuleConfigSO))]
    public class ModuleConfigSOEditor : UnityEditor.Editor
    {
        private ModuleConfigSO targetScript => (ModuleConfigSO)target;
        //模块前缀
        private string moduleFolder = "Packages/com.hellottw.pluginhub/Editor/Module/";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            //所有已添加的模块
            string[] addedModules = targetScript.tabConfigs.Select(x =>
            {
                return x.moduleList.Select(y => y.name).ToArray();
            }).SelectMany(x => x).ToArray();
            //所有模块
            string[] moduleFiles =
                System.IO.Directory.GetFiles(moduleFolder, "*.cs", System.IO.SearchOption.TopDirectoryOnly);
            GUILayout.Label($"共有{moduleFiles.Length}个模块，{moduleFiles.Length - addedModules.Length}个未添加。");
            //绘制所有未添加的模块
            for (int i = 0; i < moduleFiles.Length; i++)
            {
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(moduleFiles[i]);
                if (addedModules.Contains(fileNameWithoutExtension))
                    continue;
                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<MonoScript>(moduleFiles[i]),
                        typeof(MonoScript), false);
                    GUI.enabled = true;

                    if (targetScript.tabConfigs.Count > 0)
                    {
                        GUILayout.Label("添加到Tab：", GUILayout.ExpandWidth(false));
                        for (int j = 0; j < targetScript.tabConfigs.Count; j++)
                        {
                            if (GUILayout.Button($"{j}", GUILayout.ExpandWidth(false)))
                            {
                                targetScript.tabConfigs[j].moduleList
                                    .Add(AssetDatabase.LoadAssetAtPath<MonoScript>(moduleFiles[i]));
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }


            GUILayout.Space(20);

            #region Button

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("清空配置"))
                {
                    targetScript.tabConfigs.Clear();
                    PluginHubWindow.RestartWindow();
                }
                if (GUILayout.Button("载入最小模块配置"))
                {
                    MakeMinimalModuleConfig();
                    PluginHubWindow.RestartWindow();
                }
                if (GUILayout.Button("载入默认模块配置"))
                {
                    MakeDefaultModuleConfig();
                    PluginHubWindow.RestartWindow();
                }
            }
            GUILayout.EndHorizontal();


            if (GUILayout.Button("重启PluginHub", GUILayout.Height(30)))
            {
                PluginHubWindow.RestartWindow();
            }

            #endregion
        }

        private void MakeMinimalModuleConfig()
        {
            targetScript.tabConfigs.Clear();

            targetScript.tabConfigs.Add(new ModuleTabConfig()
            {
                tabName = "快捷方式",
                moduleList = new List<MonoScript>()
                {
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}NavigationBarModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}SelectedModule.cs"),
                    AssetDatabase.LoadAssetAtPath<MonoScript>($"{moduleFolder}SceneModule.cs"),
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