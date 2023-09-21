using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace PluginHub.Helper
{
    //对EditorGUILayout的封装，整合一些常用功能
    public static class EditorGUILayoutEx
    {
        static EditorGUILayoutEx()
        {
            EditorStyles.label.wordWrap = true;
        }

        public static void TextBox(string text)
        {
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField(text, EditorStyles.label);
            EditorGUILayout.EndHorizontal();
        }


        //一行两个文本
        public static void RowTwoText(string text0, string text1)
        {
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField(text0, EditorStyles.label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(text1, EditorStyles.label, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }


        #region 一行两个东西

        private const float labelWidth = 130;
        private const bool expandWidth = false;

        public static T LableWithObjectFiled<T>(string lableText, Object obj) where T : Object
        {
            T objReturn;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth), GUILayout.ExpandWidth(expandWidth));
                objReturn = (T)EditorGUILayout.ObjectField(obj, typeof(T), true);
            }
            GUILayout.EndHorizontal();
            return objReturn;
        }

        public static bool LabelWithToggle(string lableText, bool toggleOldValue)
        {
            bool toggleReturn = false;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth), GUILayout.ExpandWidth(expandWidth));
                toggleReturn = GUILayout.Toggle(toggleOldValue, "");
            }
            GUILayout.EndHorizontal();
            return toggleReturn;
        }

        public static float LabelWithSlider(string lableText, float sliderOldValue, float leftValue, float rightValue)
        {
            float toggleReturn;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth));
                toggleReturn = GUILayout.HorizontalSlider(sliderOldValue, leftValue, rightValue);
                toggleReturn = EditorGUILayout.FloatField("", toggleReturn, GUILayout.Width(50));
            }
            GUILayout.EndHorizontal();
            return toggleReturn;
        }

        public static Vector3 LabelWithVector3Field(string lableText, Vector3 oldVector3)
        {
            Vector3 returnValue;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth));
                returnValue = EditorGUILayout.Vector3Field("", oldVector3);
            }
            GUILayout.EndHorizontal();
            return returnValue;
        }

        #endregion
    }
}