using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PluginHub.Runtime.Debugger.CustomWindow;

namespace PluginHub.Runtime
{
    // 为应用程序提供统一的屏幕配置入口：分辨率/宽高比要求、帧率、休眠、多屏、自动窗口尺寸、自动切换场景
    public class ScreenSetting : MonoBehaviour, IDebuggerCustomWindowGUI
    {
        #region 枚举定义

        // 屏幕分辨率要求等级，从弱到强
        public enum ScreenRequirementLevel
        {
            [InspectorName("无要求")]
            None,           // 不检查屏幕分辨率或宽高比
            [InspectorName("宽高比要求")]
            AspectRatio,    // 仅检查宽高比是否匹配
            [InspectorName("准确分辨率要求")]
            ExactResolution // 检查分辨率是否完全匹配（同时检查宽高比）
        }

        // 目标帧率
        public enum FrameRateType
        {
            [InspectorName("不设置")]
            DontCare = 0,
            [InspectorName("不限制 (-1)")]
            Unlimited = -1,
            [InspectorName("10 FPS")]
            FPS_10 = 10,
            [InspectorName("30 FPS")]
            FPS_30 = 30,
            [InspectorName("50 FPS")]
            FPS_50 = 50,
            [InspectorName("60 FPS")]
            FPS_60 = 60,
            [InspectorName("120 FPS")]
            FPS_120 = 120,
        }

        // 屏幕休眠策略（主要针对移动设备）
        public enum SleepPolicy
        {
            [InspectorName("跟随系统设置")]
            SystemSetting,
            [InspectorName("永不休眠")]
            NeverSleep,
        }

        #endregion

        #region 序列化字段

        [Header("屏幕要求")]
        [Tooltip("设置该应用程序对屏幕分辨率的要求等级。\n" +
                 "• 无要求：不做任何分辨率/宽高比检查\n" +
                 "• 宽高比要求：运行时会持续检查宽高比是否匹配\n" +
                 "• 准确分辨率要求：运行时会持续检查分辨率是否完全匹配")]
        public ScreenRequirementLevel screenRequirement = ScreenRequirementLevel.None;

        [Tooltip("设计宽高比（仅在「宽高比要求」模式下需要手动设置）。\n" +
                 "例：16:9 填入 (16, 9)，32:9 填入 (32, 9)")]
        public Vector2 designAspectRatio = new Vector2(16, 9);

        [Tooltip("设计分辨率（仅在「准确分辨率要求」模式下需要设置）。\n" +
                 "系统会自动从此值推导出宽高比。")]
        public Vector2Int designResolution = new Vector2Int(1920, 1080);

        [Header("帧率")]
        [Tooltip("应用程序的目标帧率。\n" +
                 "• 不设置：保持 Unity 默认行为\n" +
                 "• 不限制 (-1)：尽可能快地渲染\n" +
                 "• 其他值：限制到指定帧率")]
        public FrameRateType targetFrameRate = FrameRateType.DontCare;

        [Header("移动设备")]
        [Tooltip("控制移动设备的屏幕休眠行为。\n" +
                 "展览、演示类应用通常应选择「永不休眠」。")]
        public SleepPolicy sleepPolicy = SleepPolicy.NeverSleep;

        [Header("桌面端")]
        [Tooltip("启动后若检测到窗口模式，自动按照屏幕要求中的宽高比计算最大合适尺寸并设置窗口分辨率。\n" +
                 "适用于在普通显示器上测试为异形宽高比设备开发的应用程序。\n" +
                 "仅 Standalone 平台有效。")]
        public bool autoWindowSize = false;

        [Tooltip("启动时需要激活的显示器数量。\n" +
                 "1 = 仅主显示器，大于 1 则自动激活对应数量的额外显示器。\n" +
                 "仅 Standalone 平台有效。")]
        [Range(1, 8)]
        public int activeDisplayCount = 1;

        [Header("其他")]
        [Tooltip("打包后启动时自动切换到指定场景（按 Build Settings 中的索引）。\n" +
                 "-1 表示不自动切换。仅在非编辑器模式下生效。")]
        public int autoSwitchSceneIndex = -1;

        #endregion

        #region 运行时属性

        // 当前屏幕是否满足分辨率要求
        public bool IsResolutionMatch
        {
            get
            {
                if (screenRequirement != ScreenRequirementLevel.ExactResolution) return true;
                return Screen.width == designResolution.x && Screen.height == designResolution.y;
            }
        }

        // 当前屏幕是否满足宽高比要求
        public bool IsAspectRatioMatch
        {
            get
            {
                if (screenRequirement == ScreenRequirementLevel.None) return true;
                Vector2 ratio = EffectiveAspectRatio;
                if (ratio.x <= 0 || ratio.y <= 0) return true;
                float targetAspect = ratio.x / ratio.y;
                float currentAspect = (float)Screen.width / Screen.height;
                return Mathf.Abs(currentAspect - targetAspect) < 0.01f;
            }
        }

        // 根据当前屏幕要求等级，获取生效的宽高比
        public Vector2 EffectiveAspectRatio
        {
            get
            {
                switch (screenRequirement)
                {
                    case ScreenRequirementLevel.AspectRatio:
                        return designAspectRatio;
                    case ScreenRequirementLevel.ExactResolution:
                        // 从分辨率推导宽高比
                        if (designResolution.x > 0 && designResolution.y > 0)
                            return new Vector2(designResolution.x, designResolution.y);
                        return Vector2.zero;
                    default:
                        return Vector2.zero;
                }
            }
        }

        #endregion

        #region 生命周期

        void Start()
        {
            ApplySleepPolicy();
            ApplyFrameRate();
            ActivateDisplays();
            ApplyAutoWindowSize();
            ApplyAutoSwitchScene();

            // 如果屏幕要求不满足，启动持续检测协程
            if (!IsResolutionMatch || !IsAspectRatioMatch)
            {
                Debug.LogWarning($"[ScreenSetting] 当前屏幕不满足要求，启动持续检测。");
                StartCoroutine(ScreenRequirementCheckRoutine());
            }
        }

        #endregion

        #region 各功能实现

        private void ApplySleepPolicy()
        {
            Screen.sleepTimeout = sleepPolicy == SleepPolicy.NeverSleep
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
            Debug.Log($"[ScreenSetting] 屏幕休眠策略: {sleepPolicy}");
        }

        private void ApplyFrameRate()
        {
            if (targetFrameRate == FrameRateType.DontCare) return;
            Application.targetFrameRate = (int)targetFrameRate;
            Debug.Log($"[ScreenSetting] 目标帧率: {(int)targetFrameRate}");
        }

        private void ActivateDisplays()
        {
            if (activeDisplayCount <= 1) return;

            // 仅 Standalone 平台
            if (!IsStandalonePlatform()) return;

            int available = Display.displays.Length;
            int toActivate = Mathf.Min(activeDisplayCount, available);

            for (int i = 1; i < toActivate; i++)
            {
                Display.displays[i].Activate();
                Debug.Log($"[ScreenSetting] 已激活显示器 {i} ({Display.displays[i].systemWidth}x{Display.displays[i].systemHeight})");
            }

            if (activeDisplayCount > available)
                Debug.LogWarning($"[ScreenSetting] 请求激活 {activeDisplayCount} 个显示器，但系统仅检测到 {available} 个");
        }

        private void ApplyAutoWindowSize()
        {
            if (!autoWindowSize) return;
            if (!IsStandalonePlatform()) return;
            if (Screen.fullScreen)
            {
                Debug.Log("[ScreenSetting] 当前是全屏模式，跳过自动窗口尺寸调整");
                return;
            }

            // 确定要使用的宽高比
            float aspect;
            Vector2 ratio = EffectiveAspectRatio;
            if (ratio.x > 0 && ratio.y > 0)
            {
                aspect = ratio.x / ratio.y;
            }
            else
            {
                // 没有指定宽高比要求时，使用当前窗口宽高比
                aspect = (float)Screen.width / Screen.height;
            }

            int screenW = Screen.currentResolution.width;
            int screenH = Screen.currentResolution.height;

            // 预留像素：窗口标题栏 + 任务栏/Dock + 边框
            const int reserveTop = 40;
            const int reserveBottom = 70;
            const int reserveHorizontal = 16;
            int availableW = screenW - reserveHorizontal * 2;
            int availableH = screenH - reserveTop - reserveBottom;

            Vector2Int useResolution;
            if ((float)availableW / availableH > aspect)
            {
                // 可用区域偏宽，以高度为基准
                useResolution = new Vector2Int((int)(availableH * aspect), availableH);
            }
            else
            {
                // 可用区域偏高，以宽度为基准
                useResolution = new Vector2Int(availableW, (int)(availableW / aspect));
            }

            Debug.Log($"[ScreenSetting] 自动窗口尺寸 → 桌面:{screenW}x{screenH} 可用:{availableW}x{availableH} " +
                      $"宽高比:{aspect:F3} → 窗口:{useResolution.x}x{useResolution.y}");
            Screen.SetResolution(useResolution.x, useResolution.y, false);
        }

        private void ApplyAutoSwitchScene()
        {
            if (autoSwitchSceneIndex < 0) return;
            if (Application.isEditor) return;

            Debug.Log($"[ScreenSetting] 自动切换到场景索引 {autoSwitchSceneIndex}");
            SceneManager.LoadScene(autoSwitchSceneIndex);
        }

        private IEnumerator ScreenRequirementCheckRoutine()
        {
            // 持续检测屏幕要求是否满足，每秒检查一次
            while (true)
            {
                if (!IsResolutionMatch)
                {
                    Debug.LogWarning($"[ScreenSetting] 分辨率不匹配 → 要求:{designResolution.x}x{designResolution.y} " +
                                     $"当前:{Screen.width}x{Screen.height}");
                }

                if (!IsAspectRatioMatch)
                {
                    Vector2 ratio = EffectiveAspectRatio;
                    float target = ratio.x / ratio.y;
                    float current = (float)Screen.width / Screen.height;
                    Debug.LogWarning($"[ScreenSetting] 宽高比不匹配 → 要求:{ratio.x}:{ratio.y} ({target:F3}) " +
                                     $"当前:{Screen.width}:{Screen.height} ({current:F3})");
                }

                // 如果已经满足所有要求，停止检测
                if (IsResolutionMatch && IsAspectRatioMatch)
                {
                    Debug.Log("[ScreenSetting] 屏幕要求已满足，停止检测");
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private static bool IsStandalonePlatform()
        {
            return Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.OSXPlayer
                || Application.platform == RuntimePlatform.LinuxPlayer;
        }

        #endregion

        #region IDebuggerCustomWindowGUI

        public bool IsVisible =>
            screenRequirement == ScreenRequirementLevel.ExactResolution
            && designResolution.x > 0 && designResolution.y > 0;

        public void OnDrawDebuggerGUI()
        {
            GUILayout.Label($"当前分辨率: {Screen.width}x{Screen.height}");
            GUILayout.Label($"设计分辨率: {designResolution.x}x{designResolution.y}");
            GUILayout.Label($"分辨率匹配: {IsResolutionMatch}  宽高比匹配: {IsAspectRatioMatch}");

            if (designResolution.x > 0 && designResolution.y > 0
                && GUILayout.Button("设置为设计分辨率"))
            {
                Screen.SetResolution(designResolution.x, designResolution.y, Screen.fullScreen);
            }
        }

        #endregion
    }
}
