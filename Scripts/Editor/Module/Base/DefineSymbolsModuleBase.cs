using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PluginHub.Editor
{
    // 在模块基类基础上添加脚本宏定义功能, 用于动态添加宏定义以动态添加一些功能.
    // 继承自该类的模块可以添加自定义宏,自定宏可以在 PlayerSettings->ScriptDefineSymbols 中查看
    public abstract class DefineSymbolsModuleBase : PluginHubModuleBase
    {
        public override string moduleDescription => "支持动态添加脚本宏定义的模块基类";
        // 表示该模块的自定宏符号名称,例:PH_HierachyExtension, 会添加到 PlayerSettings->ScriptDefineSymbols 中
        public abstract string baseSymbolName { get; }
        //菜单前缀
        protected const string MenuPrefix = "PluginHub/";


        private GUIContent _guiContent = new GUIContent("启用 DefineSymbol", "启用基础宏");

        protected override void DrawGuiContent()
        {
            EditorGUILayout.HelpBox($"该模块具有编辑宏功能,能够动态添加一些功能.下面的开关控制 {baseSymbolName} DefineSymbol的启用", MessageType.Info);
            enableBaseSymbols = EditorGUILayout.Toggle(_guiContent, enableBaseSymbols);
            GUILayout.Space(10);
        }

        //是否启用基础宏.每个继承自该类的模块都会有一个基础宏,用于控制基础功能是否启用
        //也可以添加更多的宏,用于控制更多的功能,请依照该属性的写法添加即可
        protected bool enableBaseSymbols
        {
            get => ScriptingDefineSymbolsState(out _, baseSymbolName);
            set
            {
                if (value)//设置的意图是启用
                {
                    if(ScriptingDefineSymbolsState(out string[] outputStr, baseSymbolName))
                        return;
                    AddScriptingDefineSymbolsToPlayerSettings(outputStr, baseSymbolName);
                }
                else//设置的意图是禁用
                {
                    if(!ScriptingDefineSymbolsState(out string[] outputStr, baseSymbolName))
                        return;
                    RemoveScriptingDefineSymbolsFromPlayerSettings(outputStr, baseSymbolName);
                }
            }
        }

        //从 PlayerSettings 中移除 symbolToRemove 宏
        protected void RemoveScriptingDefineSymbolsFromPlayerSettings(string[] oldSymbols, string symbolToRemove)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            string newDefineSymbols = string.Empty;
            for (int i = 0; i < oldSymbols.Length; i++)
            {
                if (oldSymbols[i] == symbolToRemove)
                    continue;
                newDefineSymbols += oldSymbols[i] + ";";
            }

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefineSymbols);
        }

        //添加 symbolToAdd 宏到 PlayerSettings
        protected void AddScriptingDefineSymbolsToPlayerSettings(string[] oldSymbols, string symbolToAdd)
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

        //位置在PlayerSettings->Other Settings->ScriptingDefineSymbols
        //获取当前有哪些宏，返回是否有 symbol2Find 宏
        protected bool ScriptingDefineSymbolsState(out string[] outputStr, string symbol2Find)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out outputStr);
            bool hasPHSymbols = false;//是否有PH的宏
            for (int i = 0; i < outputStr.Length; i++)
            {
                if (outputStr[i] == symbol2Find)
                {
                    hasPHSymbols = true;
                    break;
                }
            }
            return hasPHSymbols;
        }
    }
}