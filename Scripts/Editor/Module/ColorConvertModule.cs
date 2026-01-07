using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PluginHub.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginHub.Editor
{
    public class ColorConvertModule : PluginHubModuleBase
    {
        public override ModuleType moduleType => ModuleType.Tool;
        private bool addFSuffix = false; //是否添加f后缀
        private bool addPrefix = false; //是否添加#前缀

        private Color _theColor = Color.white;

        public override string moduleDescription => "一个在HTML颜色和0-1颜色范围之间转换颜色的小工具";

        private GUILayoutOption[] miniBtnOption = { GUILayout.ExpandWidth(false), GUILayout.Width(23), GUILayout.Height(17) };

        // -1 没有想要输入颜色值 0 0-1 1 0-255 2 hex
        private int intentInput = -1;
        private string inputStr0, inputStr1, inputStr2, inputStr3;

        protected override void DrawGuiContent()
        {
            _theColor = EditorGUILayout.ColorField("Color Picker", _theColor);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("0-1 Color：", GUILayout.Width(150));
                string text = $"{_theColor.r:F3}{(addFSuffix ? "f" : "")},{_theColor.g:F3}{(addFSuffix ? "f" : "")},{_theColor.b:F3}{(addFSuffix ? "f" : "")},{_theColor.a:F3}{(addFSuffix ? "f" : "")}";
                GUILayout.Label(text);


                //输入按钮
                if (GUILayout.Button(PluginHubEditor.IconContent("CollabEdit Icon", "", "Input"), miniBtnOption))
                {
                    inputStr0 = _theColor.r.ToString("F3");
                    inputStr1 = _theColor.g.ToString("F3");
                    inputStr2 = _theColor.b.ToString("F3");
                    inputStr3 = _theColor.a.ToString("F3");
                    intentInput = 0;
                }
                //添加f后缀按钮
                if (GUILayout.Button(PluginHubEditor.GuiContent("F", "add 'f' suffix"), miniBtnOption))
                    addFSuffix = !addFSuffix;

                //拷贝按钮
                if (GUILayout.Button(PluginHubEditor.IconContent("Clipboard", "", "Copy"), GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = text;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("0-255 Color:", GUILayout.Width(150));
                string text =
                    $"{_theColor.r * 255:F0},{_theColor.g * 255:F0},{_theColor.b * 255:F0},{_theColor.a * 255:F0}";
                GUILayout.Label(text);

                //输入按钮
                if (GUILayout.Button(PluginHubEditor.IconContent("CollabEdit Icon", "", "Input"), miniBtnOption))
                {
                    inputStr0 = (_theColor.r * 255).ToString("F0");
                    inputStr1 = (_theColor.g * 255).ToString("F0");
                    inputStr2 = (_theColor.b * 255).ToString("F0");
                    inputStr3 = (_theColor.a * 255).ToString("F0");
                    intentInput = 1;
                }
                //拷贝按钮
                if (GUILayout.Button(PluginHubEditor.IconContent("Clipboard", "", "Copy"), GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = text;
            }
            GUILayout.EndHorizontal();

            string hexColor = ColorUtility.ToHtmlStringRGBA(_theColor);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("HTML Color(HEX)：", GUILayout.Width(150));
                GUILayout.Label(addPrefix ? $"#{hexColor}" : hexColor);

                //输入按钮
                if (GUILayout.Button(PluginHubEditor.IconContent("CollabEdit Icon", "", "Input"), miniBtnOption))
                {
                    inputStr0 = hexColor.Substring(0, 2);
                    inputStr1 = hexColor.Substring(2, 2);
                    inputStr2 = hexColor.Substring(4, 2);
                    inputStr3 = hexColor.Substring(6, 2);
                    intentInput = 2;
                }

                //添加#前缀按钮
                if (GUILayout.Button(PluginHubEditor.GuiContent("#", "add '#' prefix"), miniBtnOption))
                    addPrefix = !addPrefix;

                //拷贝按钮
                if (GUILayout.Button(PluginHubEditor.IconContent("Clipboard", "", "Copy"), miniBtnOption))
                    GUIUtility.systemCopyBuffer = addPrefix ? $"#{hexColor}" : hexColor;
            }
            GUILayout.EndHorizontal();


            // 用户颜色输入UI
            if (intentInput != -1)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Input color : ", GUILayout.Width(150));

                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("R", GUILayout.Width(50));
                            GUILayout.Label("G", GUILayout.Width(50));
                            GUILayout.Label("B", GUILayout.Width(50));
                            GUILayout.Label("A", GUILayout.Width(50));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            inputStr0 = GUILayout.TextField(inputStr0, GUILayout.Width(50));
                            inputStr1 = GUILayout.TextField(inputStr1, GUILayout.Width(50));
                            inputStr2 = GUILayout.TextField(inputStr2, GUILayout.Width(50));
                            inputStr3 = GUILayout.TextField(inputStr3, GUILayout.Width(50));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("▼"))
                                inputStr0 = MoveString(inputStr0, -1);
                            if (GUILayout.Button("▲"))
                                inputStr0 = MoveString(inputStr0, 1);
                            if (GUILayout.Button("▼"))
                                inputStr1 = MoveString(inputStr1, -1);
                            if (GUILayout.Button("▲"))
                                inputStr1 = MoveString(inputStr1, 1);
                            if (GUILayout.Button("▼"))
                                inputStr2 = MoveString(inputStr2, -1);
                            if (GUILayout.Button("▲"))
                                inputStr2 = MoveString(inputStr2, 1);
                            if (GUILayout.Button("▼"))
                                inputStr3 = MoveString(inputStr3, -1);
                            if (GUILayout.Button("▲"))
                                inputStr3 = MoveString(inputStr3, 1);
                        }
                        GUILayout.EndHorizontal();

                    }
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("OK",GUILayout.Width(50)))
                    {
                        switch (intentInput)
                        {
                            case 0:
                                //填入0-1颜色
                                _theColor = new Color(float.Parse(inputStr0), float.Parse(inputStr1), float.Parse(inputStr2), float.Parse(inputStr3));
                                break;
                            case 1:
                                //填入0-255颜色
                                _theColor = new Color(float.Parse(inputStr0) / 255, float.Parse(inputStr1) / 255, float.Parse(inputStr2) / 255, float.Parse(inputStr3) / 255);
                                break;
                            case 2:
                                //填入hex颜色
                                ColorUtility.TryParseHtmlString($"#{inputStr0}{inputStr1}{inputStr2}{inputStr3}", out _theColor);
                                break;
                        }
                        intentInput = -1;
                    }
                }
                GUILayout.EndHorizontal();
            }

            //TODO 颜色历史和颜色收藏功能
            // GUILayout.BeginHorizontal();
            // {
            //     GUILayout.FlexibleSpace();
            //     if (PluginHubFunc.DrawStarBtn(""))
            //     {
            //         AddRecordableObject(new ColorContainer() { color = _theColor });
            //     }
            // }
            // GUILayout.EndHorizontal();
            //
            //
            // GUILayout.Space(50);
            //
            // GUILayout.Label("Favorites / Input History", EditorStyles.boldLabel);
            // //绘制收藏夹和历史颜色
            // GUILayout.BeginVertical();
            // {
            //     for(int i = RecordableAssets.Count - 1; i >= 0; i--)
            //     {
            //         GUILayout.BeginHorizontal();
            //         {
            //             Color color = (RecordableAssets[i] as ColorContainer).color;
            //             EditorGUILayout.ColorField($"Color {i}", color);
            //
            //             if (PluginHubFunc.DrawDeleteBtn())
            //             {
            //                 RemoveRecordableObject(RecordableAssets[i]);
            //             }
            //         }
            //         GUILayout.EndHorizontal();
            //     }
            // }
            // GUILayout.EndVertical();
        }

        private string MoveString(string inputString, int move)
        {
            if (inputString.IsFloat())
            {
                float value = float.Parse(inputString);
                value += move * 0.01f;
                return value.ToString("F3");
            }
            else if (inputString.IsInt())
            {
                int value = int.Parse(inputString);
                value += move;
                return value.ToString();
            }
            else if (inputString.Is2CharHex())
            {
                int value = int.Parse(inputString, System.Globalization.NumberStyles.HexNumber);
                value += move;
                return value.ToString("X2");
            }
            else
            {
                return inputString;
            }
        }

    }
}