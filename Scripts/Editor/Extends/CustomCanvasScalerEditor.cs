using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

// 《Unity编辑器开发与拓展》 P70页提到的，保持原有Inspector的同时，为系统组件添加新的Inspector功能

namespace PluginHub.Editor
{
    [CustomEditor(typeof(CanvasScaler))]
    public class CustomCanvasScalerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor instance;

        private void OnEnable()
        {
            // CanvasScalerEditor
            var editorType = typeof(CanvasScalerEditor);
            instance = CreateEditor(targets, editorType);
        }

        private void OnDisable()
        {
            if (instance != null)
                DestroyImmediate(instance);
        }

        public override void OnInspectorGUI()
        {
            // 绘制原有Inspector
            if (instance != null)
                instance.OnInspectorGUI();

            // 绘制新的Inspector
            GUILayout.Space(10);
            GUILayout.Label("iPhone 15 Pro Max 分辨率: 1290 x 2796");
        }
    }
}