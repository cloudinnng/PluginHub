namespace PluginHub.Editor
{
    // 扩展了UGUI Image组件的检视面板，添加按比例调整RectTransform尺寸的按钮
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEditor;
    using System.Linq;
    using System.Reflection;

    [CustomEditor(typeof(Image)), CanEditMultipleObjects]
    public sealed class CustomImageEditor : Editor
    {
        private Editor instance;

        private void OnEnable()
        {
            var editorType = Assembly.GetAssembly(typeof(UnityEditor.UI.ImageEditor)).GetTypes()
                .FirstOrDefault(m => m.Name == "ImageEditor");
            instance = CreateEditor(targets, editorType);
        }

        public override void OnInspectorGUI()
        {
            if (instance)
                instance.OnInspectorGUI();

            Image image = target as Image;
            if (image == null || image.sprite == null)
                return;

            Rect spriteRect = image.sprite.rect;
            float aspectRatio = spriteRect.width / spriteRect.height;

            RectTransform rt = image.rectTransform;

            GUILayout.Space(6);
            EditorGUILayout.LabelField($"原始图片尺寸: {spriteRect.width} x {spriteRect.height}  比例: {aspectRatio:F3}",
                EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("依照比例调整高度"))
                {
                    foreach (var t in targets)
                    {
                        Image img = t as Image;
                        if (img == null || img.sprite == null) continue;
                        RectTransform rectTransform = img.rectTransform;
                        float ratio = img.sprite.rect.width / img.sprite.rect.height;
                        Undo.RecordObject(rectTransform, "依照比例调整高度");
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x,
                            rectTransform.sizeDelta.x / ratio);
                    }
                }

                if (GUILayout.Button("依照比例调整宽度"))
                {
                    foreach (var t in targets)
                    {
                        Image img = t as Image;
                        if (img == null || img.sprite == null) continue;
                        RectTransform rectTransform = img.rectTransform;
                        float ratio = img.sprite.rect.width / img.sprite.rect.height;
                        Undo.RecordObject(rectTransform, "依照比例调整宽度");
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.y * ratio,
                            rectTransform.sizeDelta.y);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public override bool HasPreviewGUI() => instance != null && instance.HasPreviewGUI();

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (instance)
                instance.OnPreviewGUI(rect, background);
        }

        private void OnDisable()
        {
            if (instance)
                DestroyImmediate(instance);
        }
    }
}
