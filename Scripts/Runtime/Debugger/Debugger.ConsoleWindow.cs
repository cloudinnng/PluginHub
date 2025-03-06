using System;
using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    public partial class Debugger
    {
        /// <summary>
        /// 控制台窗口
        /// </summary>
        public class ConsoleWindow : IDebuggerWindow
        {
            private readonly LinkedList<LogNode> _logs = new LinkedList<LogNode>(); //保存日志

            //过滤
            private bool _infoFilter = true;
            private bool _warningFilter = true;
            private bool _errorFilter = true;
            private bool _exceptionFilter = true;

            private int _infoCount = 0;
            private int _warningCount = 0;
            private int _errorCount = 0;
            private int _exceptionCount = 0;

            private bool _lockScroll = true;
            private Vector2 _logScrollPosition = Vector2.zero;
            private Vector2 _stackScrollPosition = Vector2.zero;
            private LinkedListNode<LogNode> _selectedNode = null;

            //log 是否使用独立视图。独立视图中，日志详情会占据整个窗口
            // private bool _independentView = false;
            // private bool _maximize = false;

            public void OnStart()
            {
            }

            private string searchText = "";
            private bool enableSearch = false;
            public void OnDraw()
            {
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Clear", GUILayout.Width(70f)))
                    {
                        _logs.Clear();
                    }

                    _lockScroll = GUILayout.Toggle(_lockScroll, "Lock Scroll", GUILayout.Width(90f));

                    enableSearch = GUILayout.Toggle(enableSearch, "Search", GUILayout.Width(65f));


                    GUILayout.FlexibleSpace();
                    // _independentView = GUILayout.Toggle(_independentView, "Independent View", GUILayout.Width(120f));
                    _infoFilter = GUILayout.Toggle(_infoFilter, string.Format("Info ({0})", _infoCount.ToString()));
                    _warningFilter = GUILayout.Toggle(_warningFilter,
                        string.Format("Warning ({0})", _warningCount.ToString()));
                    _errorFilter = GUILayout.Toggle(_errorFilter, string.Format("Error ({0})", _errorCount.ToString()));
                    _exceptionFilter = GUILayout.Toggle(_exceptionFilter,
                        string.Format("Exception ({0})", _exceptionCount.ToString()));
                }
                GUILayout.EndHorizontal();

                if (enableSearch)
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("X", GUILayout.Width(30f)))
                            searchText = "";
                        searchText = GUILayout.TextField(searchText);
                    }
                    GUILayout.EndHorizontal();
                }

                if (_selectedNode == null)
                {
                    DrawLogList();
                }
                else
                {
                    // if (_maximize)
                    // {
                    //     DrawLogDetail(0);
                    // }
                    // else
                    // {
                        DrawLogList();
                        DrawLogDetail(Debugger.Instance.realScreenSize.y / 3f);
                    // }
                }
            }

            //绘制日志内容
            private void DrawLogList()
            {
                GUILayout.BeginVertical("box");
                {
                    if (_lockScroll)
                    {
                        _logScrollPosition.y = float.MaxValue;
                    }

                    _logScrollPosition = GUILayout.BeginScrollView(_logScrollPosition);
                    {
                        bool selected = false;
                        for (LinkedListNode<LogNode> i = _logs.First; i != null; i = i.Next)
                        {
                            switch (i.Value.LogType)
                            {
                                case LogType.Log:
                                    if (!_infoFilter)
                                    {
                                        continue;
                                    }

                                    break;
                                case LogType.Warning:
                                    if (!_warningFilter)
                                    {
                                        continue;
                                    }

                                    break;
                                case LogType.Error:
                                    if (!_errorFilter)
                                    {
                                        continue;
                                    }

                                    break;
                                case LogType.Exception:
                                    if (!_exceptionFilter)
                                    {
                                        continue;
                                    }

                                    break;
                            }

                            if (enableSearch)
                            {
                                if (!i.Value.LogMessage.ToLower().Contains(searchText.ToLower()))
                                    continue;
                            }

                            GUILayout.BeginHorizontal();
                            {
                                //前面的开关
                                if (GUILayout.Toggle(_selectedNode == i,"",GUILayout.ExpandWidth(false),GUILayout.Height(30f)))
                                {
                                    selected = true;
                                    if (_selectedNode != i)
                                    {
                                        _selectedNode = i;
                                        _stackScrollPosition = Vector2.zero;
                                    }
                                }
                                //绘制这个日志
                                GUILayout.Label(GetLogString(i.Value));
                            }
                            GUILayout.EndHorizontal();
                        }

                        if (!selected)
                        {
                            _selectedNode = null;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            private void DrawLogDetail(float height)
            {
                if(_selectedNode == null)
                    return;
                if(height == 0)
                    GUILayout.BeginVertical("box");
                else
                    GUILayout.BeginVertical("box",GUILayout.Height(height));
                    {
                        _stackScrollPosition = GUILayout.BeginScrollView(_stackScrollPosition);
                        {
                            GUILayout.BeginHorizontal();
                            Color32 color = GetLogStringColor(_selectedNode.Value.LogType);
                            GUILayout.Label(string.Format("<color=#{0}{1}{2}{3}><b>{4}</b></color>",
                                color.r.ToString("x2"),
                                color.g.ToString("x2"), color.b.ToString("x2"), color.a.ToString("x2"),
                                _selectedNode.Value.LogMessage));

                            // if (GUILayout.Button("Maximize", GUILayout.Width(80f), GUILayout.Height(30f)))
                            // {
                            //     _maximize = !_maximize;
                            // }

                            // if (GUILayout.Button("Back", GUILayout.Width(60f), GUILayout.Height(30f)))
                            // {
                            //     _selectedNode = null;
                            //     GUIUtility.ExitGUI();
                            // }

                            if (GUILayout.Button("COPY", GUILayout.Width(60f), GUILayout.Height(30f)))
                            {
                                TextEditor textEditor = new TextEditor();
                                textEditor.text = string.Format("{0}\n\n{1}", _selectedNode.Value.LogMessage,
                                    _selectedNode.Value.StackTrack);
                                textEditor.OnFocus();
                                textEditor.Copy();
                            }

                            GUILayout.EndHorizontal();
                            GUILayout.Label(_selectedNode.Value.StackTrack);
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
            }


            private string GetLogString(LogNode logNode)
            {
                Color32 color = GetLogStringColor(logNode.LogType);
                return string.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}>{4:[HH点mm分ss秒fff]}:{5}</color>",
                    color.r, color.g, color.b, color.a, logNode.LogTime, logNode.LogMessage);
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

            private sealed class LogNode
            {
                private readonly DateTime m_LogTime;
                private readonly LogType m_LogType;
                private readonly string m_LogMessage;
                private readonly string m_StackTrack;

                public LogNode(LogType logType, string logMessage, string stackTrack)
                {
                    m_LogTime = DateTime.Now;
                    m_LogType = logType;
                    m_LogMessage = logMessage;
                    m_StackTrack = stackTrack;
                }

                public DateTime LogTime
                {
                    get { return m_LogTime; }
                }

                public LogType LogType
                {
                    get { return m_LogType; }
                }

                public string LogMessage
                {
                    get { return m_LogMessage; }
                }

                public string StackTrack
                {
                    get { return m_StackTrack; }
                }
            }

            //当接收到log时调用
            public void OnConsoleReceivedLog(string logMessage, string stackTrace, LogType logType)
            {
                if (logType == LogType.Assert)
                {
                    logType = LogType.Error;
                }

                _logs.AddLast(new LogNode(logType, logMessage, stackTrace));
                while (_logs.Count > Debugger.Instance.consoleMaxLine)
                {
                    _logs.RemoveFirst();
                }
            }

            public void RefreshCount()
            {
                _infoCount = 0;
                _warningCount = 0;
                _errorCount = 0;
                _exceptionCount = 0;
                for (LinkedListNode<LogNode> i = _logs.First; i != null; i = i.Next)
                {
                    switch (i.Value.LogType)
                    {
                        case LogType.Log:
                            _infoCount++;
                            break;
                        case LogType.Warning:
                            _warningCount++;
                            break;
                        case LogType.Error:
                            _errorCount++;
                            break;
                        case LogType.Exception:
                            _exceptionCount++;
                            break;
                    }
                }
            }

            public int ErrorCount
            {
                get { return _errorCount; }
            }
        }
    }
}