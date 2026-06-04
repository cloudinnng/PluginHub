using System.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 模仿安卓手机的 Toast：屏幕底部居中、半透明黑底白字，一段时间后自动渐隐消失。
    /// - 新 Toast 立刻覆盖旧 Toast（停止旧动画，强制重置 opacity=0 再渐入）。
    /// - 显示节奏：0.2s 渐入 + DefaultShowTime 全显 + 0.2s 渐出（默认总时长约 3.4s）。
    /// - 字号基准：1280x720 下 30px，按屏幕"对角线长度"等比缩放（每次 Show 重算）。
    /// - 不拦截下层 UI 的点击（root + Label 均设 PickingMode.Ignore）。
    /// - 配套 UXML：PluginHubRuntime.ResolveRelativePath("Scripts/Runtime/UITK/ToastOverlap.uxml")
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class ToastManager : SceneSingleton<ToastManager>
    {
        // 完全显示时长（不含渐入/渐出）
        public float DefaultShowTime = 3f;

        public bool testToastUseSpace = false;

        [SerializeField]
        private PanelSettings panelSettings;
        [SerializeField]
        private VisualTreeAsset toastVisualTreeAsset;

        // 渐入/渐出动画时长（必须与 UXML 里 transition-duration 保持一致）
        private const float FadeDuration = .2f;

        // 字号基准：1280x720 设计分辨率下 Label 文字 30px
        private const float BaseFontSize = 30f;

        // 基准对角线长度，约 1468.6
        private static readonly float BaseDiagonal = Mathf.Sqrt(1280f * 1280f + 720f * 720f);

        // UXML 中 Label 的 name
        private const string ToastLabelName = "ToastLabel";

        private UIDocument uiDocument;
        private Label toastLabel;

        // 当前显示流程的协程，便于被新 Show 打断
        private Coroutine showCoroutine;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }
            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = toastVisualTreeAsset;
            uiDocument.sortingOrder = 9999;
        }

        private void OnEnable()
        {
            // UIDocument 在 OnEnable 时才保证 rootVisualElement 可用
            var root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("[ToastManager] rootVisualElement 为空，请检查 UIDocument.sourceAsset 是否指向 ToastOverlap.uxml");
                return;
            }

            // 整个 Toast 面板都不挡点击：root 和它所有子元素
            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                child.pickingMode = PickingMode.Ignore;

            toastLabel = root.Q<Label>(ToastLabelName);
            if (toastLabel == null)
            {
                Debug.LogError($"[ToastManager] 在 UXML 中找不到 name=\"{ToastLabelName}\" 的 Label，请检查 ToastOverlap.uxml");
                return;
            }

            // 双保险：Label 也不挡点击
            toastLabel.pickingMode = PickingMode.Ignore;
            toastLabel.text = string.Empty;
            toastLabel.style.opacity = 0f;

        }

        void Update()
        {
            if (testToastUseSpace)
            {
                if (InputEx.GetKeyDown(KeyCode.Space))
                {
                    Show($"Tick: {Time.time:F2}s");
                }
            }
        }

        public void Show(string text)
        {
            if (toastLabel == null)
            {
                Debug.LogWarning("[ToastManager] toastLabel 未绑定，无法显示 Toast。请确认场景中 ToastManager 已启用且 UXML 配置正确。");
                return;
            }

            // 1) 立刻打断旧的显示协程
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }

            // 2) 立刻替换文本
            toastLabel.text = text;

            // 3) 按当前屏幕对角线重算字号（运行时屏幕尺寸可能变化）
            UpdateFontSize(toastLabel);

            // 4) 关键：强制把 opacity 拉回 0，等下一帧再设 1 才能触发完整的 1s 渐入 transition
            //    （UI Toolkit 的 transition 不会从"目标值 -> 目标值"插值，必须有变化才会触发）
            toastLabel.style.opacity = 0f;

            Debug.Log($"[ToastManager] Show: \"{text}\", visibleTime={DefaultShowTime}s, fontSize={toastLabel.resolvedStyle.fontSize:F1}px");

            showCoroutine = StartCoroutine(ShowRoutine(DefaultShowTime));
        }

        /// <summary>
        /// 显示流程：渐入 -> 全显 -> 渐出。期间被打断会由 Show() 重新启动一条新协程。
        /// </summary>
        private IEnumerator ShowRoutine(float visibleTime)
        {
            // 等一帧让 opacity=0 真正落到样式上
            yield return null;

            // 触发渐入 transition
            // 直接设置 opacity=1f（UI Toolkit 的 style.opacity 若设置 transition-duration，会自动进行淡入动画）
            toastLabel.style.opacity = 1f;

            // 等"渐入完成 + 完全显示"时长
            yield return new WaitForSeconds(FadeDuration + visibleTime);

            // 触发渐出 transition
            toastLabel.style.opacity = 0f;

            // 等渐出动画走完，避免协程提前结束
            yield return new WaitForSeconds(FadeDuration);

            showCoroutine = null;
        }

        /// <summary>
        /// 按对角线长度等比缩放字号：fontSize = 30 * (currentDiagonal / 1468.6)
        /// </summary>
        private static void UpdateFontSize(Label label)
        {
            float w = Screen.width;
            float h = Screen.height;
            float currentDiagonal = Mathf.Sqrt(w * w + h * h);
            float scale = currentDiagonal / BaseDiagonal;
            float fontSize = BaseFontSize * scale;
            label.style.fontSize = new StyleLength(new Length(fontSize, LengthUnit.Pixel));
        }
    }
}
