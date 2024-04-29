using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//每日登录助手
//为解决游戏中普遍的每日登录问题
public class SimpleDailyLoginHelper
{
    private static string key = "daily-login-key";

    
    /// <summary>
    /// 在需要检查每日登录的地方调用该方法。
    /// 返回值说明
    /// 0 不签到
    /// 1 重新签到
    /// 2 继续签到
    /// </summary>
    public static int Require()
    {
        string lastData = PlayerPrefs.GetString(key,null);
        string currData = DateTime.Now.ToString("yyyyMMdd");
        if (string.IsNullOrWhiteSpace(lastData))//没有上次签到日期，即为首次签到
        {
            PlayerPrefs.SetString(key,currData);
            return 1;
        }
        else
        {
            long lastDataL =long.Parse(lastData);
            long currDataL =long.Parse(currData);
            if (currDataL > lastDataL)
            {
                if (currDataL - lastDataL == 1)
                {
                    PlayerPrefs.SetString(key,currData);
                    return 2;
                }else
                {
                    PlayerPrefs.SetString(key,currData);
                    return 1;
                }
            }else
            {
                return 0;
            }
        }
    }
    
}
