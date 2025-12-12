using System;
using System.Collections;
using UnityEngine;

namespace PluginHub.Runtime
{
    using System.IO;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Build;

    using UnityEditor.Build.Reporting;
    using static PluginHub.Runtime.ApplicationExpire;


    [CustomEditor(typeof(ApplicationExpire))]
    public class ApplicationExpireEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Label($"Current Time: {System.DateTime.Now}");


            ApplicationExpire appExpire = target as ApplicationExpire;

            DateTime lastRunTime = new DateTime(appExpire.lastRunTime);
            GUILayout.Label($"Last Run Time: {lastRunTime}");
            GUILayout.Label($"Build DateTime: {new DateTime(ApplicationExpire.buildTimeTicks)}");

            bool isExpired = appExpire.expireStatus != ApplicationExpireStatus.NotExpired;
            string statusEmoji = isExpired ? "❌" : "✅";
            GUILayout.Label($"Status: {statusEmoji} {(isExpired ? "Expired" : "Not Expired")} ({appExpire.expireStatus})");
            GUILayout.Label($"Expired Date: {appExpire.expireDate}");

            if (GUILayout.Button("Clear Last Run Time"))
            {
                PlayerPrefs.DeleteKey(appExpire.key);
            }
            if (GUILayout.Button("生成构建时间文件"))
            {
                appExpire.GenerateBuildDataFile();
            }
        }
    }
#endif


    // 在检视面板中设置程序到期时间，到期后程序无法运行
    // 使用简单的方式，记录上次运行时间。防止用户修改系统时间继续使用
    public class ApplicationExpire : MonoBehaviour, Debugger.CustomWindow.IDebuggerCustomWindowGUI
#if UNITY_EDITOR
    , IPreprocessBuildWithReport
#endif
    {

        public enum ApplicationExpireStatus
        {
            NotExpired,// 只有该状态允许进入程序
            Expired,
            BuildTimeError,
            SystemTimeError
        }


        public string expireDate = "2099-12-31";

        public string key => $"{Application.productName}_{Application.version}_ApplicationExpire_LastRunTime";
        // 上次运行时间
        public long lastRunTime
        {
            get => long.Parse(PlayerPrefs.GetString(key, "0"));
            private set => PlayerPrefs.SetString(key, value.ToString());
        }



#if UNITY_EDITOR
        public void GenerateBuildDataFile()
        {
            long ticks = DateTime.Now.Ticks;
            WriteDataToFile(ticks);
            Debug.Log($"ApplicationExpire: 构建数据已保存到 {dataFilePath}");
        }

        // 构建预处理
        public void OnPreprocessBuild(BuildReport report)
        {
            GenerateBuildDataFile();
        }
#endif

        private GUIStyle _Style;
        private GUIStyle style
        {
            get
            {
                if (_Style == null)
                {
                    _Style = new GUIStyle(GUI.skin.box);
                    _Style.alignment = TextAnchor.MiddleCenter;
                    _Style.fontSize = 26;
                }
                return _Style;
            }
        }

        void Start()
        {
            if (expireStatus != ApplicationExpireStatus.NotExpired)
            {
                StartCoroutine(DelayQuit());
            }

            // 只往后调整最后运行时间
            if (lastRunTime < System.DateTime.Now.Ticks)
                lastRunTime = System.DateTime.Now.Ticks;
        }

        // 是否已经过期
        public ApplicationExpireStatus expireStatus
        {
            get
            {
                try
                {
                    long nowTicks = DateTime.Now.Ticks;
                    if (nowTicks > DateTime.Parse(expireDate).Ticks) // 常规过期
                        return ApplicationExpireStatus.Expired;
                    if (nowTicks < buildTimeTicks) // 用户在一个早于真实时间的系统上运行
                        return ApplicationExpireStatus.BuildTimeError;
                    if (nowTicks < lastRunTime) // 用户手动修改了系统时间
                        return ApplicationExpireStatus.SystemTimeError;
                    return ApplicationExpireStatus.NotExpired;
                }
                catch (FormatException e)
                {
                    Debug.LogError($"{e}: {e.Message}");
                    return ApplicationExpireStatus.BuildTimeError;
                }
            }
        }

        IEnumerator DelayQuit()
        {
            yield return new WaitForSeconds(10);
            Application.Quit();

#if UNITY_EDITOR
            if (Application.isEditor)
                EditorApplication.isPlaying = false;
#endif
        }

        private void OnGUI()
        {
            if (expireStatus != ApplicationExpireStatus.NotExpired)
            {
                for (int i = 0; i < 10; i++)
                    GUI.Box(new Rect(0, 0, Screen.width, Screen.height), $"程序已到期({expireStatus})，请联系供应商", style);
            }
        }

        public int DebuggerDrawOrder { get; set; } = 999;

        public int callbackOrder => 1;


        public void OnDrawDebuggerGUI()
        {
            GUILayout.Label($"您的应用程序将在 {expireDate} 到期");

            if (Input.GetKey(KeyCode.K) && GUILayout.Button("Clear Last Run Time"))
                PlayerPrefs.DeleteKey(key);
        }


        #region save and load data

        // 存储构建时间的文件
        private static string dataFilePath => Path.Combine(Application.streamingAssetsPath, "ApplicationExpire.data");

        public static long buildTimeTicks => ReadDataFromFile();

        /// <summary>
        /// 从二进制文件读取构建时间
        /// </summary>
        private static long ReadDataFromFile()
        {
            try
            {
                if (!File.Exists(dataFilePath))
                    return new DateTime(2099, 12, 31).Ticks;
                byte[] encryptedBytes = File.ReadAllBytes(dataFilePath);
                if (encryptedBytes.Length != 8)
                {
                    Debug.LogError($"构建时间文件格式错误，长度应为8字节，实际为{encryptedBytes.Length}字节");
                    return new DateTime(2099, 12, 31).Ticks;
                }

                // 将字节数组转换为long
                long ticks = BitConverter.ToInt64(encryptedBytes, 0);
                return ticks;
            }
            catch (Exception e)
            {
                Debug.LogError($"读取构建时间文件失败: {e.Message}");
                return new DateTime(2099, 12, 31).Ticks;
            }
        }

        /// <summary>
        /// 将构建时间以二进制方式写入文件
        /// </summary>
        private static void WriteDataToFile(long ticks)
        {
            try
            {
                // 转换为字节数组
                byte[] bytes = BitConverter.GetBytes(ticks);
                // 写入文件
                File.WriteAllBytes(dataFilePath, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"写入构建时间文件失败: {e.Message}");
            }
        }

        #endregion
    }

}
