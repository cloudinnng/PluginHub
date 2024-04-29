
namespace Cloudinnng.CFramework
{
    public partial class Debugger
    {

        /// <summary>
        /// 调试器窗口需要实现这个接口
        /// </summary>
        private interface IDebuggerWindow
        {
            void OnStart();
            void OnDraw();
        }
    }
}