using System.Collections;
using System.Collections.Generic;
using PluginHub;
using PluginHub.Helper;
using PluginHub.Module;
using UnityEditor;
using UnityEngine;

public static class PluginHubMenu
{

    private const string MenuPrefix = "PluginHub/";

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
}
