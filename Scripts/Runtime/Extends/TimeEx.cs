using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    public static class TimeEx
    {
        //获取时间戳(从1970年1月1日0点0分0秒开始计算，到现在经过的秒数)
        //eg: 1658907538
        public static long GetTimeStamp()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }

        ///获取精确时间字符串,不会重复。
        ///eg: 202207271538071166
        public static string GetPreciseTimeStr()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssffff");
        }

        ///获取简短时间字符串，精确到秒，并格式化为好看的格式
        /// eg: 2022-08-18 14-56-30
        public static string GetTimeStrToSecondPretty()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
        }

        // MTom : month to minute
        public static string GetTimeStrMTomPrettyCN()
        {
            return DateTime.Now.ToString("MM月dd日 HH-mm");
        }

        //
        public static string GetDateStr()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }
    }
}