using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;

//在timeline录屏的时候 输出进度作为提示，录制完成后自动退出播放模式
[RequireComponent(typeof(PlayableDirector))]
public class TimelineRecordTool : MonoBehaviour
{
    private PlayableDirector playableDirector;
    private bool _once = true;
    private bool _timelineIsPlayedBefore = false;//曾经播放过timeline

    private void Awake()
    {
        //run only editor
        if (!Application.isEditor)
        {
            Destroy(this);
        }
    }

    void Start()
    {
        if(playableDirector == null)
            playableDirector = GetComponent<PlayableDirector>();
    }

    private void Update()
    {
        if (_once)
        {
            //打印时间轴进度
            if (Time.frameCount % 60 == 0)
            {
                float percent = (float)(playableDirector.time / playableDirector.duration);
                print($"Timeline已走百分比{percent.ToString("F2")}");
                
                if(playableDirector.state == PlayState.Playing)//播放过
                {
                    _timelineIsPlayedBefore = true;
                }
                
                
                if (_timelineIsPlayedBefore)
                {
                    if (playableDirector.state != PlayState.Playing || Mathf.Approximately(percent, 1))
                    {
                        _timelineIsPlayedBefore = false;
                        _once = false;
                        print("检测到时间线走完");
                        StartCoroutine(Delay());
                    }
                }
            }
        }
    }
    
    IEnumerator Delay()
    {
        yield return new WaitForSeconds(1);
        #if UNITY_EDITOR
        Debug.LogWarning("退出播放模式");
        EditorApplication.isPlaying = false;
        #endif
    }
}
