using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    // 用于代码性能测试，支持嵌套使用
    // 用法1（基础用法）:
    // PerformanceTest.Start();
    // // 你的代码
    // PerformanceTest.End("代码执行时间");
    //
    // 用法2（嵌套用法，使用标签配对）:
    // PerformanceTest.Start("外层");
    // // 外层代码...
    //     PerformanceTest.Start("内层");
    //     // 内层代码...
    //     PerformanceTest.End("内层");
    // // 外层代码继续...
    // PerformanceTest.End("外层");
    public class PerformanceTest
    {
        // 默认标签，用于向后兼容无标签调用
        private const string DefaultLabel = "__default__";

        // 使用字典存储多个计时器，以标签为键
        private static Dictionary<string, System.Diagnostics.Stopwatch> stopwatches =
            new Dictionary<string, System.Diagnostics.Stopwatch>();

        /// <summary>
        /// 开始计时（无标签，向后兼容）
        /// </summary>
        public static void Start()
        {
            Start(DefaultLabel);
        }

        /// <summary>
        /// 开始计时，使用指定标签
        /// </summary>
        /// <param name="label">计时器标签，用于标识和配对Start/End调用</param>
        public static void Start(string label)
        {
            if (string.IsNullOrEmpty(label))
                label = DefaultLabel;

            // 如果该标签的计时器不存在，则创建新的
            if (!stopwatches.TryGetValue(label, out var stopwatch))
            {
                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatches[label] = stopwatch;
            }

            stopwatch.Reset();
            stopwatch.Start(); // 开始监视代码运行时间
        }

        /// <summary>
        /// 结束计时（无标签，向后兼容）
        /// </summary>
        /// <param name="testTitle">输出的测试标题</param>
        /// <param name="msThreshold">毫秒阈值，执行时间大于此值才会打印</param>
        public static void End(string testTitle = "", float msThreshold = 0)
        {
            EndInternal(testTitle, testTitle, msThreshold);
        }

        /// <summary>
        /// 结束计时，使用指定标签配对
        /// </summary>
        /// <param name="label">计时器标签，必须与Start时使用的标签一致</param>
        /// <param name="testTitle">输出的测试标题，如果为空则使用标签作为标题</param>
        /// <param name="msThreshold">毫秒阈值，执行时间大于此值才会打印</param>
        public static void End(string label, string testTitle, float msThreshold = 0)
        {
            EndInternal(label, testTitle, msThreshold);
        }

        /// <summary>
        /// 内部结束计时方法
        /// </summary>
        private static void EndInternal(string label, string testTitle, float msThreshold)
        {
            if (string.IsNullOrEmpty(label))
                label = DefaultLabel;

            if (!stopwatches.TryGetValue(label, out var stopwatch))
            {
                Debug.LogWarning($"[PerformanceTest] 未找到标签为 '{label}' 的计时器，请确保已调用对应的 Start(\"{label}\")");
                return;
            }

            stopwatch.Stop(); // 停止监视

            double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            // 如果没有提供testTitle，使用标签作为标题
            if (string.IsNullOrEmpty(testTitle))
                testTitle = label == DefaultLabel ? "代码段" : label;

            if (elapsedMilliseconds > msThreshold)
            {
                // 打印代码执行时间
                Debug.Log($"[PerformanceTest] {testTitle} takes {elapsedMilliseconds:F3} ms (> Threshold:{msThreshold} ms)");
            }

            // 可选：从字典中移除已完成的计时器以释放内存
            // stopwatches.Remove(label);
        }

        /// <summary>
        /// 清除所有计时器
        /// </summary>
        public static void ClearAll()
        {
            foreach (var sw in stopwatches.Values)
            {
                sw.Stop();
            }
            stopwatches.Clear();
        }

        /// <summary>
        /// 获取当前活跃的计时器数量
        /// </summary>
        public static int ActiveTimerCount => stopwatches.Count;
    }
}