using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PluginHub.Runtime.Debugger.CustomWindow;

namespace PluginHub.Runtime
{
//https://mp.weixin.qq.com/s?__biz=MzkyMTM5Mjg3NQ==&mid=2247536007&idx=1&sn=a4d1b41637880fce3e932f610e3f4418&source=41#wechat_redirect
    public class ScreenSetting : MonoBehaviour, IDebuggerCustomWindowGUI
    {
        public enum FrameRateType
        {
            DontCare = 0, //不会设置帧率
            FPS_N1 = -1,
            FPS_10 = 10,
            FPS_30 = 30,
            FPS_50 = 50,
            FPS_60 = 60,
            FPS_120 = 120,
            FPS_9999 = 9999,
        }


        [Tooltip("屏幕不休眠（针对移动设备）")] public bool screenNeverSleep = true;

        [Tooltip("帧率限制")] public FrameRateType Rate = FrameRateType.DontCare;
        private float currentFrameTime;

        [Tooltip("自动切换场景（-1为不切换）,只在发布后生效")] public int changeSceneOnStart = -1;

        [Tooltip("启动程序后，如果目前是窗口模式，则保持窗口宽高比并自动使用合适的较大分辨率显示。这在使用窗口模式以多种平台下测试应用程序时非常方便有用,仅Standalone平台有效")]
        public bool autoWindowSize = false;

        [Tooltip("设计分辨率,若有值,则系统会在没有运行在此分辨率的情况下,向用户做出提示")]
        public Vector2Int designResolution = Vector2Int.zero;

        void Start()
        {
            Screen.sleepTimeout = screenNeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

            //Windows
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor)
            {
                Application.targetFrameRate = 9999;
                targetFrameRate = (int)Rate;
                if (targetFrameRate <= 0)
                    targetFrameRate = 9999;
                StartCoroutine("WaitForNextFrame");
            }
            else
            {
                //Other Platform
                if (Rate != FrameRateType.DontCare)
                    Application.targetFrameRate = (int)Rate;
                Destroy(this);
            }

            if (changeSceneOnStart != -1 && !Application.isEditor)
            {
                Debug.Log($"启动了自动切换场景，将切换到场景{changeSceneOnStart}");
                SceneManager.LoadScene(changeSceneOnStart);
            }

            if (autoWindowSize && Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                if (Screen.fullScreen == true) // 全屏模式下不进行调整
                    return;
                // 刚进入程序时的宽高比
                float aspect = (float)Screen.width / Screen.height;
                // 桌面分辨率
                Vector2Int screenResolution = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
                Vector2Int useResolution = new Vector2Int();
                if (aspect > 1)
                {
                    // 是一个横屏游戏
                    useResolution.x = screenResolution.x * 6 / 10;
                    useResolution.y = (int)(useResolution.x / aspect);
                }else
                {
                    // 是一个竖屏游戏
                    useResolution.y = screenResolution.y * 9 / 10;
                    useResolution.x = (int)(useResolution.y * aspect);
                }
                Screen.SetResolution(useResolution.x, useResolution.y, false);
            }

            if (designResolution != Vector2Int.zero)
                StartCoroutine(nameof(Runner));
        }

        private IEnumerator Runner()
        {
            while (true)
            {
                if (Screen.width != designResolution.x || Screen.height != designResolution.y)
                    Debug.LogWarning($"系统未在设计分辨率下运行({designResolution.x} x {designResolution.y})");
                yield return new WaitForSeconds(1);
            }
        }


        private static int targetFrameRate = 0;

        IEnumerator WaitForNextFrame()
        {
            currentFrameTime = Time.realtimeSinceStartup;
            while (true)
            {
                yield return new WaitForEndOfFrame();
                currentFrameTime += 1.0f / targetFrameRate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = currentFrameTime - t - 0.01f;
                if (sleepTime > 0)
                    Thread.Sleep((int)(sleepTime * 1000));
                while (t < currentFrameTime)
                    t = Time.realtimeSinceStartup;
            }
        }

        public void OnDrawDebuggerGUI()
        {
            if(designResolution.x > 0 && designResolution.y > 0 && GUILayout.Button("设置为设计分辨率"))
            {
                Screen.SetResolution(designResolution.x,designResolution.y,Screen.fullScreen);
            }
        }
    }
}