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

            if (foldout = EditorGUILayout.Foldout(foldout, "更多信息", true))
            {
                RectTransform rectTransform = target as RectTransform;

                // 将锚点矩形精确对齐到当前 UI 在父节点坐标下的外接矩形四角，并把 offset 清零（与 Unity 检视里“拉伸四角”效果一致）
                if (GUILayout.Button("锚点贴合当前矩形四角"))
                {
                    int applied = 0;
                    foreach (var obj in targets)
                    {
                        if (obj is RectTransform rt)
                        {
                            if (SnapAnchorsToRectCorners(rt))
                                applied++;
                        }
                    }

                    Debug.Log($"[CustomRectTransformEditor] 锚点贴合四角：成功处理 {applied}/{targets.Length} 个对象。");
                }

                EditorGUILayout.Vector3Field("Position", rectTransform.position);
                rectTransform.localPosition = EditorGUILayout.Vector3Field("Local Position", rectTransform.localPosition);
                rectTransform.anchoredPosition = EditorGUILayout.Vector2Field("Anchored Position", rectTransform.anchoredPosition);
                rectTransform.sizeDelta = EditorGUILayout.Vector2Field("Size Delta", rectTransform.sizeDelta);
                EditorGUILayout.RectField("Rect", rectTransform.rect);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                // Screen.width, Screen.height 不能在编辑器中调用，显示的是不正确的
                // EditorGUILayout.Vector2Field("Screen.width/height", new Vector2(Screen.width, Screen.height));
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

        /// <summary>
        /// 把 anchorMin / anchorMax 挪到「当前矩形」在父 Rect 归一化坐标系下的四角位置，并 offsetMin/Max 置零。
        /// 数学上等价于：原先子矩形相对父 anchor 框的像素偏移，换算成 anchor 坐标上的增量并合并进 anchor。
        /// </summary>
        /// <returns>是否执行成功（无父 RectTransform 或父尺寸为 0 时返回 false）</returns>
        private static bool SnapAnchorsToRectCorners(RectTransform rt)
        {
            if (rt == null)
                return false;

            RectTransform parentRt = rt.parent as RectTransform;
            if (parentRt == null)
            {
                Debug.LogWarning($"[CustomRectTransformEditor] 「{rt.name}」没有 RectTransform 父物体，无法把锚点对齐到四角。", rt);
                return false;
            }

            float w = parentRt.rect.width;
            float h = parentRt.rect.height;
            if (Mathf.Approximately(w, 0f) || Mathf.Approximately(h, 0f))
            {
                Debug.LogWarning($"[CustomRectTransformEditor] 父物体「{parentRt.name}」Rect 宽高为 0，跳过。", parentRt);
                return false;
            }

            Undo.RecordObject(rt, "锚点贴合当前矩形四角");

            // 与 Unity 社区常用的「扩展锚点包住当前矩形」公式一致（本文件上方曾注释的 Auto Anchors）
            Vector2 newMin = new Vector2(
                rt.anchorMin.x + rt.offsetMin.x / w,
                rt.anchorMin.y + rt.offsetMin.y / h);
            Vector2 newMax = new Vector2(
                rt.anchorMax.x + rt.offsetMax.x / w,
                rt.anchorMax.y + rt.offsetMax.y / h);

            rt.anchorMin = newMin;
            rt.anchorMax = newMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(rt);
            return true;
        }
    }
}
