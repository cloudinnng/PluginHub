// WinClipboard.cs
// 用法：WinClipboard.CopyFiles(new[]{ @"C:\temp\a.png", @"D:\b.txt" });
using System;
using System.Runtime.InteropServices;
using System.Text;

// 适合使用程序复制Zip文件，然后方便的粘贴到微信等其他软件
public static class WinClipboard
{
    const uint CF_HDROP = 15;
    const uint GMEM_MOVEABLE = 0x0002;
    const uint GMEM_ZEROINIT  = 0x0040;
    const int  GHND = (int)(GMEM_MOVEABLE | GMEM_ZEROINIT);

    [StructLayout(LayoutKind.Sequential)]
    struct DROPFILES
    {
        public uint pFiles;      // 从内存块起始到文件列表起始的偏移
        public int  pt_x;
        public int  pt_y;
        public bool fNC;
        public bool fWide;       // true=Unicode
    }

    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll")] static extern bool EmptyClipboard();
    [DllImport("user32.dll")] static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll")] static extern IntPtr GlobalAlloc(int uFlags, UIntPtr dwBytes);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")] static extern bool   GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll")] static extern UIntPtr GlobalSize(IntPtr hMem);

    // 注册并设置 "Preferred DropEffect" = DROPEFFECT_COPY
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint RegisterClipboardFormat(string lpszFormat);

    public static void CopyFiles(string[] paths)
    {
        if (paths == null || paths.Length == 0) return;

        // 构造双 0 结尾的 Unicode 路径块： "C:\a.txt\0D:\b.png\0\0"
        var sb = new StringBuilder();
        foreach (var p in paths) { sb.Append(p); sb.Append('\0'); }
        sb.Append('\0');
        var fileListBytes = Encoding.Unicode.GetBytes(sb.ToString());

        // 计算总大小：DROPFILES 结构 + 文件名块
        int headerSize = Marshal.SizeOf(typeof(DROPFILES));
        int totalSize  = headerSize + fileListBytes.Length;

        IntPtr hGlobal = GlobalAlloc(GHND, (UIntPtr)totalSize);
        if (hGlobal == IntPtr.Zero) return;

        IntPtr ptr = GlobalLock(hGlobal);
        try
        {
            // 写入 DROPFILES 头
            var df = new DROPFILES
            {
                pFiles = (uint)headerSize,
                pt_x = 0, pt_y = 0,
                fNC = false,
                fWide = true
            };
            Marshal.StructureToPtr(df, ptr, false);

            // 写入文件名块
            IntPtr filesPtr = IntPtr.Add(ptr, headerSize);
            Marshal.Copy(fileListBytes, 0, filesPtr, fileListBytes.Length);
        }
        finally { GlobalUnlock(hGlobal); }

        // 开剪贴板并设置 CF_HDROP
        if (OpenClipboard(IntPtr.Zero))
        {
            try
            {
                EmptyClipboard();
                SetClipboardData(CF_HDROP, hGlobal);
                // hGlobal 所有权转给剪贴板，调用方不再释放

                // 设置 Preferred DropEffect = COPY
                uint fmt = RegisterClipboardFormat("Preferred DropEffect");
                IntPtr hPref = GlobalAlloc(GHND, (UIntPtr)4);
                IntPtr pPref = GlobalLock(hPref);
                try
                {
                    // DWORD 1 = DROPEFFECT_COPY
                    Marshal.WriteInt32(pPref, 1);
                }
                finally { GlobalUnlock(hPref); }
                SetClipboardData(fmt, hPref);
            }
            finally { CloseClipboard(); }
        }
    }
}
