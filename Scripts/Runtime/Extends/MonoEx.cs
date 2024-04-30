using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginHub.Runtime
{
    public static class MonoEx
    {
        private static StringBuilder _stringBuilder = new StringBuilder();

        public static void Printf(this MonoBehaviour mono, params string[] strs)
        {
            _stringBuilder.Clear();
            foreach (var str in strs)
            {
                _stringBuilder.Append(str);
                _stringBuilder.Append(", ");
            }

            Debug.Log(_stringBuilder.ToString());
        }
    }
}