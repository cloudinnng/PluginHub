using System;
using System.Collections;
using System.IO;
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

            private string deviceIdentifier = "DefaultIdentifier";
            private string tempText = "";
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
            private string[] tabNames = new string[] { "Common", "Scene"};//, "PersistentDataPath" };

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
                        // case 2:
                        //     DrawPersistentDataModule();
                        //     break;
                    }
                }
                GUILayout.EndVertical();
            }

            #region Nas上storage-system的文件上传助手方法

            private IEnumerator UploadFile(string pathToFile, string serverPath = "./uploads")
            {
                Debug.Log($"UploadFile:{pathToFile} to {serverPath}");
                string fileName = Path.GetFileName(pathToFile); //eg 1.txt
                string url = "https://hellottw.com:5555/storage-system/upload.php";

                WWWForm form = new WWWForm();
                form.AddBinaryData("file", File.ReadAllBytes(pathToFile), fileName);
                form.AddField("dir", serverPath);
                UnityWebRequest request = UnityWebRequest.Post(url, form);
                yield return request.SendWebRequest();

#if UNITY_2019
                if(!string.IsNullOrWhiteSpace(request.error))
#else
                if (request.result != UnityWebRequest.Result.Success)
#endif
                {
                    Debug.LogError("上传失败");
                    Debug.LogError(request.error);
                    if (ToastManager.Instance != null)
                        ToastManager.Instance.Show("上传失败");
                }
                else
                {
                    Debug.Log(request.downloadHandler.text);
                }

                request.Dispose();
            }

            private IEnumerator DownloadFile(string serverPathToFile = "./uploads/demo.txt",
                string localPath = "./uploads/fileName.txt")
            {
                string url = $"https://hellottw.com:5555/storage-system/download.php?dir={serverPathToFile}";
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();
#if UNITY_2019
                if(!string.IsNullOrWhiteSpace(request.error))
#else
                if (request.result != UnityWebRequest.Result.Success)
#endif
                {
                    Debug.LogError(request.error);
                }
                else
                {
                    //save to persistentDataPath
                    string path = Path.Combine(Application.persistentDataPath, localPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, request.downloadHandler.data);
                    Debug.Log($"DownloadFile:{path}");
                }

                request.Dispose();
            }

            private IEnumerator ListFiles(string serverPath = "./uploads", Action<string> onSucess = null)
            {
                string url = $"https://hellottw.com:5555/storage-system/list-recursion.php?dir={serverPath}";
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();
#if UNITY_2019
                if(!string.IsNullOrWhiteSpace(request.error))
#else
                if (request.result != UnityWebRequest.Result.Success)
#endif
                {
                    Debug.LogError(request.error);
                    Debug.Log(request.downloadHandler.text);
                }
                else
                {
                    Debug.Log(request.downloadHandler.text);
                    onSucess?.Invoke(request.downloadHandler.text);
                }

                request.Dispose();
            }

            #endregion

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

            private void DrawPersistentDataModule()
            {
                GUILayout.Label("PersistentDataPath管理:");
                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("输入云备份设备标识符:", GUILayout.Width(140));
                        deviceIdentifier = GUILayout.TextField(deviceIdentifier).Trim();

                        if (GUILayout.Button("上传全部文件"))
                        {
                            string path = Application.persistentDataPath;
                            DirectoryInfo directory = new DirectoryInfo(path);
                            if (directory.Exists)
                            {
                                //递归列出所有文件
                                FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);
                                foreach (FileInfo file in files)
                                {
                                    string fullPath = file.FullName.Replace("\\", "/");
                                    // print(path);
                                    string suffix = fullPath.Replace(path + "/", "");
                                    // Debug.Log(suffix);
                                    string serverPath = "./" + Application.identifier + "/" +
                                                        deviceIdentifier + "/" + suffix;
                                    serverPath = Path.GetDirectoryName(serverPath);
                                    serverPath = serverPath.Replace("\\", "/");
                                    // Debug.Log(serverPath);
                                    Debugger.Instance.StartCoroutine(UploadFile(file.FullName, serverPath));
                                }
                            }
                        }

                        if (GUILayout.Button("下载全部文件"))
                        {
                            string serverPath = "./" + Application.identifier + "/" + deviceIdentifier;
                            Debugger.Instance.StartCoroutine(ListFiles(serverPath, (result) =>
                            {
                                string[] serverFiles = result.Split(',');
                                foreach (string serverFile in serverFiles)
                                {
                                    // print(serverFile);
                                    if (string.IsNullOrWhiteSpace(serverFile)) continue;
                                    string localPath = serverFile.Replace(
                                        "./" + Application.identifier + "/" + deviceIdentifier, "");
                                    localPath = Application.persistentDataPath + localPath;
                                    // print(localPath);   
                                    Debugger.Instance.StartCoroutine(DownloadFile(serverFile, localPath));
                                }
                            }));
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("简易文件管理器：");

                    float buttonWidth = 50;
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Home", GUILayout.Width(buttonWidth)))
                        {
                            currentPath = Application.persistentDataPath;
                        }

                        if (GUILayout.Button("上一级", GUILayout.Width(buttonWidth)))
                        {
                            currentPath = Path.GetDirectoryName(currentPath);
                        }

                        if (GUILayout.Button("打开目录"))
                        {
                            System.Diagnostics.Process.Start(currentPath);
                        }

                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    //显示当前路径
                    GUILayout.BeginVertical("Box");
                    {
                        GUILayout.Label(currentPath);
                    }
                    GUILayout.EndVertical();

                    //list all directory
                    string[] dirs = Directory.GetDirectories(currentPath);
                    foreach (string dir in dirs)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("文件夹:", GUILayout.Width(50));
                            GUILayout.Label(dir.Replace(currentPath, ""));
                            if (GUILayout.Button("进入", GUILayout.Width(buttonWidth)))
                            {
                                currentPath = dir;
                            }

                            if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                            {
                                Directory.Delete(dir, true);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }

                    {
                        //list all files
                        string[] files = Directory.GetFiles(currentPath);
                        foreach (string file in files)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("文件:", GUILayout.Width(50));
                                GUILayout.Label(file.Replace(currentPath, ""));
                                if (GUILayout.Button("Open", GUILayout.Width(buttonWidth)))
                                {
                                    tempText = File.ReadAllText(file);
                                }

                                // if (GUILayout.Button("上传",GUILayout.Width(buttonWidth)))
                                // {
                                //     Debugger.Instance.StartCoroutine(UploadFile(file));
                                // }
                                if (GUILayout.Button("删除", GUILayout.Width(buttonWidth)))
                                {
                                    File.Delete(file);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        if (!string.IsNullOrWhiteSpace(tempText))
                        {
                            GUILayout.Label(tempText.Trim());
                            if (GUILayout.Button("Close"))
                            {
                                tempText = "";
                            }
                        }
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}