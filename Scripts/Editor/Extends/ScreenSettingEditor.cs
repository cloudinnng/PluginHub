using UnityEditor;
using UnityEngine;
using PluginHub.Runtime;

namespace PluginHub.Editor
{
    [CustomEditor(typeof(ScreenSetting))]
    public class ScreenSettingEditor : UnityEditor.Editor
    {
        // 屏幕要求
        private SerializedProperty _screenRequirement;
        private SerializedProperty _designAspectRatio;
        private SerializedProperty _designResolution;

        // 帧率
        private SerializedProperty _targetFrameRate;

        // 移动设备
        private SerializedProperty _sleepPolicy;

        // 桌面端
        private SerializedProperty _autoWindowSize;
        private SerializedProperty _activeDisplayCount;

        // 其他
        private SerializedProperty _autoSwitchSceneIndex;

        private static class Styles
        {
            public static readonly GUIContent HeaderScreen = new GUIContent("屏幕要求");
            public static readonly GUIContent HeaderFrameRate = new GUIContent("帧率");
            public static readonly GUIContent HeaderMobile = new GUIContent("移动设备");
            public static readonly GUIContent HeaderDesktop = new GUIContent("桌面端 (Standalone)");
            public static readonly GUIContent HeaderOther = new GUIContent("其他");

            public static readonly GUIContent LabelRequirementLevel = new GUIContent("要求等级",
                "设置该应用程序对屏幕分辨率的要求等级。\n" +
                "• 无要求：不做任何分辨率/宽高比检查\n" +
                "• 宽高比要求：运行时会持续检查宽高比是否匹配\n" +
                "• 准确分辨率要求：运行时会持续检查分辨率是否完全匹配");

            public static readonly GUIContent LabelAspectRatio = new GUIContent("设计宽高比",
                "目标宽高比。例：16:9 填入 (16, 9)，32:9 填入 (32, 9)，21:9 填入 (21, 9)");

            public static readonly GUIContent LabelResolution = new GUIContent("设计分辨率",
                "目标分辨率。系统会自动从此值推导出宽高比。");

            public static readonly GUIContent LabelDerivedAspectRatio = new GUIContent("推导宽高比",
                "由设计分辨率自动计算得出（只读）");

            public static readonly GUIContent LabelFrameRate = new GUIContent("目标帧率",
                "应用程序的目标帧率。\n" +
                "• 不设置：保持 Unity 默认行为\n" +
                "• 不限制 (-1)：Application.targetFrameRate = -1，尽可能快地渲染\n" +
                "• 其他值：限制到指定帧率");

            public static readonly GUIContent LabelSleepPolicy = new GUIContent("休眠策略",
                "控制移动设备的屏幕休眠行为。\n展览、演示类应用通常应选择「永不休眠」。");

            public static readonly GUIContent LabelAutoWindowSize = new GUIContent("自动窗口尺寸",
                "启动后若检测到窗口模式，自动按照屏幕要求中的宽高比计算最大合适尺寸并设置窗口分辨率。\n" +
                "适用于在普通显示器上测试为异形宽高比设备开发的应用程序。\n" +
                "仅 Standalone 平台有效。");

            public static readonly GUIContent LabelActiveDisplayCount = new GUIContent("激活显示器数量",
                "启动时需要激活的显示器数量。\n" +
                "1 = 仅主显示器，大于 1 则自动激活对应数量的额外显示器。\n" +
                "仅 Standalone 平台有效。");

            public static readonly GUIContent LabelAutoSwitchScene = new GUIContent("自动切换场景",
                "打包后启动时自动切换到指定场景（按 Build Settings 中的索引）。\n" +
                "-1 表示不自动切换。仅在非编辑器模式下生效。");

            public static readonly Color SectionHeaderColor = new Color(0.7f, 0.85f, 1f, 1f);
        }

        private void OnEnable()
        {
            _screenRequirement = serializedObject.FindProperty("screenRequirement");
            _designAspectRatio = serializedObject.FindProperty("designAspectRatio");
            _designResolution = serializedObject.FindProperty("designResolution");
            _targetFrameRate = serializedObject.FindProperty("targetFrameRate");
            _sleepPolicy = serializedObject.FindProperty("sleepPolicy");
            _autoWindowSize = serializedObject.FindProperty("autoWindowSize");
            _activeDisplayCount = serializedObject.FindProperty("activeDisplayCount");
            _autoSwitchSceneIndex = serializedObject.FindProperty("autoSwitchSceneIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScreenRequirementSection();
            EditorGUILayout.Space(4);
            DrawFrameRateSection();
            EditorGUILayout.Space(4);
            DrawMobileSection();
            EditorGUILayout.Space(4);
            DrawDesktopSection();
            EditorGUILayout.Space(4);
            DrawOtherSection();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                DrawRuntimeInfo();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScreenRequirementSection()
        {
            DrawSectionHeader(Styles.HeaderScreen);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_screenRequirement, Styles.LabelRequirementLevel);

            var level = (ScreenSetting.ScreenRequirementLevel)_screenRequirement.enumValueIndex;

            switch (level)
            {
                case ScreenSetting.ScreenRequirementLevel.AspectRatio:
                    EditorGUILayout.PropertyField(_designAspectRatio, Styles.LabelAspectRatio);
                    DrawAspectRatioPreview(_designAspectRatio.vector2Value);
                    break;

                case ScreenSetting.ScreenRequirementLevel.ExactResolution:
                    EditorGUILayout.PropertyField(_designResolution, Styles.LabelResolution);
                    Vector2Int res = _designResolution.vector2IntValue;
                    if (res.x > 0 && res.y > 0)
                    {
                        Vector2 derived = SimplifyAspectRatio(res.x, res.y);
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.Vector2Field(Styles.LabelDerivedAspectRatio, derived);
                        EditorGUI.EndDisabledGroup();
                        DrawAspectRatioPreview(new Vector2(res.x, res.y));
                    }
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawFrameRateSection()
        {
            DrawSectionHeader(Styles.HeaderFrameRate);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_targetFrameRate, Styles.LabelFrameRate);
            EditorGUI.indentLevel--;
        }

        private void DrawMobileSection()
        {
            DrawSectionHeader(Styles.HeaderMobile);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_sleepPolicy, Styles.LabelSleepPolicy);
            EditorGUI.indentLevel--;
        }

        private void DrawDesktopSection()
        {
            DrawSectionHeader(Styles.HeaderDesktop);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_autoWindowSize, Styles.LabelAutoWindowSize);

            if (_autoWindowSize.boolValue)
            {
                var level = (ScreenSetting.ScreenRequirementLevel)_screenRequirement.enumValueIndex;
                if (level == ScreenSetting.ScreenRequirementLevel.None)
                {
                    EditorGUILayout.HelpBox(
                        "屏幕要求等级为「无要求」时，自动窗口尺寸将使用当前窗口的宽高比。\n" +
                        "若想使用特定宽高比，请将屏幕要求等级设为「宽高比要求」或更高。",
                        MessageType.Info);
                }
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_activeDisplayCount, Styles.LabelActiveDisplayCount);

            EditorGUI.indentLevel--;
        }

        private void DrawOtherSection()
        {
            DrawSectionHeader(Styles.HeaderOther);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_autoSwitchSceneIndex, Styles.LabelAutoSwitchScene);

            if (_autoSwitchSceneIndex.intValue >= 0)
            {
                int sceneCount = EditorBuildSettings.scenes.Length;
                if (_autoSwitchSceneIndex.intValue >= sceneCount)
                {
                    EditorGUILayout.HelpBox(
                        $"场景索引 {_autoSwitchSceneIndex.intValue} 超出 Build Settings 中的场景数量（共 {sceneCount} 个）。",
                        MessageType.Warning);
                }
                else
                {
                    string scenePath = EditorBuildSettings.scenes[_autoSwitchSceneIndex.intValue].path;
                    EditorGUILayout.HelpBox($"将自动切换到: {scenePath}", MessageType.None);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawRuntimeInfo()
        {
            DrawSectionHeader(new GUIContent("运行时状态"));
            EditorGUI.indentLevel++;

            var ss = (ScreenSetting)target;
            EditorGUILayout.LabelField("当前分辨率", $"{Screen.width} x {Screen.height}");

            float currentAspect = (float)Screen.width / Screen.height;
            EditorGUILayout.LabelField("当前宽高比", $"{currentAspect:F4}");

            if (ss.screenRequirement != ScreenSetting.ScreenRequirementLevel.None)
            {
                EditorGUILayout.LabelField("宽高比匹配", ss.IsAspectRatioMatch ? "✓ 匹配" : "✗ 不匹配");
            }
            if (ss.screenRequirement == ScreenSetting.ScreenRequirementLevel.ExactResolution)
            {
                EditorGUILayout.LabelField("分辨率匹配", ss.IsResolutionMatch ? "✓ 匹配" : "✗ 不匹配");
            }

            EditorGUI.indentLevel--;
        }

        #region 辅助绘制

        private static void DrawSectionHeader(GUIContent label)
        {
            EditorGUILayout.Space(2);
            var rect = EditorGUILayout.GetControlRect(false, 20);
            rect.xMin -= 2;
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.15f));
            var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
            EditorGUI.LabelField(rect, label, style);
        }

        // 绘制宽高比可视化预览条
        private static void DrawAspectRatioPreview(Vector2 ratio)
        {
            if (ratio.x <= 0 || ratio.y <= 0) return;

            float aspect = ratio.x / ratio.y;
            string label = $"{ratio.x}:{ratio.y} = {aspect:F4}";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");

            var rect = EditorGUILayout.GetControlRect(false, 36);
            float maxWidth = rect.width;
            float maxHeight = rect.height;

            float previewW, previewH;
            if (aspect >= 1f)
            {
                previewW = Mathf.Min(maxWidth, maxHeight * aspect);
                previewH = previewW / aspect;
            }
            else
            {
                previewH = maxHeight;
                previewW = previewH * aspect;
            }

            var previewRect = new Rect(rect.x, rect.y + (maxHeight - previewH) * 0.5f, previewW, previewH);
            EditorGUI.DrawRect(previewRect, new Color(0.3f, 0.6f, 0.9f, 0.3f));
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, previewRect.width, 1), new Color(0.3f, 0.6f, 0.9f, 0.8f));
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.yMax - 1, previewRect.width, 1), new Color(0.3f, 0.6f, 0.9f, 0.8f));
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, 1, previewRect.height), new Color(0.3f, 0.6f, 0.9f, 0.8f));
            EditorGUI.DrawRect(new Rect(previewRect.xMax - 1, previewRect.y, 1, previewRect.height), new Color(0.3f, 0.6f, 0.9f, 0.8f));

            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            EditorGUI.LabelField(previewRect, label, labelStyle);

            EditorGUILayout.EndHorizontal();
        }

        // 将分辨率简化为最简宽高比
        private static Vector2 SimplifyAspectRatio(int w, int h)
        {
            int gcd = GCD(w, h);
            return new Vector2(w / gcd, h / gcd);
        }

        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        #endregion
    }
}
