using UnityEngine;

namespace PluginHub.Runtime
{
    public class GUIContentEx
    {
        private static GUIContent _temp = new GUIContent();

        public static GUIContent Temp(string text, string tooltip = null)
        {
            _temp.text = text;
            _temp.tooltip = tooltip;
            return _temp;
        }
    }
}