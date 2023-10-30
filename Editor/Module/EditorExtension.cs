using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PluginHub.Module
{
    //该模块比较特殊，他会为编辑器添加菜单以增强编辑器的功能
    //菜单使用预编译指令实现动态添加
    public class EditorExtension : PluginHubModuleBase
    {
        public override string moduleName { get { return "菜单 / 编辑器增强"; } }

        //菜单前缀
        private const string MenuPrefix = "PluginHub/";
        //预编译指令名称
        private const string PH_EditorExtension = "PH_EditorExtension";

        //每个项目第一次启动时，会默认启用编辑器增强
        private bool isFirstTime
        {
            get { return EditorPrefs.GetBool(PluginHubFunc.ProjectUniquePrefix + "EditorExtension_isFirstTime", true); }
            set { EditorPrefs.SetBool(PluginHubFunc.ProjectUniquePrefix + "EditorExtension_isFirstTime", value); }
        }

        //是否启用编辑器增强
        private bool enableEditorExtension {

            get
            {
                bool hasPHSymbols = ScriptingDefineSymbolsState(out string[] outputStr);
                return hasPHSymbols;
            }
            set
            {
                if (value)//设置的意图是启用
                {
                    bool hasPHSymbols = ScriptingDefineSymbolsState(out string[] outputStr);
                    if(hasPHSymbols)
                        return;
                    AddPHScriptingDefineSymbolsToPlayerSettings(outputStr, PH_EditorExtension);
                }
                else//设置的意图是禁用
                {
                    bool hasPHSymbols = ScriptingDefineSymbolsState(out string[] outputStr);
                    if(!hasPHSymbols)
                        return;
                    RemovePHScriptingDefineSymbolsFromPlayerSettings(outputStr);
                }
            }
        }

        public override void OnInitOnload()
        {
            base.OnInitOnload();
            if (isFirstTime)
            {
                isFirstTime = false;
                enableEditorExtension = true;
            }
        }

        private void RemovePHScriptingDefineSymbolsFromPlayerSettings(string[] oldSymbols)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            string newDefineSymbols = string.Empty;
            for (int i = 0; i < oldSymbols.Length; i++)
            {
                if (oldSymbols[i] == PH_EditorExtension)
                    continue;
                newDefineSymbols += oldSymbols[i] + ";";
            }

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefineSymbols);
        }

        private void AddPHScriptingDefineSymbolsToPlayerSettings(string[] oldSymbols, string symbolToAdd)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            string newDefineSymbols = string.Empty;
            for (int i = 0; i < oldSymbols.Length; i++)
            {
                if (oldSymbols[i] == symbolToAdd) //已经有了
                    return;
                newDefineSymbols += oldSymbols[i] + ";";
            }
            newDefineSymbols += symbolToAdd;

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefineSymbols);
        }

        //获取当前有哪些宏，返回是否有PH的宏
        //位置在PlayerSettings->Other Settings->ScriptingDefineSymbols
        private bool ScriptingDefineSymbolsState(out string[] outputStr)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out outputStr);
            bool hasPHSymbols = false;//是否有PH的宏
            for (int i = 0; i < outputStr.Length; i++)
            {
                if (outputStr[i] == PH_EditorExtension)
                {
                    hasPHSymbols = true;
                    break;
                }
            }
            return hasPHSymbols;
        }

        protected override void DrawGuiContent()
        {
            EditorGUILayout.HelpBox("该模块比较特殊，他会为编辑器添加菜单以增强编辑器的功能。", MessageType.Info);
            enableEditorExtension = EditorGUILayout.Toggle("启用 PluginHub 编辑器增强", enableEditorExtension);
        }

#if PH_EditorExtension

        //折叠层级视图
        [MenuItem("GameObject/折叠层级视图", false, -51)]
        public static void CollapseHierarchy()
        {
            GameObject[] s = Selection.gameObjects;
            PluginHubFunc.CollapseGameObjects();
            PluginHubFunc.CollapseGameObjects();
            Selection.objects = s;
        }

        #region Camera模式菜单
        //Alt+S
        [MenuItem(MenuPrefix + "Shortcut/切换最近相机模式 &S", false, 1)]
        public static void SwitchBetweenNormalLightmap()
        {
            CameraShowModeModule.SwitchRecentCameraModeShotcut(SceneView.lastActiveSceneView.cameraMode.drawMode);
        }

        //Alt+1
        [MenuItem(MenuPrefix + "Shortcut/相机模式-Shaded &1", false, 2)]
        public static void ChangeToNormalCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Textured);
        }

        //Alt+2
        [MenuItem(MenuPrefix + "Shortcut/相机模式-Wireframe &2", false, 3)]
        public static void ChangeToWireframeCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Wireframe);
        }

        //Alt+3
        [MenuItem(MenuPrefix + "Shortcut/相机模式-ShadedWireframe &3", false, 4)]
        public static void ChangeToShadedWireframeCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.TexturedWire);
        }

        //Alt+4
        [MenuItem(MenuPrefix + "Shortcut/相机模式-BakedLightmap &4", false, 5)]
        public static void ChangeToBakedLightmapCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.BakedLightmap);
        }
        #endregion



#endif

    }
}