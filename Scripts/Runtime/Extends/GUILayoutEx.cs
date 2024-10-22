using UnityEngine;

namespace PluginHub.Runtime
{
    public static class GUILayoutEx
    {
        //TODO implement DropDown
        public static int DropDown(string text, string[] options)
        {
            if (GUILayout.Button(text))
            {
            }

            return 0;
        }
    }
}