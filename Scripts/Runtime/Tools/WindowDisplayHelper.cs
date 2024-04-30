using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 可以直接设置应用窗口和是否显示标题 可让应用窗口运行 没有标题栏
    ///
    /// 适用于windows
    ///
    /// 2021年7月19日 是否可以直接用Screen.SetResolution方法代替该类
    /// 2023年3月27日 若需要带标题栏的窗口，可以用Screen.SetResolution方法代替该类
    ///
    /// </summary>
    public static class WindowDisplayHelper
    {

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // not used rigth now
        //const uint SWP_NOMOVE = 0x2;
        //const uint SWP_NOSIZE = 1;
        //const uint SWP_NOZORDER = 0x4;
        //const uint SWP_HIDEWINDOW = 0x0080;

        const uint SWP_SHOWWINDOW = 0x0040;
        const int GWL_STYLE = -16;
        const int WS_BORDER = 1;
        const int WS_POPUP = 0x800000;
        const int WS_CAPTION = 0xC00000;

        /// <summary>
        /// 设置应用以窗口化全屏显示
        /// 必须显示器的分辨率有这么大，调用才有效果
        /// 编辑器模式下无效果
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="resolution"></param>
        public static void WithoutTitle(int x, int y, int width, int height)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor) return;

            Screen.SetResolution(width, height, false);
            SetWindowLong(GetForegroundWindow(), GWL_STYLE, WS_BORDER);
            SetWindowPos(GetForegroundWindow(), 0, x, y, width, height, SWP_SHOWWINDOW);
        }

        public static void WithTitle(int width, int height)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor) return;

            Screen.SetResolution(width, height, false);
            SetWindowLong(GetForegroundWindow(), GWL_STYLE, WS_CAPTION);
            SetWindowPos(GetForegroundWindow(), 0, 0, 0, width, height, SWP_SHOWWINDOW);
        }

        public static void SetWindowRect(int x, int y, int width, int height)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor) return;

            SetWindowPos(GetForegroundWindow(), 0, x, y, width, height, SWP_SHOWWINDOW);
        }

        //设置为无标题的窗口模式
        public static void SetNoTitle(int x, int y, int width, int height)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor) return;

            SetWindowLong(GetForegroundWindow(), GWL_STYLE, WS_BORDER);
            SetWindowPos(GetForegroundWindow(), 0, x, y, width, height, SWP_SHOWWINDOW);
        }
        // //设置为有标题的窗口模式
        // public static void SetWithTitle(int x,int y,int width,int height)
        // {
        //     if(Application.platform==RuntimePlatform.WindowsEditor)return;
        //
        //     SetWindowLong(GetForegroundWindow(), GWL_STYLE, WS_CAPTION);
        //     SetWindowPos(GetForegroundWindow(), 0, 0, 0, width, height, SWP_SHOWWINDOW);
        // }

    }
}