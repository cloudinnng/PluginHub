using System;
using System.Collections;
using UnityEngine;

namespace PluginHub.Runtime
{
    using System.IO;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine.Assertions;

    [CustomEditor(typeof(ApplicationExpire))]
    public class ApplicationExpireEditor : Editor
    {
        private string expireDateForWriteFile
        {
            get => EditorPrefs.GetString("ApplicationExpire_ExpireDateForWriteFile", "2099-12-31");
            set => EditorPrefs.SetString("ApplicationExpire_ExpireDateForWriteFile", value);
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Label($"Current Time: {System.DateTime.Now}");


            ApplicationExpire appExpire = target as ApplicationExpire;

            DateTime lastRunTime = new DateTime(appExpire.lastRunTime);
            GUILayout.Label($"Last Run Time: {lastRunTime}");
            GUILayout.Label($"Build DateTime: {appExpire.buildDate}");
            GUILayout.Label($"Expired Date: {appExpire.expireDate}");

            bool isExpired = appExpire.expireStatus != ApplicationExpireStatus.NotExpired;
            string statusEmoji = isExpired ? "❌" : "✅";
            GUILayout.Label($"Status: {statusEmoji} {(isExpired ? "Expired" : "Not Expired")} ({appExpire.expireStatus})");

            // 
            expireDateForWriteFile = EditorGUILayout.TextField("到期日期", expireDateForWriteFile);

            if (GUILayout.Button("生成数据文件"))
            {
                GenerateDataFile(appExpire.dataFilePath);
            }

            if (GUILayout.Button("重新载入数据"))
            {
                appExpire.InitDataFromFile();
            }

            if (GUILayout.Button("Clear Last Run Time"))
            {
                PlayerPrefs.DeleteKey(appExpire.key);
            }
        }

        private void GenerateDataFile(string dataFilePath)
        {
            long currTicks = DateTime.Now.Ticks;
            long expireTicks = DateTime.Parse(expireDateForWriteFile).Ticks;
            WriteDataToFile(dataFilePath, currTicks, expireTicks);
            Debug.Log($"[ApplicationExpire] GenerateDataFile: 构建数据已保存到 {dataFilePath}");
        }

        /// <summary>
        /// 将构建时间以二进制方式写入文件
        /// </summary>
        private void WriteDataToFile(string dataFilePath, long buildTicks,long expireTicks)
        {
            try
            {
                // 转换为字节数组
                byte[] bytes = BitConverter.GetBytes(buildTicks);
                byte[] expireBytes = BitConverter.GetBytes(expireTicks);
                byte[] dataBytes = new byte[bytes.Length + expireBytes.Length];
                Assert.IsTrue(dataBytes.Length == 16);
                Buffer.BlockCopy(bytes, 0, dataBytes, 0, bytes.Length);
                Buffer.BlockCopy(expireBytes, 0, dataBytes, bytes.Length, expireBytes.Length);
                // 写入文件
                File.WriteAllBytes(dataFilePath, dataBytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ApplicationExpire] WriteDataToFile: 写入构建时间文件失败: {e.Message}");
            }
        }
    }
#endif

    public static class ApplicationExpireStyle
    { 
        private static GUIStyle _Style;
        public static GUIStyle Style
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
    }
    
    public enum ApplicationExpireStatus
    {
        NotExpired,// 只有该状态允许进入程序
        Expired,
        BuildTimeError,
        SystemTimeError
    }


    // 在检视面板中设置程序到期时间，到期后程序无法运行
    // 使用简单的方式，记录上次运行时间。防止用户修改系统时间继续使用
    public class ApplicationExpire : MonoBehaviour, Debugger.CustomWindow.IDebuggerCustomWindowGUI
    {
        public string dataFilePath => Path.Combine(Application.streamingAssetsPath, "ApplicationExpire.data");

        private DateTime _expireDate;
        private DateTime _buildDate;

        public DateTime expireDate
        {
            get
            { 
                if (_expireDate == default(DateTime))
                    InitDataFromFile();
                return _expireDate;
            }
        }
        public DateTime buildDate
        {
            get
            {
                if (_buildDate == default(DateTime))
                    InitDataFromFile();
                return _buildDate;
            }
        }

        public void InitDataFromFile()
        {
            try
            {
                Debug.Log($"[ApplicationExpire] InitDataFromFile: {dataFilePath}");
                if (!File.Exists(dataFilePath))
                {
                    _expireDate = new DateTime(1970, 1, 1);
                    _buildDate = new DateTime(1970, 1, 1);
                    return;
                }
                byte[] dataBytes = File.ReadAllBytes(dataFilePath);
                if (dataBytes.Length != 16)
                {
                    Debug.LogError($"[ApplicationExpire] InitDataFromFile: 构建时间文件格式错误，长度应为16字节，实际为{dataBytes.Length}字节");
                    _expireDate = new DateTime(1970, 1, 1);
                    _buildDate = new DateTime(1970, 1, 1);
                    return;
                }

                // 将字节数组转换为long
                long buildTicks = BitConverter.ToInt64(dataBytes, 0);
                long expireTicks = BitConverter.ToInt64(dataBytes, 8);
                _buildDate = new DateTime(buildTicks);
                _expireDate = new DateTime(expireTicks);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ApplicationExpire] InitDataFromFile: 读取构建时间文件失败: {e.Message}");
                _expireDate = new DateTime(1970, 1, 1);
                _buildDate = new DateTime(1970, 1, 1);
                return;
            }
        }

        public string key => $"{Application.productName}_{Application.version}_ApplicationExpire_LastRunTime";
        // 上次运行时间
        public long lastRunTime
        {
            get => long.Parse(PlayerPrefs.GetString(key, "0"));
            private set => PlayerPrefs.SetString(key, value.ToString());
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
                    if (nowTicks > expireDate.Ticks) // 常规过期
                        return ApplicationExpireStatus.Expired;
                    if (nowTicks < buildDate.Ticks) // 用户在一个早于真实时间的系统上运行
                        return ApplicationExpireStatus.BuildTimeError;
                    if (nowTicks < lastRunTime) // 用户手动修改了系统时间
                        return ApplicationExpireStatus.SystemTimeError;
                    return ApplicationExpireStatus.NotExpired;
                }
                catch (FormatException e)
                {
                    Debug.LogError($"[ApplicationExpire] expireStatus: {e}: {e.Message}");
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
                    GUI.Box(new Rect(0, 0, Screen.width, Screen.height), $"程序已到期({expireStatus})，请联系供应商", ApplicationExpireStyle.Style);
            }
        }

        public int DebuggerDrawOrder { get; set; } = 999;

        public int callbackOrder => 1;


        public void OnDrawDebuggerGUI()
        {
            GUILayout.Label($"您的应用程序将在 {expireDate} 到期");

            if (InputEx.GetKey(KeyCode.K) && GUILayout.Button("Clear Last Run Time"))
                PlayerPrefs.DeleteKey(key);
        }

    }

}
