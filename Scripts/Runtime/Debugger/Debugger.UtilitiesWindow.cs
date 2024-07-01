using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace PluginHub.Runtime
{
    public partial class Debugger
    {
        public class UtilitiesWindow : ScrollableDebuggerWindowBase
        {
            private string[] buildSceneNames;
            private bool _intentSetQualityLevel = false; //是否准备设置质量等级


            private string currentPath;

            private string tempText = "";
            // 存储飞书云文件的<文件token,文件名>
            private Dictionary<string,string> feiShuCloudFiles = new Dictionary<string, string>();
            // private float resolutionScale = 1;

            private int inputResolutionWidth
            {
                get { return PlayerPrefs.GetInt("PH_inputResolutionWidth", 1280); }
                set { PlayerPrefs.SetInt("PH_inputResolutionWidth", value); }
            }

            private int inputResolutionHeight
            {
                get { return PlayerPrefs.GetInt("PH_inputResolutionHeight", 720); }
                set { PlayerPrefs.SetInt("PH_inputResolutionHeight", value); }
            }

            private bool fullScreen
            {
                get { return PlayerPrefs.GetInt("PH_fullScreen", 0) == 1; }
                set { PlayerPrefs.SetInt("PH_fullScreen", value ? 1 : 0); }
            }

            private bool isPortrait
            {
                get { return PlayerPrefs.GetInt("PH_portrait", 1) == 1; }
                set { PlayerPrefs.SetInt("PH_portrait", value ? 1 : 0); }
            }


            private int _selectedTab = 0;
            private string[] tabNames = new string[] { "Common", "Scene", "PersistentDataPath" };

            public override void OnStart()
            {
                base.OnStart();
                currentPath = Application.persistentDataPath;
            }

            public override void OnDrawToolbar()
            {
                _selectedTab = GUILayout.Toolbar(_selectedTab, tabNames);
            }

            protected override void OnDrawScrollableWindow()
            {
                GUILayout.BeginVertical("Box");
                {
                    switch (_selectedTab)
                    {
                        case 0:
                            DrawResolutionModule();
                            DrawDebugModule();
                            DrawFrameRateModule();
                            break;
                        case 1:
                            DrawSceneModule();
                            break;
                        case 2:
                            DrawPersistentDataModule();
                        break;
                    }
                }
                GUILayout.EndVertical();
            }

            private void DrawResolutionModule()
            {
                //移动平台上不显示
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
                    return;

                GUILayout.Label("Resolution:");

                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("调用：Screen.SetResolution(w ,h ,false)");
                        inputResolutionWidth = int.Parse(GUILayout.TextField(inputResolutionWidth.ToString(),GUILayout.Width(60)));
                        GUILayout.Label("x",GUILayout.ExpandWidth(false));
                        inputResolutionHeight = int.Parse(GUILayout.TextField(inputResolutionHeight.ToString(),GUILayout.Width(60)));
                        // GUILayout.FlexibleSpace();
                        if (GUILayout.Button("应用",GUILayout.Width(100)))
                        {
                            Screen.SetResolution(inputResolutionWidth, inputResolutionHeight, false);
                        }
                        if (GUILayout.Button("全屏/窗口 切换"))
                        {
                            Screen.fullScreen = !Screen.fullScreen;
                        }
                        if (GUILayout.Button("Native全屏"))
                        {
                            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height,true);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("分辨率快选:");

                        GUILayout.FlexibleSpace();

                        fullScreen = GUILayout.Toggle(fullScreen, "全屏");
                        if (GUILayout.Button("720P",GUILayout.Width(100)))
                            Screen.SetResolution(1280, 720, fullScreen);
                        if (GUILayout.Button("1080P",GUILayout.Width(100)))
                            Screen.SetResolution(1920, 1080, fullScreen);
                        if (GUILayout.Button("2K",GUILayout.Width(100)))
                            Screen.SetResolution(2560, 1440, fullScreen);
                        if (GUILayout.Button("4K",GUILayout.Width(100)))
                            Screen.SetResolution(3840, 2160, fullScreen);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("移动设备预览快选:");
                        GUILayout.FlexibleSpace();
                        isPortrait = GUILayout.Toggle(isPortrait, "竖屏");
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button($"12 Pro max"))
                            TryResolutionWindowed(1284, 2778, isPortrait);
                        if (GUILayout.Button($"iPad Air 5"))
                            TryResolutionWindowed(1536, 2048, isPortrait);
                    }
                    GUILayout.EndHorizontal();

                }
                GUILayout.EndVertical();
            }

            // 窗口模式设置为一个以给定分辨率尽可能大的分辨率，保持宽高比。（给任务栏留出空间）
            private void TryResolutionWindowed(int width, int height, bool isPortrait = true)
            {
                float factorForTaskBar = 0.9f;
                float ratio = isPortrait ? (float)width / height : (float)height / width;
                int screenWidth = Screen.currentResolution.width;
                int screenHeight = Screen.currentResolution.height;
                if (screenWidth > screenHeight)// 横屏,以屏幕高度来限制窗口高度
                {
                    int heightUse = (int) (screenHeight * factorForTaskBar);
                    int widthUse = (int) (heightUse * ratio);
                    Screen.SetResolution(widthUse, heightUse, false);
                }
                else// 竖屏,以屏幕宽度来限制窗口宽度
                {
                    int widthUse = (int) (screenWidth * factorForTaskBar);
                    int heightUse = (int) (widthUse / ratio);
                    Screen.SetResolution(widthUse, heightUse, false);
                }
            }

            private void DrawDebugModule()
            {
                GUILayout.Label("Debug:");
                GUILayout.BeginVertical("Box");
                {
                    if (_intentSetQualityLevel)
                    {
                        GUILayout.Label("选择一个画面质量:");
                        int count = QualitySettings.names.Length;
                        for (int i = 0; i < count; i++)
                        {
                            if (GUILayout.Button(QualitySettings.names[i]))
                            {
                                QualitySettings.SetQualityLevel(i);
                                _intentSetQualityLevel = false;
                                break;
                            }
                        }

                        if (GUILayout.Button("取消"))
                        {
                            _intentSetQualityLevel = false;
                        }
                    }

                    if (!_intentSetQualityLevel)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label("System:");
                                if (GUILayout.Button("重载当前场景"))
                                {
                                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                                }

                                //设置与显示画面质量
                                if (GUILayout.Button(
                                        $"切换质量: {QualitySettings.names[QualitySettings.GetQualityLevel()]}"))
                                {
                                    _intentSetQualityLevel = true;
                                }

                                //只有windows平台才有打开屏幕键盘这个功能
                                if (Application.platform == RuntimePlatform.WindowsPlayer ||
                                    Application.platform == RuntimePlatform.WindowsEditor)
                                {
                                    if (GUILayout.Button("系统屏幕键盘(osk)"))
                                    {
                                        System.Diagnostics.Process.Start("osk");
                                    }
                                }

                                if (GUILayout.Button("尝试释放内存"))
                                {
                                    Resources.UnloadUnusedAssets();
                                    GC.Collect();
                                }

                                if (GUILayout.Button("关闭应用程序"))
                                {
                                    Application.Quit();
                                }
                            }
                            GUILayout.EndVertical();


                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label("Clear:");
                                if (GUILayout.Button("PlayerPrefs"))
                                {
                                    PlayerPrefs.DeleteAll();
                                }

                                if (GUILayout.Button("Persistent Path"))
                                {
                                    string path = Application.persistentDataPath;
                                    DirectoryInfo directory = new DirectoryInfo(path);
                                    if (directory.Exists)
                                    {
                                        //删除文件夹下的所有文件
                                        FileInfo[] files = directory.GetFiles();
                                        foreach (FileInfo file in files)
                                            file.Delete();
                                        //删除文件夹下的所有子文件夹
                                        DirectoryInfo[] directories = directory.GetDirectories();
                                        foreach (DirectoryInfo dir in directories)
                                            dir.Delete(true);
                                    }
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }

            private void DrawFrameRateModule()
            {
                // https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html
                GUILayout.Label($"TargetFrameRate:{Application.targetFrameRate}");
                GUILayout.BeginHorizontal("Box");
                {
                    if (GUILayout.Button("-1"))
                        Application.targetFrameRate = -1;
                    if (GUILayout.Button("15"))
                        Application.targetFrameRate = 15;
                    if (GUILayout.Button("30"))
                        Application.targetFrameRate = 30;
                    if (GUILayout.Button("50"))
                        Application.targetFrameRate = 50;
                    if (GUILayout.Button("60"))
                        Application.targetFrameRate = 70;
                    if (GUILayout.Button("9999"))
                        Application.targetFrameRate = 9999;
                }
                GUILayout.EndHorizontal();
            }
            
            private void DrawSceneModule()
            {
                GUILayout.Label($"当前场景：{SceneManager.GetActiveScene().name}");
                if (GUILayout.Button("重载当前场景"))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                if (buildSceneNames == null)
                {
                    buildSceneNames = new string[SceneManager.sceneCountInBuildSettings];
                    for (int i = 0; i < buildSceneNames.Length; i++)
                    {
                        //string sceneName = SceneManager.GetSceneByBuildIndex(i).path;
                        string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                        if (string.IsNullOrWhiteSpace(scenePath)) scenePath = "NoName";
                        buildSceneNames[i] = $"{i} {scenePath}";
                    }
                }

                GUILayout.Label($"或者选择一个场景:{buildSceneNames.Length}");
                for (int i = 0; i < buildSceneNames.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(buildSceneNames[i]);
                    if (GUILayout.Button($"{i}、{name}"))
                    {
                        SceneManager.LoadScene(i);
                    }
                }
            }

            //绘制PersistentDataPath管理的UI
            private void DrawPersistentDataModule()
            {
                // if (GUILayout.Button("压缩测试"))
                // {
                //     //解决中文乱码
                //     ICSharpCode.SharpZipLib.Zip.ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                //     new FastZip().CreateZip(@"C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\1.zip", @"C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\新建文件夹\", true, null);
                // }
                // if (GUILayout.Button("解压测试"))
                // {
                //     new FastZip().ExtractZip("C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\1.zip", "C:\\Users\\TTW\\AppData\\LocalLow\\DefaultCompany\\10.RunProgramTest\\解压", null);
                // }
                // if (GUILayout.Button("Download test"))
                // {
                //     Debugger.Instance.StartCoroutine(FeiShuFileBakcup.DownloadFile("SS4zbPEvvodjDWx7bKEck1r3nYf", "1.txt"));
                // }

                GUILayout.Label("PersistentDataPath管理:");
                float buttonWidth = 80;
                GUILayout.BeginVertical("Box");
                {
                    GUILayout.Label("简易文件管理器：");

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("上一级", GUILayout.Width(buttonWidth)))
                            currentPath = Path.GetDirectoryName(currentPath);
                        if (GUILayout.Button("Home", GUILayout.Width(buttonWidth)))
                            currentPath = Application.persistentDataPath;
                        if (GUILayout.Button("使用Explorer打开目录"))
                            System.Diagnostics.Process.Start(currentPath);

                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    //显示当前路径
                    GUILayout.BeginVertical("Box");
                    {
                        GUILayout.Label(currentPath);
                    }
                    GUILayout.EndVertical();

                    // 列出所有目录
                    string[] dirs = Directory.GetDirectories(currentPath);
                    foreach (string dir in dirs)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("目录", GUILayout.Width(buttonWidth)))
                                currentPath = dir;

                            GUILayout.Label(dir.Replace(currentPath, ""));

                            if (GUILayout.Button("压缩", GUILayout.Width(buttonWidth)))
                            {
                                //解决中文乱码
                                ICSharpCode.SharpZipLib.Zip.ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                                new FastZip().CreateZip(dir + ".zip", dir, true, null);
                            }


                            if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                            {
                                Directory.Delete(dir, true);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }

                    //列出所有文件
                    {
                        string[] files = Directory.GetFiles(currentPath);
                        foreach (string file in files)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("", GUILayout.Width(buttonWidth));
                                GUILayout.Label(file.Replace(currentPath, ""));
                                bool isZipFile = file.ToLower().EndsWith(".zip");

                                if (!isZipFile && GUILayout.Button("OpenAsText", GUILayout.Width(buttonWidth)))
                                    tempText = File.ReadAllText(file);


                                if (GUILayout.Button("上传", GUILayout.Width(buttonWidth)))
                                {
                                    Debugger.Instance.StartCoroutine(FeiShuFileBackup.UploadFile(file, FeiShuFileBackup.rootFolderToken));
                                }

                                if (isZipFile)
                                {
                                    if (GUILayout.Button("解压到此处", GUILayout.Width(buttonWidth)))
                                    {
                                        //解决中文乱码
                                        ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                                        string zipNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                                        new FastZip().ExtractZip(file, currentPath, null);
                                    }
                                    if (GUILayout.Button("解压到目录", GUILayout.Width(buttonWidth)))
                                    {
                                        //解决中文乱码
                                        ZipStrings.CodePage = Encoding.GetEncoding("gbk").CodePage;
                                        string zipNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                                        string targetPath = Path.Combine(currentPath, zipNameWithoutExtension);
                                        new FastZip().ExtractZip(file, targetPath, null);
                                    }
                                }else
                                {
                                    GUILayout.Label("", GUILayout.Width(buttonWidth));
                                }

                                if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                                    File.Delete(file);
                            }
                            GUILayout.EndHorizontal();
                        }

                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("显示云端内容"))
                        {
                            Debugger.Instance.StartCoroutine(
                                FeiShuFileBackup.ListFiles(FeiShuFileBackup.rootFolderToken,feiShuCloudFiles));
                        }
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    //显示云端文件
                    foreach (var feiShuCloudFile in feiShuCloudFiles)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            string fileName = feiShuCloudFile.Value;
                            string fileToken = feiShuCloudFile.Key;

                            GUILayout.Label(fileName);

                            if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                                Debugger.Instance.StartCoroutine(FeiShuFileBackup.DeleteFile(fileToken));

                            if (GUILayout.Button("下载", GUILayout.Width(buttonWidth)))
                            {
                                string savePath =Path.Combine(currentPath,fileName);
                                Debugger.Instance.StartCoroutine(FeiShuFileBackup.DownloadFile(fileToken, savePath));
                            }
                        }
                        GUILayout.EndHorizontal();
                    }


                    //显示以文本方式打开的文件内容
                    if (!string.IsNullOrWhiteSpace(tempText))
                    {
                        if (GUILayout.Button("Close"))
                            tempText = "";
                        GUILayout.Label(tempText.Trim());
                    }
                }
                GUILayout.EndVertical();
            }

        }
    }
}