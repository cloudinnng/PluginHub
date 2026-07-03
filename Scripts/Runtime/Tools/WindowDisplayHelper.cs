using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 可以直接设置应用窗口分辨率和是否显示标题 （可让应用窗口运行没有标题栏）
    ///
    /// 仅适用于windows非编辑器下
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>Win32 ShowWindow：最小化窗口</summary>
        private const int SW_MINIMIZE = 6;

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

        /// <summary>
        /// 读取当前 Unity 应用程序窗口的屏幕坐标与尺寸（Windows 独立构建有效）。
        /// </summary>
        public static bool TryGetApplicationWindowRect(out int x, out int y, out int width, out int height)
        {
            x = 0;
            y = 0;
            width = 0;
            height = 0;

            if (Application.platform == RuntimePlatform.WindowsEditor)
                return false;

            if (Application.platform != RuntimePlatform.WindowsPlayer)
                return false;

            IntPtr hwnd = GetActiveWindow();
            if (hwnd == IntPtr.Zero)
                hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                Debug.LogWarning("[WindowDisplayHelper] TryGetApplicationWindowRect: 无法获取窗口句柄。");
                return false;
            }

            if (!GetWindowRect(hwnd, out RECT rect))
            {
                Debug.LogWarning($"[WindowDisplayHelper] TryGetApplicationWindowRect: GetWindowRect 失败，hwnd={hwnd}");
                return false;
            }

            x = rect.Left;
            y = rect.Top;
            width = rect.Right - rect.Left;
            height = rect.Bottom - rect.Top;
            Debug.Log($"[WindowDisplayHelper] TryGetApplicationWindowRect: ({x}, {y}) {width}x{height}");
            return true;
        }

        /// <summary>
        /// 最小化当前 Unity 应用程序窗口（Windows 独立构建有效，编辑器内无效果）
        /// </summary>
        public static void MinimizeApplicationWindow()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Debug.Log("[WindowDisplayHelper] 编辑器模式下无法最小化 Player 窗口，请在 Windows 独立构建中测试。");
                return;
            }

            if (Application.platform != RuntimePlatform.WindowsPlayer)
            {
                Debug.LogWarning($"[WindowDisplayHelper] 最小化窗口仅支持 Windows 平台，当前平台: {Application.platform}");
                return;
            }

            // 优先取当前线程活动窗口（IMGUI 按钮点击时更可靠），失败则回退到前台窗口
            IntPtr hwnd = GetActiveWindow();
            if (hwnd == IntPtr.Zero)
            {
                hwnd = GetForegroundWindow();
                Debug.LogWarning("[WindowDisplayHelper] GetActiveWindow 返回空句柄，已回退到 GetForegroundWindow。");
            }

            bool success = ShowWindow(hwnd, SW_MINIMIZE);
            Debug.Log($"[WindowDisplayHelper] 调用 ShowWindow(SW_MINIMIZE)，hwnd={hwnd}, success={success}");
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