    using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PluginHub.Runtime
{
#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(IMGUIConsole))]
    public class IMGUIConsoleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            IMGUIConsole imGUIConsole = target as IMGUIConsole;

            if (GUILayout.Button("Add Log"))
                imGUIConsole.AddLog("Test Log", "Test StackTrace", LogType.Log);
            if (GUILayout.Button("Add Warning"))
                imGUIConsole.AddLog("Test Warning", "Test StackTrace", LogType.Warning);
            if (GUILayout.Button("Add Error"))
                imGUIConsole.AddLog("Test Error", "Test StackTrace", LogType.Error);

            if (GUILayout.Button("Clear"))
                imGUIConsole.Clear();
        }
    }
#endif


    // IMGUI 控制台组件
    [ExecuteAlways]
    public class IMGUIConsole : IMGUIManager.IIMGUI
    {
        private class LogNode
        {
            public readonly DateTime logTime = DateTime.Now;
            public string message;
            public string stacktrace;
            public LogType type;
            public Color color;
        }

        public int maxLogCount = 100;
        private List<LogNode> _logs = new List<LogNode>();
        private LogNode _selectedLogNode;// 当前选中的日志
        private bool enableNormalLog = true;
        private bool enableWarningLog = true;
        private bool enableErrorLog = true;

        private bool _lockScroll = true;
        private Vector2 _scrollPositionItemArea = Vector2.zero;
        private Vector2 _scrollPositionDetailArea = Vector2.zero;


        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void HandleLog(string condition, string stacktrace, LogType type)
        {
            AddLog(condition, stacktrace, type);
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        public void AddLog(string condition, string stacktrace, LogType type)
        {
            Color color;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    color = Color.red;
                    break;
                case LogType.Warning:
                    color = Color.yellow;
                    break;
                default:
                    color = Color.white;
                    break;
            }

            _logs.Add(new LogNode()
            {
                message = condition,
                stacktrace = stacktrace,
                type = type,
                color = color
            });
            if (_logs.Count > maxLogCount)
                _logs.RemoveAt(0);
        }

        public void Clear()
        {
            _logs.Clear();
        }

        public override void IMGUIDraw()
        {
            Vector2 screenSize = IMGUIManager.Instance.ScreenSize(localGUIScale);

            GUILayout.BeginArea(new Rect(0, 0, screenSize.x, screenSize.y));
            {
                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Clear", GUILayout.Width(100)))
                            Clear();

                        _lockScroll = GUILayout.Toggle(_lockScroll, "Lock Scroll");

                        GUILayout.FlexibleSpace();

                        int logCount = _logs.Where(log=>log.type == LogType.Log).Count();
                        enableNormalLog = GUILayout.Toggle(enableNormalLog, $"Log ({logCount})");
                        int warningCount = _logs.Where(log => log.type == LogType.Warning).Count();
                        enableWarningLog = GUILayout.Toggle(enableWarningLog, $"Warning ({warningCount})");
                        int errorCount = _logs.Where(log => log.type == LogType.Error).Count();
                        enableErrorLog = GUILayout.Toggle(enableErrorLog, $"Error ({errorCount})");
                    }
                    GUILayout.EndHorizontal();

                    if (_lockScroll)
                        _scrollPositionItemArea.y = float.MaxValue;

                    _scrollPositionItemArea = GUILayout.BeginScrollView(_scrollPositionItemArea,"Box");
                    {
                        bool selected = false;
                        foreach (var log in _logs)
                        {
                            // 过滤日志
                            if(log.type == LogType.Log && !enableNormalLog)
                                continue;
                            if(log.type == LogType.Warning && !enableWarningLog)
                                continue;
                            if(log.type == LogType.Error && !enableErrorLog)
                                continue;


                            if (GUILayout.Toggle(_selectedLogNode == log, GetLogString(log)))
                            {
                                selected = true;
                                if (_selectedLogNode != log)
                                {
                                    _selectedLogNode = log;
                                    _scrollPositionDetailArea = Vector2.zero;
                                }
                            }
                        }
                        if (!selected)
                            _selectedLogNode = null;
                    }
                    GUILayout.EndScrollView();

                    _scrollPositionDetailArea = GUILayout.BeginScrollView(_scrollPositionDetailArea,"Box",GUILayout.Height(150));
                    {
                        if(_selectedLogNode != null)
                        {
                            GUILayout.Label($"Condition: {_selectedLogNode?.message}");
                            GUILayout.Label($"StackTrace: {_selectedLogNode?.stacktrace}");
                        }
                    }
                    GUILayout.EndScrollView();


                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        private string GetLogString(LogNode logNode)
        {
            Color32 color = GetLogStringColor(logNode.type);
            //很多行时只显示第一行
            string[] splitResult = logNode.message.Split('\n');
            string logShow = splitResult[0];
            if (splitResult.Length > 1)
                logShow = logShow + "<color=#00ff00ff>该条日志有多行，点击展开</color>";
            return string.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}>{4:[HH点mm分ss秒fff]} {5}</color>",
                color.r, color.g, color.b, color.a, logNode.logTime, logShow);
        }

        private Color32 GetLogStringColor(LogType logType)
        {
            Color32 color = Color.white;
            switch (logType)
            {
                case LogType.Log:
                    color = Color.white;
                    break;
                case LogType.Warning:
                    color = Color.yellow;
                    break;
                case LogType.Error:
                    color = Color.red;
                    break;
                case LogType.Exception:
                    color = new Color(0.7f, 0.2f, 0.2f);
                    break;
            }

            return color;
        }

        public override int IMGUIOrder => -1000;
    }

}
