using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using System.IO;
using PluginHub.Runtime;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace PluginHub.Runtime
{
    public partial class Debugger
    {
        public class InfoWindow : ScrollableDebuggerWindowBase
#if UNITY_EDITOR
            , IPreprocessBuildWithReport
#endif
        {
            private float titleWidth = 240;

            private StringBuilder sb = new StringBuilder();

            private string[] tabNames = new string[] { "Summary", "Memory", "Screen" };
            private int _selectedTab = 0;

            //用于保存构建信息的文件的路径
            private string buildInfoPath
            { get { return Path.Combine(Application.streamingAssetsPath, "BuildInfo.txt"); } }

            private string appBuildDatatime;
            private string appUpdateInfo;

            public override void OnStart()
            {
                base.OnStart();
                //载入构建信息
                if (File.Exists(buildInfoPath))
                {
                    INIParser iniParser = new INIParser();
                    iniParser.Open(buildInfoPath);
                    //读取构建信息
                    appBuildDatatime = iniParser.ReadValue("BuildInfo", "BuildTime","");
                    appUpdateInfo = iniParser.ReadValue("BuildInfo", "UpdateInfo","");
                    //解释更新信息字符串的时候，将明文的"\n"转换为换行符
                    appUpdateInfo = appUpdateInfo.Replace("\\n", "\n").Trim();
                    iniParser.Close();
                }
            }

            private void ListDirectory(string path)
            {
                if (Directory.Exists(path))
                {
                    string[] files = Directory.GetFiles(path);
                    foreach  (string file in files)
                        Debug.LogWarning(file);
                    string[] directories = Directory.GetDirectories(path);
                    foreach (string directory in directories)
                        Debug.LogWarning(directory);
                }
            }

            protected override void OnDrawScrollableWindow()
            {
                _selectedTab = GUILayout.Toolbar(_selectedTab, tabNames);

                GUILayout.BeginVertical("Box");
                {
                    switch (_selectedTab)
                    {
                        case 0:
                            GUILayout.Label("<b>Base:</b>");
                            DrawRow("App Name", Application.productName);
                            DrawRow("Company Name", Application.companyName);
                            DrawRow("App Version", Application.version);
                            DrawRow("Build DataTime", appBuildDatatime);
                            DrawRow("App UpdateInfo", appUpdateInfo);

                            DrawRow("Unity3D Version", Application.unityVersion);
                            DrawRow("Identifier",
                                string.IsNullOrWhiteSpace(Application.identifier) ? "None" : Application.identifier);
                            DrawRow("Device Name", SystemInfo.deviceName);
                            DrawRow("Device Unique Identifier", SystemInfo.deviceUniqueIdentifier);
                            DrawRow("Internet Reachability",Application.internetReachability.ToString());
                            DrawRow("Device Mac", PHHelper.DeviceMac());

                            GUILayout.Label("<b>Path:</b>");
                            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                                Application.platform == RuntimePlatform.WindowsEditor)
                            {
                                DrawRow("Persistent Data Path", Application.persistentDataPath, "Open",
                                    () => PHHelper.OpenFileExplorer(Application.persistentDataPath));
                                DrawRow("Temporary Cache Path", Application.temporaryCachePath, "Open",
                                    () => PHHelper.OpenFileExplorer(Application.temporaryCachePath));
                                DrawRow("Data Path", Application.dataPath, "Open",
                                    () => PHHelper.OpenFileExplorer(Application.dataPath));
                                DrawRow("Streaming Assets Path", Application.streamingAssetsPath, "Open",
                                    () => PHHelper.OpenFileExplorer(Application.streamingAssetsPath));
                            }
                            else
                            {
                                DrawRow("Persistent Data Path", Application.persistentDataPath, "List",
                                    () => ListDirectory(Application.persistentDataPath));
                                DrawRow("Temporary Cache Path", Application.temporaryCachePath, "List",
                                    () => ListDirectory(Application.temporaryCachePath));
                                DrawRow("Data Path", Application.dataPath, "List",
                                    () => ListDirectory(Application.dataPath));
                                DrawRow("Streaming Assets Path", Application.streamingAssetsPath, "List",
                                    () => ListDirectory(Application.streamingAssetsPath));
                            }
                            
                            break;
                        case 1:
                            //绘制内存占用情况
                            GUILayout.Label("<b>Profiler Information</b>");
                            DrawRow("Supported", Profiler.supported.ToString());
                            DrawRow("Enabled", Profiler.enabled.ToString());

                            DrawRow("", "");
                            DrawRow("Mono Used Size", GetByteLengthString(Profiler.GetMonoUsedSizeLong()));
                            DrawRow("Mono Heap Size", GetByteLengthString(Profiler.GetMonoHeapSizeLong()));
                            DrawRow("Used Heap Size", GetByteLengthString(Profiler.usedHeapSizeLong));

                            DrawRow("", "");
                            DrawRow("Total Allocated Memory",
                                GetByteLengthString(Profiler.GetTotalAllocatedMemoryLong()));
                            DrawRow("Total Reserved Memory",
                                GetByteLengthString(Profiler.GetTotalReservedMemoryLong()));
                            DrawRow("Total Unused Reserved Memory",
                                GetByteLengthString(Profiler.GetTotalUnusedReservedMemoryLong()));

                            DrawRow("", "");
                            DrawRow("Allocated Memory For Graphics Driver",
                                GetByteLengthString(Profiler.GetAllocatedMemoryForGraphicsDriver()));

                            if (GUILayout.Button("Call GC"))
                            {
                                Resources.UnloadUnusedAssets();
                                GC.Collect();
                            }

                            break;
                        case 2://screen infos
                            DrawRow("Screen Width", $"{Screen.width} px / {GetCentimetersFromPixels(Screen.width)} cm");
                            DrawRow("Screen Height",
                                $"{Screen.height} px / {GetCentimetersFromPixels(Screen.height)} cm");
                            DrawRow("Screen DPI", Screen.dpi.ToString());
                            DrawRow("Current Resolution", Screen.currentResolution.ToString());
                            DrawRow("FullScreenMode", Screen.fullScreenMode.ToString());
                            sb.Clear();
                            foreach (var resolution in Screen.resolutions)
                                sb.Append($"{resolution}\n");
                            DrawRow("Supported Resolutions:", sb.ToString());
                        
                            GUILayout.Label("<b>TouchScreenKeyboard:</b>");
                            DrawRow("TouchScreenKeyboard.area",TouchScreenKeyboard.area.ToString());
                            DrawRow("TouchScreenKeyboard.visible",TouchScreenKeyboard.visible.ToString());
                            DrawRow("TouchScreenKeyboard.isSupported",TouchScreenKeyboard.isSupported.ToString());
                            
                            break;
                    }
                }
                GUILayout.EndVertical();
            }

            private void DrawRow(string title, string content, string buttonName = "", Action action = null)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(title, GUILayout.Width(titleWidth));

                    if (GUILayout.Button(content, "Label"))
                    {
                        //复制到剪贴板
                        GUIUtility.systemCopyBuffer = content;
                    }

                    if (!string.IsNullOrWhiteSpace(buttonName) &&
                        GUILayout.Button(buttonName, GUILayout.ExpandWidth(false)))
                    {
                        action?.Invoke();
                    }
                }
                GUILayout.EndHorizontal();
            }

            //获取合适的表示方法
            public static string GetByteLengthString(long byteLength)
            {
                if (byteLength < 1024L) // 2 ^ 10
                {
                    return $"{byteLength} Bytes";
                }

                if (byteLength < 1048576L) // 2 ^ 20
                {
                    return $"{(byteLength / 1024f).ToString("F2")} KB";
                }

                if (byteLength < 1073741824L) // 2 ^ 30
                {
                    return $"{(byteLength / 1048576f).ToString("F2")} MB";
                }

                if (byteLength < 1099511627776L) // 2 ^ 40
                {
                    return $"{(byteLength / 1073741824f).ToString("F2")} GB";
                }

                if (byteLength < 1125899906842624L) // 2 ^ 50
                {
                    return $"{(byteLength / 1099511627776f).ToString("F2")} TB";
                }

                if (byteLength < 1152921504606846976L) // 2 ^ 60
                {
                    return $"{(byteLength / 1125899906842624f).ToString("F2")} PB";
                }

                return $"{(byteLength / 1152921504606846976f).ToString("F2")} EB";
            }

            public static float GetCentimetersFromPixels(float pixels)
            {
                return 2.54f * pixels / Screen.dpi;
            }


#if UNITY_EDITOR
            //将在构建之前调用
            public void OnPreprocessBuild(BuildReport report)
            {
                //if windows editor
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    Debug.Log($"Write build info to {buildInfoPath}");
                    //make sure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(buildInfoPath));

                    INIParser iniParser = new INIParser();
                    iniParser.Open(buildInfoPath);
                    //写入构建信息到文件中
                    iniParser.WriteValue("BuildInfo", "BuildTime", TimeEx.GetTimeStrToSecondPretty());
                    iniParser.Close();
                }
            }

            public int callbackOrder => 0;
#endif
        }
    }
}