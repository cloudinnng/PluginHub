using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PluginHub.Helper;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class CameraShowModeModule : DefineSymbolsModuleBase
    {
        public override ModuleType moduleType => ModuleType.Shortcut;
        public override string moduleName
        {
            get { return "场景相机着色模式"; }
        }

        public override string moduleDescription => "一键切换场景视图相机的着色模式,方便美术师查看";
        public override string baseSymbolName => "PH_CameraShowModeShorcut";

        DrawCameraMode[] commonCameraModes = new DrawCameraMode[]
        {
            DrawCameraMode.Textured, DrawCameraMode.Wireframe, DrawCameraMode.TexturedWire,
            DrawCameraMode.DeferredDiffuse, DrawCameraMode.DeferredSmoothness, DrawCameraMode.LightOverlap,
            DrawCameraMode.BakedLightmap, DrawCameraMode.BakedUVOverlap, DrawCameraMode.BakedTexelValidity,
        }; //常用的相机模式

        private string[] cameraModeText; //相机模式文本数组  用于显示
        private bool showAllCameraMode = false;
        private static List<DrawCameraMode> recentCameraMode = new List<DrawCameraMode>(); //存储最近的相机模式

        protected override void DrawGuiContent()
        {
            // base.DrawGuiContent();
            enableBaseSymbols = EditorGUILayout.Toggle("启用快捷键", enableBaseSymbols);

            GUILayout.BeginVertical("Box");
            {
                DrawCameraMode currMode = SceneView.lastActiveSceneView.cameraMode.drawMode;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"选择相机模式：\n当前模式：{currMode}");

                    if (GUILayout.Button(PluginHubFunc.GuiContent("N/L", "快速在标准/光图视图模式之间切换"), GUILayout.Width(44),
                            GUILayout.Height(36)))
                    {
                        if (SceneView.lastActiveSceneView.cameraMode.drawMode == DrawCameraMode.BakedLightmap)
                            ChangeDrawCameraMode(DrawCameraMode.Textured);
                        else
                            ChangeDrawCameraMode(DrawCameraMode.BakedLightmap);
                    }

                    //刷新Icon  切换最近的两个相机模式
                    if (GUILayout.Button(PluginHubFunc.Icon("d_RotateTool On@2x", "", "切换显示最近两个相机模式"), GUILayout.Width(44),
                            GUILayout.Height(36)))
                    {
                        SwitchRecentCameraModeShotcut(currMode);
                    }
                }
                GUILayout.EndHorizontal();

                //get select index
                // int selectIndex = -1;
                // for (int i = 0; i < commonCameraModes.Length; i++)
                // {
                //     if (currMode == commonCameraModes[i])
                //     {
                //         selectIndex = i;
                //         break;
                //     }
                // }

                //画常用的相机模式
                // GUIStyle s = new GUIStyle(GUI.skin.button);
                // float w = editorWindow.position.width / 3f - 12;
                // s.fixedWidth = w;

                // GUILayout.SelectionGrid()

                float width3 = PluginHubWindow.Window.CaculateButtonWidth(3);
                for (int i = 0; i < commonCameraModes.Length; i++)
                {
                    DrawCameraMode drawCameraMode = commonCameraModes[i];

                    if (PluginHubFunc.ShouldBeginHorizontal(i, 3))
                        GUILayout.BeginHorizontal();

                    Color oldColor = GUI.color;
                    if (CurrDrawCameraModeIs(commonCameraModes[i]))
                        GUI.color = PluginHubFunc.SelectedColor;
                    if (GUILayout.Button(drawCameraMode.ToString(), GUILayout.Width(width3)))
                    {
                        //切换相机模式
                        DrawCameraMode mode = commonCameraModes[i];
                        ChangeDrawCameraMode(mode);
                    }

                    GUI.color = oldColor;

                    if (PluginHubFunc.ShouldEndHorizontal(i, 3))
                        GUILayout.EndHorizontal();

                }

                //相机模式的提示
                if (CurrDrawCameraModeIs(DrawCameraMode.BakedTexelValidity))
                {
                    GUILayout.TextArea(
                        "提示：\n当前相机模式为BakedTexelValidity，该模式下红色表示不进行GI贡献的纹素。将背面公差属性设为0关闭该功能，设为1则会凡是遇到1条光线击中几何体背面就会丢弃该纹素的GI贡献 ");
                }

                if (showAllCameraMode = EditorGUILayout.Foldout(showAllCameraMode, "显示所有相机模式"))
                {
                    //绘制所有相机模式选择按钮
                    if (cameraModeText == null)
                    {
                        cameraModeText = new string[36];
                        for (int i = -1; i < 35; i++)
                        {
                            cameraModeText[i + 1] = ((DrawCameraMode)i).ToString();
                        }
                    }

                    // selectIndex = (int) SceneView.lastActiveSceneView.cameraMode.drawMode;
                    // selectIndex += 1;
                    for (int i = 0; i < cameraModeText.Length; i++)
                    {
                        DrawCameraMode drawCameraMode =
                            (DrawCameraMode)Enum.Parse(typeof(DrawCameraMode), cameraModeText[i]);

                        if (PluginHubFunc.ShouldBeginHorizontal(i, 3))
                            GUILayout.BeginHorizontal();

                        Color oldColor = GUI.color;
                        if (CurrDrawCameraModeIs(drawCameraMode))
                            GUI.color = PluginHubFunc.SelectedColor;
                        if (GUILayout.Button(drawCameraMode.ToString(), GUILayout.Width(width3)))
                        {
                            //切换相机模式
                            DrawCameraMode mode = drawCameraMode;
                            ChangeDrawCameraMode(mode);
                        }

                        GUI.color = oldColor;

                        if (PluginHubFunc.ShouldEndHorizontal(i, 3) || i == cameraModeText.Length - 1)
                            GUILayout.EndHorizontal();
                    }

                }
            }
            GUILayout.EndVertical();
        }

        public bool CurrDrawCameraModeIs(DrawCameraMode mode)
        {
            if (SceneView.lastActiveSceneView == null) return false;

            SceneView.CameraMode paraMode;
            try
            {
                paraMode = SceneView.GetBuiltinCameraMode(mode);
            }
            catch
            {
                // Debug.Log(e);
                return false;
            }

            return SceneView.lastActiveSceneView.cameraMode == paraMode;
        }

        //修改相机模式并保存到最近的相机模式
        public static void ChangeDrawCameraMode(DrawCameraMode mode)
        {
            if (SceneView.lastActiveSceneView == null) return;
            SceneView.lastActiveSceneView.cameraMode = SceneView.GetBuiltinCameraMode(mode);
            if (recentCameraMode.Count >= 2)
            {
                DrawCameraMode mode1 = recentCameraMode[1];
                recentCameraMode.Clear();
                recentCameraMode.Add(mode1);
            }

            recentCameraMode.Add(mode);
        }

        public static void SwitchRecentCameraModeShotcut(DrawCameraMode currMode)
        {
            if (recentCameraMode.Count == 0)
            {
                //给个默认值
                recentCameraMode.Add(DrawCameraMode.Textured);
                recentCameraMode.Add(DrawCameraMode.BakedLightmap);
            }
            else if (recentCameraMode.Count == 1)
            {
                if (recentCameraMode[0] == DrawCameraMode.Textured)
                    recentCameraMode.Add(DrawCameraMode.BakedLightmap);
                if (recentCameraMode[0] == DrawCameraMode.BakedLightmap)
                    recentCameraMode.Add(DrawCameraMode.Textured);
            }
            else if (recentCameraMode.Count == 2 && recentCameraMode[0] == recentCameraMode[1])
            {
                recentCameraMode.Clear();
                recentCameraMode.Add(DrawCameraMode.Textured);
                recentCameraMode.Add(DrawCameraMode.BakedLightmap);
            }

            if (currMode == recentCameraMode[0])
                ChangeDrawCameraMode(recentCameraMode[1]);
            else
                ChangeDrawCameraMode(recentCameraMode[0]);
        }

#if PH_CameraShowModeShorcut

        #region Camera模式菜单
        //Alt+S
        [MenuItem(MenuPrefix + "CameraShowModel/切换最近相机模式 &S", false, 1)]
        public static void SwitchBetweenNormalLightmap()
        {
            CameraShowModeModule.SwitchRecentCameraModeShotcut(SceneView.lastActiveSceneView.cameraMode.drawMode);
        }

        //Alt+1
        [MenuItem(MenuPrefix + "CameraShowModel/相机模式-Shaded &1", false, 2)]
        public static void ChangeToNormalCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Textured);
        }

        //Alt+2
        [MenuItem(MenuPrefix + "CameraShowModel/相机模式-Wireframe &2", false, 3)]
        public static void ChangeToWireframeCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.Wireframe);
        }

        //Alt+3
        [MenuItem(MenuPrefix + "CameraShowModel/相机模式-ShadedWireframe &3", false, 4)]
        public static void ChangeToShadedWireframeCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.TexturedWire);
        }

        //Alt+4
        [MenuItem(MenuPrefix + "CameraShowModel/相机模式-BakedLightmap &4", false, 5)]
        public static void ChangeToBakedLightmapCameraMode()
        {
            CameraShowModeModule.ChangeDrawCameraMode(DrawCameraMode.BakedLightmap);
        }
        #endregion

#endif


    }
}