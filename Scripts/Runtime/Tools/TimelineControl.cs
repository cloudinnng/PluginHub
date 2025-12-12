using System;
using System.Collections;
using System.Collections.Generic;
using PluginHub.Runtime;
using UnityEngine;
using UnityEngine.Playables;

namespace PluginHub.Runtime
{
//这个类用来键盘控制Timeline
    public class TimelineControl : MonoBehaviour, Debugger.CustomWindow.IDebuggerCustomWindowGUI
    {
        private PlayableDirector _playableDirector;

        private void Awake()
        {
            _playableDirector = GetComponent<PlayableDirector>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Time.timeScale = 1;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Time.timeScale = 2;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Time.timeScale = 5;
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Time.timeScale = 20;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_playableDirector.state == PlayState.Paused)
                    _playableDirector.Resume();
                else if (_playableDirector.state == PlayState.Playing)
                    _playableDirector.Pause();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                _playableDirector.Stop();
            }
        }

        public int DebuggerDrawOrder { get; set; }

        public void OnDrawDebuggerGUI()
        {
            GUILayout.Label("数字键1,2,3,4: 设置TimeScale,P: 暂停/继续播放,S: 停止播放");
            GUILayout.Label($"TimeScale:{Time.timeScale}");

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label($"播放状态: {_playableDirector.state}");
                GUILayout.FlexibleSpace();
                GUILayout.Label($"播放进度: {_playableDirector.time:F2}s / {_playableDirector.duration:F2}s",
                    GUILayout.Width(200));
            }
            GUILayout.EndHorizontal();
            GUILayout.HorizontalSlider((float)_playableDirector.time, 0, (float)_playableDirector.duration);

            //跳转
            GUILayout.BeginHorizontal();
            {
                for (int i = 0; i < 18; i++)
                {
                    if (GUILayout.Button($"{i:00}"))
                    {
                        _playableDirector.time = _playableDirector.duration / 18f * i;
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Play"))
                {
                    _playableDirector.Play();
                }

                if (GUILayout.Button("Paused"))
                {
                    _playableDirector.Pause();
                }

                if (GUILayout.Button("Resume"))
                {
                    _playableDirector.Resume();
                }

                if (GUILayout.Button("Stop"))
                {
                    _playableDirector.Stop();
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
