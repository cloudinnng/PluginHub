using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime.Extends
{
    /// <summary>
    /// 系统GUI类的扩展
    /// 需要放在OnGui函数内调用
    /// </summary>
    public static class GUIEx
    {
        private static readonly GUIStyle _style;
        private static GUISkin _myCustonSkin;


        //在GUILayout中,label默认是自动换行的,这里提供一个不换行的版本
        private static GUIStyle _noWordWrapLabel;

        public static GUIStyle noWordWrapLabel
        {
            get
            {
                if (_noWordWrapLabel == null)
                {
                    _noWordWrapLabel = new GUIStyle(GUI.skin.label);
                    _noWordWrapLabel.wordWrap = false;
                }

                return _noWordWrapLabel;
            }
        }

        static GUIEx()
        {
            _style = new GUIStyle();
            Font font = Font.CreateDynamicFontFromOSFont(new string[]
            {
                "Microsoft YaHei Bold",
                "Microsoft YaHei",
                "Arial Bold", "Arial"
            }, 12);
            _style.font = font;

            _myCustonSkin = CFHelper.GetGUISkin();
        }

        /// <summary>
        /// 可在游戏相机上绘制文字
        /// </summary>
        public static void DrawString(string text, Vector3 worldPos, Color? colour = null)
        {
            Camera camera = Camera.main;
            float angle = Vector3.Angle(camera.transform.forward, worldPos - camera.transform.position);

            if (angle < 90) //避免相机朝向文字的反方向也显示文字
            {
                float guiScale = Screen.height / 800f;
                guiScale = Mathf.Clamp(guiScale, 1, 4);

                Matrix4x4 matrix4X4 = GUI.matrix;
                GUI.matrix = Matrix4x4.Scale(Vector3.one * guiScale);

                if (!colour.HasValue)
                    colour = Color.white;
                Color restoreColor = GUI.color;
                GUI.color = colour.Value;
                _style.normal.textColor = colour.Value;

                Vector3 screenPos = camera.WorldToScreenPoint(worldPos) / guiScale;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height) / guiScale;

                if (screenPos.y >= 0 || screenPos.y < Screen.height || screenPos.x >= 0 || screenPos.x < Screen.width ||
                    screenPos.z > 0)
                {
                    //绘制
                    Vector2 textSize = _style.CalcSize(new GUIContent(text));
                    Rect textRect = new Rect((screenPos.x - (textSize.x / 2)), (screenSize.y - screenPos.y), textSize.x,
                        textSize.y);
                    Rect bgRect = new Rect(textRect.x - 10, textRect.y - 5, textRect.width + 20, textRect.height + 10);

                    GUI.Box(bgRect, "", _myCustonSkin.box);
                    GUI.Label(textRect, text, _style);
                    //GUI.Label(textRect, guiScale.ToString(),_style);//debug
                }

                GUI.color = restoreColor;
                GUI.matrix = matrix4X4;
            }
        }

        public static Vector2 GetSize(string text)
        {
            Vector2 textSize = _style.CalcSize(new GUIContent(text));
            textSize.x += 20;
            textSize.y += 10;
            return textSize;
        }
    }
}