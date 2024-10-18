namespace PluginHub.Editor
{
    // 扩展了RectTransform组件的检视面板
    using UnityEngine;
    using UnityEditor;
    using System.Linq;
    using System.Reflection;

    [CustomEditor(typeof(RectTransform)), CanEditMultipleObjects]
    public sealed class CustomRectTransformEditor : Editor
    {
        private MethodInfo onSceneGUI;
        private Editor instance;

        private static readonly object[] emptyArray = new object[0];
        // 默认折叠状态
        private static bool foldout = false;

        private void OnEnable()
        {
            var editorType = Assembly.GetAssembly(typeof(Editor)).GetTypes()
                .FirstOrDefault(m => m.Name == "RectTransformEditor");
            instance = CreateEditor(targets, editorType);
            onSceneGUI = editorType.GetMethod("OnSceneGUI",
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public override void OnInspectorGUI()
        {
            if (instance)
            {
                instance.OnInspectorGUI();
            }
            // GUILayout.Space(20f);

            // if(GUILayout.Button("Auto Anchors"))
            // {
            //     for (int i = 0; i < targets.Length; i++)
            //     {
            //         RectTransform tempTarget = targets[i] as RectTransform;
            //         Undo.RecordObject(tempTarget, "Auto Anchors");
            //         RectTransform prt = tempTarget.parent as RectTransform;
            //         Vector2 anchorMin = new Vector2(
            //             tempTarget.anchorMin.x + tempTarget.offsetMin.x / prt.rect.width,
            //             tempTarget.anchorMin.y + tempTarget.offsetMin.y / prt.rect.height);
            //         Vector2 anchorMax = new Vector2(
            //             tempTarget.anchorMax.x + tempTarget.offsetMax.x / prt.rect.width,
            //             tempTarget.anchorMax.y + tempTarget.offsetMax.y / prt.rect.height);
            //         tempTarget.anchorMin = anchorMin;
            //         tempTarget.anchorMax = anchorMax;
            //         tempTarget.offsetMin = tempTarget.offsetMax = Vector2.zero;
            //     }
            // }

            GUILayout.Space(10);

            foldout = EditorGUILayout.Foldout(foldout, "更多信息");
            if (foldout)
            {
                RectTransform rectTransform = target as RectTransform;
                EditorGUILayout.Vector3Field("Position", rectTransform.position);
                rectTransform.localPosition = EditorGUILayout.Vector3Field("Local Position", rectTransform.localPosition);
                rectTransform.anchoredPosition = EditorGUILayout.Vector2Field("Anchored Position", rectTransform.anchoredPosition);
                rectTransform.sizeDelta = EditorGUILayout.Vector2Field("Size Delta", rectTransform.sizeDelta);
                EditorGUILayout.RectField("Rect", rectTransform.rect);
            }
        }

        private void OnSceneGUI()
        {
            if (instance)
            {
                onSceneGUI.Invoke(instance, emptyArray);
            }
        }

        private void OnDisable()
        {
            if (instance)
            {
                DestroyImmediate(instance);
            }
        }
    }
}