using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using PluginHub;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using PluginHub.Helper;
using Debug = UnityEngine.Debug;

namespace PluginHub.Module
{
    public class NavigationBarModule : PluginHubModuleBase
    {
        public override string moduleName
        {
            get { return "常用/导航"; }
        }

        private string lastSelectionGUID;

        protected override void DrawGuiContent()
        {
            float width3 = PluginHubWindow.Window.CaculateButtonWidth(3);
            float width2 = PluginHubWindow.Window.CaculateButtonWidth(2);
            float width1 = PluginHubWindow.Window.CaculateButtonWidth(1);
            float height_nor = 19;
            GUIStyle labelCenter = new GUIStyle(PluginHubFunc.PHGUISkin.label);
            labelCenter.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("快捷导航：",labelCenter);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(PluginHubFunc.Icon("Settings","Project Settings"),GUILayout.Width(width3), GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                    }
                    if (GUILayout.Button(PluginHubFunc.Icon("Settings","Preferences"),GUILayout.Width(width3), GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Edit/Preferences...");
                    }
                    if (GUILayout.Button(PluginHubFunc.Icon("Package Manager","Package Manager"),GUILayout.Width(width3), GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Package Manager");
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(PluginHubFunc.Icon("UnityEditor.AnimationWindow","Animation"),GUILayout.Width(width2), GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
                    }
                    if (GUILayout.Button(PluginHubFunc.Icon("UnityEditor.Timeline.TimelineWindow","Timeline"),GUILayout.Width(width2), GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(PluginHubFunc.Icon("UpArrow", "文件夹向上一层"), GUILayout.Width(width3), GUILayout.Height(height_nor)))
                    {
                        //向上一层: Project视图向上导航一层文件夹
                        string currAssetPath = "";
                        string selectionGUID = "";

                        if (Selection.assetGUIDs.Length > 0)
                            selectionGUID = Selection.assetGUIDs[0];

                        if (string.IsNullOrWhiteSpace(selectionGUID))
                            selectionGUID = lastSelectionGUID;

                        currAssetPath = AssetDatabase.GUIDToAssetPath(selectionGUID);

                        // Debug.Log(currAssetPath);
                        if (!string.IsNullOrWhiteSpace(currAssetPath) && !currAssetPath.Equals("Assets"))
                        {
                            string upperPath = Path.GetDirectoryName(currAssetPath);
                            // Debug.Log(upperPath);
                            // Check the path has no '/' at the end, if it dose remove it,
                            if (upperPath[upperPath.Length - 1] == '/')
                                upperPath = upperPath.Substring(0, upperPath.Length - 1);
                            // Load object  文件夹也是一个object
                            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(upperPath, typeof(UnityEngine.Object));
                            // Select the object in the project folder
                            Selection.activeObject = obj;
                        }
                    }

                    if (GUILayout.Button(PluginHubFunc.Icon("UnityEditor.SceneHierarchyWindow@2x", "Collapse Hierarchy"),
                            GUILayout.Width(width3), GUILayout.Height(height_nor)))
                    {
                        GameObject[] s = Selection.gameObjects;
                        PluginHubFunc.CollapseGameObjects();
                        PluginHubFunc.CollapseGameObjects();
                        Selection.objects = s;
                    }

                    if (GUILayout.Button(PluginHubFunc.Icon("d_Project", "Collapse Folder"), GUILayout.Width(width3),
                            GUILayout.Height(height_nor)))
                    {
                        PluginHubFunc.CollapseFolders();
                        PluginHubFunc.CollapseFolders();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(PluginHubFunc.Icon("d_PositionAsUV1 Icon", "UVInspector"), GUILayout.Width(width3),
                            GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Window/nTools/UV Inspector");
                    }

                    if (GUILayout.Button(PluginHubFunc.Icon("d_Lighting@2x", "Light Explorer"), GUILayout.Width(width3),
                            GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Rendering/Light Explorer");
                    }

                    if (GUILayout.Button(PluginHubFunc.Icon("d_Lighting@2x", "Lighting"), GUILayout.Width(width3),
                            GUILayout.Height(height_nor)))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
                    }
                }
                GUILayout.EndHorizontal();


                if (GUILayout.Button(PluginHubFunc.Icon("LightmapParameters On Icon", " 转到 LightmapParameters"),
                        GUILayout.Width(width1), GUILayout.Height(height_nor)))
                {
    #if UNITY_2020_1_OR_NEWER
                    Selection.objects = new Object[]
                        { LightmapParameters.GetLightmapParametersForLightingSettings(Lightmapping.lightingSettings) };
                    //打开inspector面板
                    EditorApplication.ExecuteMenuItem("Window/Panels/6 Inspector");
    #endif
                }

                if (GUILayout.Button(PluginHubFunc.Icon("d_PlayButton", "Bake Lighting"), GUILayout.Width(width1),
                        GUILayout.Height(30)))
                {
                    Lightmapping.BakeAsync();
                }

                }
            GUILayout.EndVertical();


            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("快捷打开文件夹：",labelCenter);

                GUILayout.Label(PluginHubFunc.GuiContent("系统文件夹：","Unity引擎自带的文件夹或者特殊文件夹"));
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        if (GUILayout.Button("StreamingAssets",GUILayout.Width(width2)))
                        {
                            OpenFileExplorer(Application.streamingAssetsPath);
                        }
                        if (GUILayout.Button("PersistentDataPath",GUILayout.Width(width2)))
                        {
                            OpenFileExplorer(Application.persistentDataPath);
                        }
                        if (GUILayout.Button("DataPath",GUILayout.Width(width2)))
                        {
                            OpenFileExplorer(Application.dataPath);
                        }
                    }
                    GUILayout.EndVertical();


                    GUILayout.BeginVertical();
                    {
                        if (GUILayout.Button("Packages",GUILayout.Width(width2)))
                        {
                            OpenFileExplorer(Path.Combine(Application.dataPath, "../Packages"));
                        }
                        if (GUILayout.Button("ProjectSettings",GUILayout.Width(width2)))
                        {
                            OpenFileExplorer(Path.Combine(Application.dataPath, "../ProjectSettings"));
                        }
                        if (GUILayout.Button("Logs",GUILayout.Width(width2)))
                        {
                            OpenFileExplorer(Path.Combine(Application.dataPath, "../Logs"));
                        }
                    }
                    GUILayout.EndVertical();

                }
                GUILayout.EndHorizontal();


                GUILayout.Label(PluginHubFunc.GuiContent("自定文件夹：","根据个人使用习惯设置的项目文件夹"));
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Build",GUILayout.Width(width3)))
                    {
                        OpenFileExplorer(Path.Combine(Application.dataPath, "../Build"));
                    }
                    if (GUILayout.Button("Recordings",GUILayout.Width(width3)))
                    {
                        OpenFileExplorer(Path.Combine(Application.dataPath, "../Recordings"));
                    }
                    if (GUILayout.Button("ExternalAssets",GUILayout.Width(width3)))
                    {
                        OpenFileExplorer(Path.Combine(Application.dataPath, "../ExternalAssets"));
                    }

                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public static void OpenFileExplorer(string path, bool isCreate = true)
        {
            if (!Directory.Exists(path))
            {
                //如果目录不存在 创建之
                if (isCreate)
                {
                    Debug.Log($"Make dir {path}");
                    Directory.CreateDirectory(path);
                }
                else
                {
                    Debug.LogWarning(path + " not exist");
                    return;
                }
            }
            string args = $"/Select, {path}";
            args = args.Replace("/","\\");
            ProcessStartInfo pfi=new ProcessStartInfo("Explorer.exe",args);
            Process.Start(pfi);
            //该代码也是打开文件管理器，但是打开的目录在指向目录的上一层，稳定就行，不要随意换。
            //不支持D:\UnityProject\TopWellCustomPattern\Assets\..\Build\ 这种带..的路径
            //EditorUtility.RevealInFinder(path);
        }

    }
}