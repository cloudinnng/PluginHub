using System;
using System.Linq;
using System.Reflection;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PluginHub.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(ScrollRect))]
    public class CustomScrollRectEditor : Editor
    {
        private Editor instance;

        protected void OnEnable()
        {
            var editorType = typeof(ScrollRectEditor);
            instance = CreateEditor(target, editorType);
        }

        // 这个在Disable时候销毁很重要，不然会报错到（ScrollRectEditor）类里面
        private void OnDisable()
        {
            if (instance != null)
                DestroyImmediate(instance);
        }

        public override void OnInspectorGUI()
        {
            if (instance != null)
                instance.OnInspectorGUI();

            ScrollRect scrollRect = target as ScrollRect;
            if (scrollRect == null)
                return;
            if (scrollRect.content == null)
                return;

            // 下面添加两个滑动条，可用于设置滚动视图的标准化位置，设置的值会保存到ScrollRect组件中，但有时候不会在Prefab中保存
            float newValue = EditorGUILayout.Slider("Horizontal Normalized Position",
                scrollRect.horizontalNormalizedPosition, 0, 1);

            if (GUI.changed)
            {
                scrollRect.horizontalNormalizedPosition = newValue;
                if (scrollRect.horizontalScrollbar)
                    scrollRect.horizontalScrollbar.value = newValue;
                EditorUtility.SetDirty(scrollRect);
                // Debug.Log($"Set H {newValue}");
            }

            newValue = EditorGUILayout.Slider("Vertical Normalized Position",
                scrollRect.verticalNormalizedPosition, 0, 1);
            if (GUI.changed)
            {
                scrollRect.verticalNormalizedPosition = newValue;
                if (scrollRect.verticalScrollbar)
                    scrollRect.verticalScrollbar.value = newValue;
                EditorUtility.SetDirty(scrollRect);
                // Debug.Log($"Set V {newValue}");
            }
        }
    }
}