// WinClipboard.cs
// 用法：WinClipboard.CopyFiles(new[]{ @"C:\temp\a.png", @"D:\b.txt" });
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// 完全还原Windows资源管理器复制文件的底层API调用
// 只使用CF_HDROP和Preferred DropEffect，与Windows Explorer行为一致
public static class WinClipboard
{
    // 剪切板格式常量
    const uint CF_HDROP = 15;  // 标准文件拖放格式
    
    // 内存分配标志
    const uint GMEM_MOVEABLE = 0x0002;   // 可移动内存块
    const uint GMEM_ZEROINIT = 0x0040;   // 初始化为0
    const uint GHND = GMEM_MOVEABLE | GMEM_ZEROINIT;  // 组合标志
    
    // DropEffect 常量
    const int DROPEFFECT_COPY = 1;       // 复制操作
    const int DROPEFFECT_MOVE = 2;       // 移动操作

    // DROPFILES 结构 - 必须与Windows API完全一致
    // 关键：Pack=1 确保没有填充字节，与Windows API定义严格匹配
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DROPFILES
    {
        public uint pFiles;      // 从结构起始到文件列表的字节偏移（DWORD，4字节）
        public int pt_x;         // 拖放点X坐标（LONG，4字节）
        public int pt_y;         // 拖放点Y坐标（LONG，4字节）
        public int fNC;          // 非客户区标志（BOOL，4字节，0=客户区）
        public int fWide;        // Unicode标志（BOOL，4字节，1=Unicode）
    }

    // User32.dll API - 剪切板操作
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool CloseClipboard();
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EmptyClipboard();
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    // Kernel32.dll API - 内存管理
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalLock(IntPtr hMem);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalUnlock(IntPtr hMem);

    // 注册自定义剪切板格式
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint RegisterClipboardFormat(string lpszFormat);

    // Shell32.dll API - Shell操作
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr ILCreateFromPath(string pszPath);
    
    [DllImport("shell32.dll")]
    static extern void ILFree(IntPtr pidl);
    
    [DllImport("shell32.dll")]
    static extern uint ILGetSize(IntPtr pidl);

    /// <summary>
    /// 将文件路径数组复制到Windows剪切板
    /// 完全模拟Windows资源管理器的复制行为
    /// </summary>
    public static bool CopyFiles(string[] paths)
    {
        if (paths == null || paths.Length == 0) return false;

        // 验证并规范化所有路径
        string[] normalizedPaths = new string[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(paths[i]))
                return false;
            
            try
            {
                // 规范化路径：转换为绝对路径，统一路径分隔符
                string fullPath = Path.GetFullPath(paths[i]);
                
                // 验证文件或目录是否存在
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    return false;
                
                normalizedPaths[i] = fullPath;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            // 尝试打开剪切板（重试机制，因为剪切板可能被其他程序占用）
            int retryCount = 10;
            while (retryCount > 0)
            {
                if (OpenClipboard(IntPtr.Zero))
                    break;
                    
                System.Threading.Thread.Sleep(10);
                retryCount--;
            }

            if (retryCount == 0)
                return false;

            try
            {
                // 清空剪切板
                if (!EmptyClipboard())
                    return false;

                // 1. 设置 CF_HDROP 格式（必需）
                if (!SetCF_HDROP(normalizedPaths))
                    return false;

                // 2. 设置 Preferred DropEffect（可选但推荐）
                SetPreferredDropEffect(DROPEFFECT_COPY);

                // 3. 设置 Shell IDList Array（微信等应用可能需要）
                SetShellIDListArray(normalizedPaths);

                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置 CF_HDROP 格式
    /// 这是Windows文件复制的核心格式
    /// </summary>
    static bool SetCF_HDROP(string[] paths)
    {
        // 构建文件路径列表：每个路径以\0结尾，整个列表以\0\0结尾
        // 示例: "C:\file1.txt\0C:\file2.txt\0\0"
        var sb = new StringBuilder();
        foreach (var path in paths)
        {
            sb.Append(path);
            sb.Append('\0');
        }
        sb.Append('\0');  // 额外的\0作为列表结束标记

        // 使用Unicode编码（Windows推荐）
        byte[] pathBytes = Encoding.Unicode.GetBytes(sb.ToString());

        // 计算所需内存大小
        int dropFilesSize = Marshal.SizeOf(typeof(DROPFILES));
        int totalSize = dropFilesSize + pathBytes.Length;

        // 分配全局内存
        IntPtr hGlobal = GlobalAlloc(GHND, (UIntPtr)totalSize);
        if (hGlobal == IntPtr.Zero)
            return false;

        try
        {
            // 锁定内存以写入数据
            IntPtr pGlobal = GlobalLock(hGlobal);
            if (pGlobal == IntPtr.Zero)
            {
                // GlobalAlloc 成功但 GlobalLock 失败（极少见）
                return false;
            }

            try
            {
                // 填充 DROPFILES 结构
                DROPFILES dropFiles = new DROPFILES
                {
                    pFiles = (uint)dropFilesSize,  // 文件列表的偏移量
                    pt_x = 0,                      // 不使用拖放点
                    pt_y = 0,
                    fNC = 0,                       // 客户区
                    fWide = 1                      // Unicode字符串
                };

                // 将结构写入内存
                Marshal.StructureToPtr(dropFiles, pGlobal, false);

                // 将路径字符串写入结构后的内存位置
                IntPtr pathPtr = IntPtr.Add(pGlobal, dropFilesSize);
                Marshal.Copy(pathBytes, 0, pathPtr, pathBytes.Length);
            }
            finally
            {
                // 解锁内存
                GlobalUnlock(hGlobal);
            }

            // 将数据设置到剪切板
            // 注意：SetClipboardData 成功后，系统接管内存所有权，不需要手动释放
            IntPtr result = SetClipboardData(CF_HDROP, hGlobal);
            if (result == IntPtr.Zero)
                return false;

            // 成功：内存所有权已转移，不要释放 hGlobal
            return true;
        }
        catch
        {
            // 失败：需要释放内存（但GlobalFree不安全，让系统回收）
            return false;
        }
    }

    /// <summary>
    /// 设置 Preferred DropEffect
    /// 指示这是复制操作还是移动操作
    /// </summary>
    static bool SetPreferredDropEffect(int effect)
    {
        // 注册自定义格式
        uint format = RegisterClipboardFormat("Preferred DropEffect");
        if (format == 0)
            return false;

        // 分配4字节内存存储DWORD值
        IntPtr hGlobal = GlobalAlloc(GHND, (UIntPtr)4);
        if (hGlobal == IntPtr.Zero)
            return false;

        try
        {
            IntPtr pGlobal = GlobalLock(hGlobal);
            if (pGlobal == IntPtr.Zero)
                return false;

            try
            {
                // 写入效果值（1=复制，2=移动）
                Marshal.WriteInt32(pGlobal, effect);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            // 设置到剪切板
            IntPtr result = SetClipboardData(format, hGlobal);
            return result != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置 Shell IDList Array
    /// 这是Windows Shell的标准格式，包含文件的PIDL信息
    /// 微信等应用可能需要此格式来正确处理文件
    /// </summary>
    static bool SetShellIDListArray(string[] paths)
    {
        // 注册Shell IDList Array格式
        uint format = RegisterClipboardFormat("Shell IDList Array");
        if (format == 0)
            return false;

        // 为每个路径创建PIDL
        IntPtr[] pidls = new IntPtr[paths.Length];
        uint[] pidlSizes = new uint[paths.Length];
        
        try
        {
            // 创建所有PIDL
            for (int i = 0; i < paths.Length; i++)
            {
                pidls[i] = ILCreateFromPath(paths[i]);
                if (pidls[i] == IntPtr.Zero)
                {
                    // 创建PIDL失败，清理并返回
                    for (int j = 0; j < i; j++)
                        ILFree(pidls[j]);
                    return false;
                }
                pidlSizes[i] = ILGetSize(pidls[i]);
            }

            // 计算CIDA结构大小
            // CIDA格式：UINT cidl + (cidl+1)个UINT偏移 + 所有PIDL数据
            // 注意：第一个PIDL是父文件夹的PIDL，我们使用第一个文件的父目录
            string parentPath = Path.GetDirectoryName(paths[0]);
            IntPtr parentPidl = ILCreateFromPath(parentPath);
            if (parentPidl == IntPtr.Zero)
            {
                for (int i = 0; i < pidls.Length; i++)
                    ILFree(pidls[i]);
                return false;
            }
            
            uint parentPidlSize = ILGetSize(parentPidl);

            try
            {
                // 计算总大小
                uint headerSize = 4; // cidl (UINT)
                uint offsetArraySize = (uint)(4 * (paths.Length + 1)); // (cidl+1) 个偏移
                uint totalPidlSize = parentPidlSize;
                foreach (uint size in pidlSizes)
                    totalPidlSize += size;

                uint totalSize = headerSize + offsetArraySize + totalPidlSize;

                // 分配内存
                IntPtr hGlobal = GlobalAlloc(GHND, (UIntPtr)totalSize);
                if (hGlobal == IntPtr.Zero)
                    return false;

                IntPtr pGlobal = GlobalLock(hGlobal);
                if (pGlobal == IntPtr.Zero)
                    return false;

                try
                {
                    // 写入CIDA结构
                    IntPtr pCurrent = pGlobal;

                    // 1. 写入文件数量 (cidl)
                    Marshal.WriteInt32(pCurrent, paths.Length);
                    pCurrent = IntPtr.Add(pCurrent, 4);

                    // 2. 写入偏移数组
                    uint currentOffset = headerSize + offsetArraySize;
                    
                    // 第一个偏移指向父文件夹PIDL
                    Marshal.WriteInt32(pCurrent, (int)currentOffset);
                    pCurrent = IntPtr.Add(pCurrent, 4);
                    currentOffset += parentPidlSize;

                    // 后续偏移指向每个文件的PIDL
                    for (int i = 0; i < paths.Length; i++)
                    {
                        Marshal.WriteInt32(pCurrent, (int)currentOffset);
                        pCurrent = IntPtr.Add(pCurrent, 4);
                        currentOffset += pidlSizes[i];
                    }

                    // 3. 写入父文件夹PIDL
                    byte[] parentPidlData = new byte[parentPidlSize];
                    Marshal.Copy(parentPidl, parentPidlData, 0, (int)parentPidlSize);
                    Marshal.Copy(parentPidlData, 0, pCurrent, (int)parentPidlSize);
                    pCurrent = IntPtr.Add(pCurrent, (int)parentPidlSize);

                    // 4. 写入每个文件的PIDL
                    for (int i = 0; i < paths.Length; i++)
                    {
                        byte[] pidlData = new byte[pidlSizes[i]];
                        Marshal.Copy(pidls[i], pidlData, 0, (int)pidlSizes[i]);
                        Marshal.Copy(pidlData, 0, pCurrent, (int)pidlSizes[i]);
                        pCurrent = IntPtr.Add(pCurrent, (int)pidlSizes[i]);
                    }
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                // 设置到剪切板
                IntPtr result = SetClipboardData(format, hGlobal);
                return result != IntPtr.Zero;
            }
            finally
            {
                ILFree(parentPidl);
            }
        }
        finally
        {
            // 释放所有PIDL
            for (int i = 0; i < pidls.Length; i++)
            {
                if (pidls[i] != IntPtr.Zero)
                    ILFree(pidls[i]);
            }
        }
    }
}
