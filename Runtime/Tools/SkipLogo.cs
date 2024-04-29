using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;


#if PH_SKIPLOGO
//跳过Unity的Splash Logo  在项目中定义CF_SKIPLOGO脚本符号以使用该功能
namespace Cloudinnng.CFramework
{
    [Preserve]
    public class SkipLogo
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void BeforeSplashScreen()
        {
#if UNITY_EDITOR
        
#else
#if UNITY_WEBGL
        Application.focusChanged += Application_focusChanged;
#else
            System.Threading.Tasks.Task.Run(AsyncSkip);
#endif
#endif

        }

#if UNITY_WEBGL
        private static void Application_focusChanged(bool obj)
        {
            Application.focusChanged -= Application_focusChanged;
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
        }
#else
        private static void AsyncSkip()
        {
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
        }
#endif
    }
}
#endif