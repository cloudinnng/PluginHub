using System.IO;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text;

namespace PluginHub.Editor
{
    public partial class BuildModule : PluginHubModuleBase
    {
        // 获取 BaiduPCS-Go 文件夹在系统中的完整路径
        private static string baiduPCSGoFolderFullPath => Path.GetFullPath("Packages/com.hellottw.pluginhub/Plugins/BaiduPCS-Go");

        private void DrawBaiduPCSSection()
        {
            GUILayout.Label("BaiduPCS-Go:");
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                DrawIconBtnOpenFolder(baiduPCSGoFolderFullPath, true);

                if (GUILayout.Button("登录", GUILayout.ExpandWidth(false)))
                {
                    BaiduPCSLogin();
                }
                if (GUILayout.Button("登出", GUILayout.ExpandWidth(false)))
                {
                    BaiduPCSLogout();
                }

                if (GUILayout.Button("交互模式", GUILayout.ExpandWidth(false)))
                {
                    RunCmd(baiduPCSGoFolderFullPath, "BaiduPCS-Go.exe");
                    Debug.Log("已复制到剪贴板: cd /apps/BaiduPCS-Go");
                    GUIUtility.systemCopyBuffer = "cd /apps/BaiduPCS-Go";
                }

                if (GUILayout.Button("分享（有问题）", GUILayout.ExpandWidth(false)))
                {
                    RunCmd(baiduPCSGoFolderFullPath, $"BaiduPCS-Go.exe share set /apps/BaiduPCS-Go/{PlayerSettings.productName}");
                }

                if (GUILayout.Button("列出分享", GUILayout.ExpandWidth(false)))
                {
                    RunCmd(baiduPCSGoFolderFullPath, "BaiduPCS-Go.exe share list");
                }

                if (GUILayout.Button("生成下载脚本", GUILayout.ExpandWidth(false)))
                {
                    GenerateDownloadScript();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void BaiduPCSLogin()
        {
            string cookiesFilePath = Path.Combine(baiduPCSGoFolderFullPath, "cookies.txt");
            if (!File.Exists(cookiesFilePath))
            {
                EditorUtility.DisplayDialog("提示", "请先放置cookies.txt文件到BaiduPCS-Go目录下", "确定");
                return;
            }
            string fileContent = File.ReadAllText(cookiesFilePath);
            RunCmd(baiduPCSGoFolderFullPath, $"BaiduPCS-Go.exe login -cookies=\"{fileContent}\"");
        }

        private void BaiduPCSLogout()
        {
            RunCmd(baiduPCSGoFolderFullPath, "echo y | BaiduPCS-Go.exe logout");
        }

        private void GenerateDownloadScript()
        {
            string createFilePath = Path.Combine(Path.Combine(Application.dataPath, $"../Build/DownloadTools/"), $"download_{PlayerSettings.productName}.bat");

            string template = File.ReadAllText(Path.Combine(baiduPCSGoFolderFullPath, "template.bat"));
            string command = template.Replace("{{COOKIES}}", File.ReadAllText(Path.Combine(baiduPCSGoFolderFullPath, "cookies.txt")).Trim());
            command = command.Replace("{{ProductName}}", PlayerSettings.productName);

            // 创建文件夹
            string dir = Path.GetDirectoryName(createFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            // 创建脚本文件
            File.WriteAllText(createFilePath, command,Encoding.GetEncoding("GBK"));


            // 复制BaiduPCS-Go二进制文件
            string sourceBinaryFilePath = Path.Combine(baiduPCSGoFolderFullPath, "BaiduPCS-Go.exe");
            string targetBinaryFilePath = Path.Combine(dir, "BaiduPCS-Go.exe");
            if (File.Exists(sourceBinaryFilePath) && !File.Exists(targetBinaryFilePath))
                File.Copy(sourceBinaryFilePath, targetBinaryFilePath);

            Debug.Log($"已生成下载脚本: {createFilePath}");
        }

        private static void RunCmd(string workingDirectory, string command)
        {
            Debug.Log($"RunCmd: {workingDirectory} {command}");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k " + command,   // /k = 执行后不关闭窗口
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,        // 关键
                CreateNoWindow = false         // 关键
            };

            Process.Start(psi);
        }

        private static void RunCmdRedirectOutputNoWindow(string workingDirectory, string command)
        {
            Debug.Log($"RunCmdRedirectOutputNoWindow: {workingDirectory} {command}");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,   // /c = 执行后关闭窗口
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Process process = new Process();
            process.StartInfo = psi;
            process.Start();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Debug.Log(error);
            process.Close();
        }

    }

}