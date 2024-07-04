using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    #if UNITY_EDITOR
    using UnityEditor;
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

            GUILayout.Label($"Status: {(appExpire.IsExpired() ? "Expired" : "Not Expired")}");

            if (GUILayout.Button("Clear Last Run Time"))
            {
                PlayerPrefs.DeleteKey(appExpire.key);
            }
        }
    }
    #endif


    // 在检视面板中设置程序到期时间，到期后程序无法运行
    // 使用简单的方式，记录上次运行时间。防止用户修改系统时间继续使用
    public class ApplicationExpire : MonoBehaviour,Debugger.CustomWindow.ICustomWindowGUI
    {
        public string expireDate = "2099-12-31";
        private bool isExpired = false;

        public string key => $"{Application.productName}_{Application.version}_ApplicationExpire_LastRunTime";
        // 上次运行时间
        public long lastRunTime{
            get => long.Parse(PlayerPrefs.GetString(key, "0"));
            private set => PlayerPrefs.SetString(key, value.ToString());
        }

        void Start()
        {
            if (IsExpired())
            {
                isExpired = true;
                StartCoroutine(DelayQuit());
            }

            // 只往后调整最后运行时间
            if(lastRunTime < System.DateTime.Now.Ticks)
                lastRunTime = System.DateTime.Now.Ticks;
        }

        // 是否已经过期
        public bool IsExpired()
        {
            bool isExpired = false;
            try
            {
                isExpired = System.DateTime.Now > System.DateTime.Parse(expireDate) ||
                            System.DateTime.Now.Ticks < lastRunTime;
            }
            catch (FormatException e)
            {
                Debug.LogError($"{e}: {e.Message}");
                isExpired = true;
            }
            return isExpired;
        }

        IEnumerator DelayQuit()
        {
            yield return new WaitForSeconds(10);
            Application.Quit();

            #if UNITY_EDITOR
            if (Application.isEditor)
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        private void OnGUI()
        {
            if (isExpired)
            {
                for (int i = 0; i < 10; i++)
                    GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "程序已到期，请联系供应商");
            }
        }

        public int DebuggerDrawOrder { get; set; } = 999;
        public void OnDrawDebuggerGUI()
        {
            GUILayout.Label($"您的应用程序将在 {expireDate} 到期");

            if (GUILayout.Button("Clear Last Run Time"))
                PlayerPrefs.DeleteKey(key);
        }
    }
}
