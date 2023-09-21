using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PluginHub;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using PluginHub.Helper;

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
            float height = 19;

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.Icon("Settings","Project Settings"),GUILayout.Width(width3), GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                }
                if (GUILayout.Button(PluginHubFunc.Icon("Settings","Preferences"),GUILayout.Width(width3), GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Edit/Preferences...");
                }
                if (GUILayout.Button(PluginHubFunc.Icon("Package Manager","Package Manager"),GUILayout.Width(width3), GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Window/Package Manager");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.Icon("UnityEditor.AnimationWindow","Animation"),GUILayout.Width(width2), GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
                }
                if (GUILayout.Button(PluginHubFunc.Icon("UnityEditor.Timeline.TimelineWindow","Timeline"),GUILayout.Width(width2), GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.Icon("UpArrow", "文件夹向上一层"), GUILayout.Width(width3), GUILayout.Height(height)))
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
                        GUILayout.Width(width3), GUILayout.Height(height)))
                {
                    GameObject[] s = Selection.gameObjects;
                    PluginHubFunc.CollapseGameObjects();
                    PluginHubFunc.CollapseGameObjects();
                    Selection.objects = s;
                }

                if (GUILayout.Button(PluginHubFunc.Icon("d_Project", "Collapse Folder"), GUILayout.Width(width3),
                        GUILayout.Height(height)))
                {
                    PluginHubFunc.CollapseFolders();
                    PluginHubFunc.CollapseFolders();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(PluginHubFunc.Icon("d_PositionAsUV1 Icon", "UVInspector"), GUILayout.Width(width3),
                        GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Window/nTools/UV Inspector");
                }

                if (GUILayout.Button(PluginHubFunc.Icon("d_Lighting@2x", "Light Explorer"), GUILayout.Width(width3),
                        GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Window/Rendering/Light Explorer");
                }

                if (GUILayout.Button(PluginHubFunc.Icon("d_Lighting@2x", "Lighting"), GUILayout.Width(width3),
                        GUILayout.Height(height)))
                {
                    EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
                }
            }
            GUILayout.EndHorizontal();


            if (GUILayout.Button(PluginHubFunc.Icon("LightmapParameters On Icon", " 转到 LightmapParameters"),
                    GUILayout.Width(width1), GUILayout.Height(19)))
            {
#if UNITY_2020_1_OR_NEWER
                Selection.objects = new Object[]
                    { LightmapParameters.GetLightmapParametersForLightingSettings(Lightmapping.lightingSettings) };
                //打开inspector面板
                EditorApplication.ExecuteMenuItem("Window/Panels/6 Inspector");
#endif
            }

            if (GUILayout.Button(PluginHubFunc.Icon("d_PlayButton", " Bake Lighting"), GUILayout.Width(width1),
                    GUILayout.Height(30)))
            {
                Lightmapping.BakeAsync();
            }
        }

    }
}