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
    // 快捷打开  -300
    // --------------
    // 不常用的工具和命令  -100
    // --------------
    // 由模块代码添加的菜单  0
    // --------------
    // 带开关的功能 100
    public class PHTopMenu
    {
        #region 用于菜单标题，组织和分类分隔作用
        [MenuItem("PluginHub/快捷打开", false, -301)]
        public static void Shortcut() { }
        [MenuItem("PluginHub/快捷打开", true, -301)]
        public static bool ShortcutValid() => false;
        [MenuItem("PluginHub/不常用的工具和命令", false, -101)]
        public static void Separator() { }
        [MenuItem("PluginHub/不常用的工具和命令", true, -101)]
        public static bool SeparatorValid() => false;
        [MenuItem("PluginHub/由模块代码添加的菜单", false, -1)]
        public static void ModuleMenu() { }
        [MenuItem("PluginHub/由模块代码添加的菜单", true, -1)]
        public static bool ModuleMenuValid() => false;
        [MenuItem("PluginHub/带开关的功能", false, 99)]
        public static void SwitchMenu() { }
        [MenuItem("PluginHub/带开关的功能", true, 99)]
        public static bool SwitchMenuValid() => false;
        #endregion



        #region 快捷打开 Folder/Window

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

        [MenuItem("PluginHub/Open/Folder StreamingAssets", false, -300)]
        public static void OpenFolderStreamingAssets()
        {
            OpenFolder(Application.streamingAssetsPath);
        }
        [MenuItem("PluginHub/Open/Folder PersistentDataPath", false, -299)]
        public static void OpenFolderPersistentDataPath()
        {
            OpenFolder(Application.persistentDataPath);
        }
        [MenuItem("PluginHub/Open/Folder DataPath", false, -298)]
        public static void OpenFolderDataPath()
        {
            OpenFolder(Application.dataPath);
        }
        //--------------------------
        [MenuItem("PluginHub/Open/Folder Build", false, -281)]
        public static void OpenFolderBuild()
        {
            OpenFolder(_projectRootPath + "/Build/");
        }
        [MenuItem("PluginHub/Open/Folder Recordings", false, -280)]
        public static void OpenFolderRecordings()
        {
            OpenFolder(_projectRootPath + "/Recordings/");
        }
        [MenuItem("PluginHub/Open/Folder ExternalAssets", false, -279)]
        public static void OpenFolderExternalAssets()
        {
            // 这个文件夹非标准Unity文件夹，是个人习惯用于放置项目相关的外部资源，例如参考图，策划文档等。
            OpenFolder(_projectRootPath + "/ExternalAssets/");
        }
        //--------------------------
        //--------------------------
        [MenuItem("PluginHub/Open/Window Project Settings...", false, -260)]
        public static void OpenWindowProjectSettings()
        {
            EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
        }
        [MenuItem("PluginHub/Open/Window Package Manager", false, -259)]
        public static void OpenWindowPackageManager()
        {
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }
        [MenuItem("PluginHub/Open/Window Preferences...", false, -258)]
        public static void OpenWindowPreferences()
        {
            EditorApplication.ExecuteMenuItem("Edit/Preferences...");
        }
        //--------------------------
        [MenuItem("PluginHub/Open/Window Animation", false, -240)]
        public static void OpenWindowAnimation()
        {
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
        }
        [MenuItem("PluginHub/Open/Window Timeline", false, -239)]
        public static void OpenWindowTimeline()
        {
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
        }
        //--------------------------
        [MenuItem("PluginHub/Open/Window Lighting", false, -227)]
        public static void OpenWindowLighting()
        {
            EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
        }
        [MenuItem("PluginHub/Open/Window Light Explorer", false, -226)]
        public static void OpenWindowLightExplorer()
        {
            EditorApplication.ExecuteMenuItem("Window/Rendering/Light Explorer");
        }
        [MenuItem("PluginHub/Open/Window UV Inspector", false, -225)]
        public static void OpenWindowUVInspector()
        {
            EditorApplication.ExecuteMenuItem("Window/nTools/UV Inspector");
        }
        //--------------------------
        [MenuItem("PluginHub/Open/Window Test Runner", false, -213)]
        public static void OpenWindowTestRunner()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }
        #endregion

        #region 不常用的工具和命令

        [MenuItem("PluginHub/Scene View Screenshot", false, -99)]
        public static void SceneViewScreenshot()
        {
            SceneGameScreenShot.ScreenShotSceneView();
        }
        [MenuItem("PluginHub/Game View Screenshot", false, -98)]
        public static void GameViewScreenshot()
        {
            SceneGameScreenShot.ScreenShotGameView();
        }

        [MenuItem("PluginHub/Switch(Clear) Console &#c", false, -97)]
        public static void SwitchClearConsole()
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






        [MenuItem("PluginHub/创建项目基本目录", false, -96)]
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

        [MenuItem("PluginHub/添加MeshCollider到选中物体(递归)", false, -95)]
        public static void AddMeshColliderToSelected()
        {
            foreach (var obj in Selection.gameObjects)
                AddMeshColliderRecursively(obj.transform);
        }

        // 递归
        private static void AddMeshColliderRecursively(Transform transform)
        {
            MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();
            if (meshRenderer != null && transform.GetComponent<MeshCollider>() == null)
                transform.gameObject.AddComponent<MeshCollider>();
            foreach (Transform child in transform)
                AddMeshColliderRecursively(child);
        }

        #endregion

    }
}