using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using MenuItem = UnityEditor.MenuItem;
using Screen = UnityEngine.Screen;

namespace PluginHub.Editor
{
    // PluginHub 顶部菜单
    //
    // 优先级规划：
    // Shotcut  < -200
    // --------------
    // Command  -100
    // --------------
    // Module Menu  0
    // --------------
    // Tools Helper Toggle  100
    public class PHTopMenu
    {
        #region Shortcut

        //Open Folder Window/StreamingAssets
        //Open Folder Window/PersistentDataPath
        //Open Folder Window/DataPath
        //Open Folder Window/-----------
        //Open Folder Window/Build
        //Open Folder Window/Recordings
        //Open Folder Window/ExternalAssets

        //Open Folder Window/Project Settings...
        //Open Folder Window/Package Manager
        //Open Folder Window/Preferences...
        //Open Folder Window/-----------
        //Open Folder Window/Animation
        //Open Folder Window/Timeline
        //Open Folder Window/-----------
        //Open Folder Window/Lighting
        //Open Folder Window/Light Explorer
        //Open Folder Window/UV Inspector
        //Open Folder Window/-----------
        //Open Folder Window/Test Runner


        #region Open Folder/Window

        private static string _projectRootPath{ get { return Application.dataPath.Replace("/Assets", ""); } }
        private static void OpenFolder(string path)
        {
            Debug.Log("Open Folder: " + path);
            if (!Directory.Exists(path))
            {
                Debug.Log("Folder not exist, create it.");
                Directory.CreateDirectory(path);
            }
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("PluginHub/Open Folder Window/Folder StreamingAssets", false, -300)]
        public static void OpenFolderStreamingAssets()
        {
            OpenFolder(Application.streamingAssetsPath);
        }
        [MenuItem("PluginHub/Open Folder Window/Folder PersistentDataPath", false, -299)]
        public static void OpenFolderPersistentDataPath()
        {
            OpenFolder(Application.persistentDataPath);
        }
        [MenuItem("PluginHub/Open Folder Window/Folder DataPath", false, -298)]
        public static void OpenFolderDataPath()
        {
            OpenFolder(Application.dataPath);
        }
        //--------------------------
        [MenuItem("PluginHub/Open Folder Window/Folder Build", false, -281)]
        public static void OpenFolderBuild()
        {
            OpenFolder(_projectRootPath + "/Build/");
        }
        [MenuItem("PluginHub/Open Folder Window/Folder Recordings", false, -280)]
        public static void OpenFolderRecordings()
        {
            OpenFolder(_projectRootPath + "/Recordings/");
        }
        [MenuItem("PluginHub/Open Folder Window/Folder ExternalAssets", false, -279)]
        public static void OpenFolderExternalAssets()
        {
            // 这个文件夹非标准Unity文件夹，是个人习惯用于放置项目相关的外部资源，例如参考图，策划文档等。
            OpenFolder(_projectRootPath + "/ExternalAssets/");
        }
        //--------------------------
        //--------------------------
        [MenuItem("PluginHub/Open Folder Window/Window Project Settings...", false, -260)]
        public static void OpenWindowProjectSettings()
        {
            EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
        }
        [MenuItem("PluginHub/Open Folder Window/Window Package Manager", false, -259)]
        public static void OpenWindowPackageManager()
        {
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }
        [MenuItem("PluginHub/Open Folder Window/Window Preferences...", false, -258)]
        public static void OpenWindowPreferences()
        {
            EditorApplication.ExecuteMenuItem("Edit/Preferences...");
        }
        //--------------------------
        [MenuItem("PluginHub/Open Folder Window/Window Animation", false, -240)]
        public static void OpenWindowAnimation()
        {
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
        }
        [MenuItem("PluginHub/Open Folder Window/Window Timeline", false, -239)]
        public static void OpenWindowTimeline()
        {
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
        }
        //--------------------------
        [MenuItem("PluginHub/Open Folder Window/Window Lighting", false, -227)]
        public static void OpenWindowLighting()
        {
            EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
        }
        [MenuItem("PluginHub/Open Folder Window/Window Light Explorer", false, -226)]
        public static void OpenWindowLightExplorer()
        {
            EditorApplication.ExecuteMenuItem("Window/Rendering/Light Explorer");
        }
        [MenuItem("PluginHub/Open Folder Window/Window UV Inspector", false, -225)]
        public static void OpenWindowUVInspector()
        {
            EditorApplication.ExecuteMenuItem("Window/nTools/UV Inspector");
        }
        //--------------------------
        [MenuItem("PluginHub/Open Folder Window/Window Test Runner", false, -213)]
        public static void OpenWindowTestRunner()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }
        #endregion

        #region 截图
        [MenuItem("PluginHub/Scene View Screenshot", false, -210)]
        public static void SceneViewScreenshot()
        {
            SceneGameScreenShot.ScreenShotSceneView();
        }
        [MenuItem("PluginHub/Game View Screenshot", false, -209)]
        public static void GameViewScreenshot()
        {
            SceneGameScreenShot.ScreenShotGameView();
        }
        #endregion



        [MenuItem("PluginHub/Shortcut/Switch Console &#c", false, -200)]
        public static void SwitchConsole()
        {
#if UNITY_2021_1_OR_NEWER
            //当console窗口docked时，是清空console，否则是切换显示或隐藏
            Assembly assembly = Assembly.GetAssembly(typeof(EditorWindow));
            Type type = assembly.GetType("UnityEditor.ConsoleWindow");
            EditorWindow window = EditorWindow.GetWindow(type);
            type = assembly.GetType("UnityEditor.LogEntries");
            MethodInfo clearMethod = type.GetMethod("Clear");
            if (window.docked)
            {
                clearMethod.Invoke(window, null);
            }
            else
            {
                if (window.position.x == 0)
                    window.Show();
                else
                    window.Close();
            }
#else
            Assembly assembly = Assembly.GetAssembly(typeof(EditorWindow));
            Type type = assembly.GetType("UnityEditor.ConsoleWindow");
            EditorWindow window = EditorWindow.GetWindow(type);
            type = assembly.GetType("UnityEditor.LogEntries");
            MethodInfo method = type.GetMethod("Clear");
            method.Invoke(window, null);//清空控制台
#endif
        }


        #endregion


        [MenuItem("PluginHub/创建项目基本目录", false, -100)]
        public static void CreateProjectBaseDir()
        {
            Debug.Log("创建项目基本目录");
            if (!AssetDatabase.IsValidFolder("Assets/00.Addons"))
                AssetDatabase.CreateFolder("Assets", "00.Addons");
            if (!AssetDatabase.IsValidFolder("Assets/01.Scenes"))
                AssetDatabase.CreateFolder("Assets", "01.Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/02.UI"))
                AssetDatabase.CreateFolder("Assets", "02.UI");
            if (!AssetDatabase.IsValidFolder("Assets/03.Art"))
                AssetDatabase.CreateFolder("Assets", "03.Art");
            if (!AssetDatabase.IsValidFolder("Assets/03.Art/Materials"))
                AssetDatabase.CreateFolder("Assets/03.Art", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/03.Art/Materials/SimpleColor"))
                AssetDatabase.CreateFolder("Assets/03.Art/Materials", "SimpleColor");
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Gray.mat", Color.gray);
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_White.mat", Color.white);
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Black.mat", Color.black);
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Red.mat", Color.HSVToRGB(0, .72f, .72f));
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Green.mat",
                Color.HSVToRGB(1f / 3, .72f, .72f));
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Blue.mat",
                Color.HSVToRGB(2f / 3, .72f, .72f));
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Yellow.mat",
                Color.HSVToRGB(1f / 6, .72f, .72f));
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Cyan.mat",
                Color.HSVToRGB(175f / 360, .72f, .72f));
            CreateMaterial("Assets/03.Art/Materials/SimpleColor/M_SimpleColor_Magenta.mat",
                Color.HSVToRGB(5f / 6, .72f, .72f));

            if (!AssetDatabase.IsValidFolder("Assets/03.Art/Shaders"))
                AssetDatabase.CreateFolder("Assets/03.Art", "Shaders");
            if (!AssetDatabase.IsValidFolder("Assets/03.Art/Textures"))
                AssetDatabase.CreateFolder("Assets/03.Art", "Textures");
            if (!AssetDatabase.IsValidFolder("Assets/03.Art/Meshes"))
                AssetDatabase.CreateFolder("Assets/03.Art", "Meshes");
            if (!AssetDatabase.IsValidFolder("Assets/04.Scripts"))
                AssetDatabase.CreateFolder("Assets", "04.Scripts");
        }

        private static void CreateMaterial(string path, Color color)
        {
            // create material use default shader
            AssetDatabase.CreateAsset(new Material(GetAppropriateDefaultShader()), path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            material.color = color;
            //set smoothness in URP
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.0f);
            //set smoothness in buildin pipeline
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.0f);
        }

        private static Shader GetAppropriateDefaultShader()
        {
            //is URP?
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                //is HDRP?
                if (GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("HDRenderPipelineAsset"))
                {
                    return Shader.Find("HDRP/Lit");
                }
                else
                {
                    return Shader.Find("Universal Render Pipeline/Lit");
                }
            }
            else
            {
                return Shader.Find("Standard");
            }
        }



    }
}