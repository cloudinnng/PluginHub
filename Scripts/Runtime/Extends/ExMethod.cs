using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 该静态类扩展了一些类方法
    /// </summary>
    public static class ExMethod
    {
        #region 数学扩展方法

        /// <summary>
        /// 扩展方法，返回四元数的长度（模）
        /// </summary>
        public static float Magnitude(this Quaternion quaternion)
        {
            return Mathf.Sqrt(quaternion.x * quaternion.x +
                              quaternion.y * quaternion.y +
                              quaternion.z * quaternion.z +
                              quaternion.w * quaternion.w);
        }

        //float 扩展方法  重映射
        //举例  0.5f.Remap(0, 1 ,0, 10)  返回5   即：将0.5从0到1映射到0到10
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        #endregion

        #region string 扩展方法

        //是不是浮点数字
        public static bool IsFloat(this string value)
        {
            return Regex.IsMatch(value, "^(-?\\d+)(\\.\\d+)?$");
        }

        //是不是整型数字
        public static bool IsInt(this string text)
        {
            return Regex.IsMatch(text, "^-?[0-9]{1,}$");
        }

        //是不是数字
        public static bool IsNumber(this string text)
        {
            return text.IsFloat() || text.IsInt();
        }

        //替换字符串第一个匹配
        public static string ReplaceFirst(this string text, string oldValue, string newValue)
        {
            int pos = text.IndexOf(oldValue);
            if (pos < 0)
            {
                return text;
            }

            return text.Substring(0, pos) + newValue + text.Substring(pos + oldValue.Length);
        }

        #endregion

        #region float 扩展方法

        //保留x位小数
        public static float ToFloat(this float value, int x = 2)
        {
            return float.Parse(value.ToString($"F{x}"));
        }

        #endregion

        #region double 扩展方法


        //保留x位小数
        public static double ToDouble(this double value, int x = 2)
        {
            return double.Parse(value.ToString($"F{x}"));
        }

        #endregion

        #region int 扩展方法

        public static int ToInt(this string value)
        {
            return int.Parse(value);
        }

        public static int ToInt(this float value)
        {
            return (int)value;
        }

        public static int ToInt(this double value)
        {
            return (int)value;
        }

        #endregion
    }
}