using System.Collections;
using System.Collections.Generic;
using PluginHub;
using PluginHub.Helper;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    //shader 调试器模块
    //取色 用于调试shader
    public class ShaderDebuggerModule : PluginHubModuleBase
    {
        private Color _colorPicked; //拾取到的颜色

        protected override void DrawModuleDebug()
        {
            base.DrawModuleDebug();

            GUILayout.Space(50);
        }

        protected override void DrawGuiContent()
        {

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Picked Color");
                    GUILayout.FlexibleSpace();
                    _colorPicked = EditorGUILayout.ColorField(_colorPicked, GUILayout.Width(100), GUILayout.Height(80));
                }
                GUILayout.EndHorizontal();

                float width = PluginHubWindow.Window.CaculateButtonWidth(4);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Button(" ", PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button("R", PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button("G", PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button("B", PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Button("0 - 1 Range", PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button(_colorPicked.r.ToString("F3"), PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button(_colorPicked.g.ToString("F3"), PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button(_colorPicked.b.ToString("F3"), PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Button("0 - 255 Range", PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width), GUILayout.Width(width));
                    GUILayout.Button((_colorPicked.r * 255f).ToString(), PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button((_colorPicked.g * 255f).ToString(), PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                    GUILayout.Button((_colorPicked.b * 255f).ToString(), PluginHubFunc.PhguiSkinUse.label, GUILayout.Width(width));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();


        }

    }
}
