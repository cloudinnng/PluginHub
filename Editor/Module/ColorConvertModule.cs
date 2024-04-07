using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PluginHub.Helper;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class ColorConvertModule : PluginHubModuleBase
    {

        private string inputHexColor = "#FFFFFF"; //输入的十六进制颜色
        private string input0To1Color = "1,1,1,1"; //输入的0-1颜色


        private bool addFSuffix = false; //是否添加f后缀

        private Color _colorPicker = Color.white;

        public override string moduleDescription => "一个在HTML颜色和0-1颜色范围之间转换颜色的小工具";

        protected override void DrawGuiContent()
        {
            _colorPicker = EditorGUILayout.ColorField("Color Picker", _colorPicker);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("0-1 Color：", GUILayout.Width(150));
                string text = $"{_colorPicker.r:F2}{(addFSuffix ? "f" : "")},{_colorPicker.g:F2}{(addFSuffix ? "f" : "")},{_colorPicker.b:F2}{(addFSuffix ? "f" : "")},{_colorPicker.a:F2}{(addFSuffix ? "f" : "")}";
                GUILayout.Label(text);

                //添加f后缀按钮
                if (GUILayout.Button(PluginHubFunc.GuiContent("F", "add 'f' suffix"), GUILayout.ExpandWidth(false),
                        GUILayout.Height(17)))
                {
                    addFSuffix = !addFSuffix;
                }

                //拷贝按钮
                if (GUILayout.Button(PluginHubFunc.Icon("Clipboard", "", "Copy"), GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = text;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("0-255 Color:", GUILayout.Width(150));
                string text =
                    $"{_colorPicker.r * 255:F0},{_colorPicker.g * 255:F0},{_colorPicker.b * 255:F0},{_colorPicker.a * 255:F0}";
                GUILayout.Label(text);
                //拷贝按钮
                if (GUILayout.Button(PluginHubFunc.Icon("Clipboard", "", "Copy"), GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = text;
            }
            GUILayout.EndHorizontal();




            GUILayout.Space(10);
            GUILayout.Label("手动输入:");

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("HTML Color：", GUILayout.Width(150));
                inputHexColor = GUILayout.TextField(inputHexColor);
                if (!inputHexColor.StartsWith("#"))
                {
                    inputHexColor = "#" + inputHexColor;
                }
            }
            GUILayout.EndHorizontal();


            ColorUtility.TryParseHtmlString(inputHexColor, out Color color);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("0-1 Color:", GUILayout.Width(150));
                string suffix = "";
                if (addFSuffix)
                    suffix = "f";

                string s = $"{color.r}{suffix},{color.g}{suffix},{color.b}{suffix},{color.a}{suffix}";
                GUILayout.Label(s);

                Color oldColor = GUI.color;
                if (addFSuffix)
                    GUI.color = PluginHubFunc.SelectedColor;
                //添加f后缀按钮
                if (GUILayout.Button(PluginHubFunc.GuiContent("F", "add 'f' suffix"), GUILayout.ExpandWidth(false),
                        GUILayout.Height(17)))
                {
                    addFSuffix = !addFSuffix;
                }

                GUI.color = oldColor;

                //拷贝按钮
                if (GUILayout.Button(PluginHubFunc.Icon("Clipboard", "", "Copy"), GUILayout.ExpandWidth(false)))
                {
                    //copy string to clipboard
                    GUIUtility.systemCopyBuffer = s;
                }
            }
            GUILayout.EndHorizontal();


            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("0-1 Color:", GUILayout.Width(150));
                input0To1Color = GUILayout.TextField(input0To1Color);
            }
            GUILayout.EndHorizontal();

            string[] splitTexts = input0To1Color.Split(',');
            for (int i = 0; i < splitTexts.Length; i++)
            {
                if (splitTexts[i].Contains("f"))
                {
                    splitTexts[i] = splitTexts[i].Replace("f", "");
                }
            }

            float r = splitTexts.Length > 0 && splitTexts[0].IsNumber() ? float.Parse(splitTexts[0]) : 0;
            float g = splitTexts.Length > 1 && splitTexts[1].IsNumber() ? float.Parse(splitTexts[1]) : 0;
            float b = splitTexts.Length > 2 && splitTexts[2].IsNumber() ? float.Parse(splitTexts[2]) : 0;
            float a = splitTexts.Length > 3 && splitTexts[3].IsNumber() ? float.Parse(splitTexts[3]) : 0;
            Color c = new Color(r, g, b, a);
            string colorStr = ColorUtility.ToHtmlStringRGBA(c);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("HTML Color：", GUILayout.Width(150));
                GUILayout.Label(colorStr);

                //拷贝按钮
                if (GUILayout.Button(PluginHubFunc.Icon("Clipboard", "", "Copy"), GUILayout.ExpandWidth(false)))
                {
                    GUIUtility.systemCopyBuffer = colorStr;
                }
            }
            GUILayout.EndHorizontal();


        }

    }
    public static class ExMethods
    {
        //是不是浮点数字
        public static bool IsFloat(this string value)
        {
            return Regex.IsMatch(value, "^([0-9]{1,}[.][0-9]*)$");
        }
        //是不是整型数字
        public static bool IsInt(this string text)
        {
            return Regex.IsMatch(text, "^([0-9]{1,})$");
        }
        //是不是数字
        public static bool IsNumber(this string text)
        {
            return text.IsFloat() || text.IsInt();
        }
    }
}