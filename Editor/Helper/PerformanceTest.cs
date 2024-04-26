using UnityEngine;

namespace PluginHub.Helper
{
    // 用于代码性能测试
    // 用法:
    // PerformanceTest.Start();
    // // 你的代码
    // PerformanceTest.End("代码执行时间");
    public class PerformanceTest
    {
        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        public static void Start()
        {
            stopwatch.Reset();
            stopwatch.Start(); //  开始监视代码运行时间
        }

        //msThreshold毫秒阈值，大于才会打印
        public static void End(string testTitle = "",float msThreshold = 0)
        {
            stopwatch.Stop(); //  停止监视

            double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsedMilliseconds > msThreshold)
            {
                //打印代码执行时间
                Debug.Log($"{testTitle} takes {elapsedMilliseconds} ms");
            }
        }
    }
}