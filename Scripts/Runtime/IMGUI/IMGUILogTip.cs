using System;
using System.Collections;
using System.Collections.Generic;
using PluginHub.Runtime;
using UnityEngine;

namespace PluginHub.Runtime
{
    [ExecuteAlways]
    // 显示在最上层的提示语,一段时间后消失
    // 捕获错误日志来显示，以提示用户
    // 想法来自ToastManager
    public class IMGUILogTip : SceneSingleton<IMGUILogTip>, IMGUIManager.IIMGUI
    {
        [System.Serializable]
        private class ToastInstance
        {
            public string text;
            public Color color;
            public float alpha;
        }

        public float localGUIScale = 1.3f;
        public bool alwaysShow = false;// 测试用
        public bool ignoreNormalLog = true;
        public float showDuration = 2;

        private GUIContent tempContent = new GUIContent();

        [SerializeField]
        private List<ToastInstance> _toastInstances = new List<ToastInstance>();

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            // 不提醒普通日志
            if (ignoreNormalLog && type == LogType.Log)
                return;
            Color color;
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    color = Color.red;
                    break;
                case LogType.Warning:
                    color = Color.yellow;
                    break;
                case LogType.Log:
                    color = Color.white;
                    break;
                default:
                    color = Color.white;
                    break;
            }
            // 添加提示
            ToastInstance toastInstance = new ToastInstance();
            toastInstance.text = condition;
            toastInstance.color = color;
            toastInstance.alpha = 0;
            _toastInstances.Add(toastInstance);
            StartCoroutine(Delay(toastInstance));
            if (_toastInstances.Count > 10)
                _toastInstances.RemoveAt(0);
        }

        // 这是为了实现一个闪烁效果，让新的提示更加显眼
        private IEnumerator Delay(ToastInstance toastInstance)
        {
            yield return null;
            yield return null;
            toastInstance.alpha = showDuration;
        }


        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }


        public void IMGUIDraw()
        {
            for(int i = _toastInstances.Count - 1; i >= 0; i--)
            {
                ToastInstance toastInstance = _toastInstances[i];
                DrawToast(_toastInstances.Count - i - 1, toastInstance);
            }
        }

        public int IMGUIOrder => 99999;
        public float IMGUILocalGUIScale => localGUIScale;

        private void DrawToast(int positionIndex,ToastInstance toastInstance)
        {
            tempContent.text = toastInstance.text;
            Vector2 textSize = GUI.skin.label.CalcSize(tempContent);
            Vector2 areaSize = textSize + new Vector2(GUI.skin.box.padding.horizontal, GUI.skin.box.padding.vertical);

            Vector2 screenSize = IMGUIManager.Instance.ScreenSize(IMGUILocalGUIScale);
            Rect area = new Rect(screenSize.x / 2 - areaSize.x / 2, screenSize.y / 2 - areaSize.y / 2, areaSize.x, areaSize.y);
            area.y -= positionIndex * (areaSize.y + 3);

            toastInstance.color.a = alwaysShow ? 1 : Mathf.Clamp01(toastInstance.alpha);
            // toastInstance.color.a -= positionIndex * 0.2f;
            GUI.color = toastInstance.color;
            GUILayout.BeginArea(area, GUI.skin.box);
            {
                GUILayout.Label(toastInstance.text, GUILayout.Width(areaSize.x), GUILayout.Height(areaSize.y));
            }
            GUILayout.EndArea();
            GUI.color = Color.white;
        }

        private void Update()
        {
            for (int i = _toastInstances.Count - 1; i >= 0; i--)
            {
                ToastInstance toastInstance = _toastInstances[i];
                if (toastInstance.alpha > 0)
                {
                    toastInstance.alpha -= Time.deltaTime;
                    if (toastInstance.alpha <= 0)
                    {
                        if (!alwaysShow)
                            _toastInstances.RemoveAt(i);
                    }
                }
            }
        }
    }
}

