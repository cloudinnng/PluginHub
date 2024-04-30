using System.Collections;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Runtime
{
//https://mp.weixin.qq.com/s?__biz=MzkyMTM5Mjg3NQ==&mid=2247536007&idx=1&sn=a4d1b41637880fce3e932f610e3f4418&source=41#wechat_redirect
    public class ScreenSetting : MonoBehaviour
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
    }
}