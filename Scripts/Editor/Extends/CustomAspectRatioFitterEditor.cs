using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

// 《Unity编辑器开发与拓展》 P70页提到的，保持原有Inspector的同时，为系统组件添加新的Inspector功能

namespace PluginHub.Editor
{
    [CustomEditor(typeof(AspectRatioFitter))]
    public class CustomAspectRatioFitterEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor instance;

        private void OnEnable()
        {
            var editorType = typeof(AspectRatioFitterEditor);
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

            AspectRatioFitter aspectRatioFitter = (AspectRatioFitter)target;

            // 绘制新的Inspector
            GUILayout.Space(10);
            if (GUILayout.Button("设置 aspectRatio 为Image比例"))
            {
                Image image = aspectRatioFitter.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    aspectRatioFitter.aspectRatio = (float)image.sprite.rect.width / image.sprite.rect.height;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(aspectRatioFitter);
                }
                else
                {
                    Debug.LogWarning("没有找到Image组件或Sprite为空，无法设置aspectRatio。");
                }
            }
        }
    }
}