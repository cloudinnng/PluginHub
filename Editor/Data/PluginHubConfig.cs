using PluginHub.Helper;
using UnityEngine;

namespace PluginHub.Data
{
    //项目独立的配置文件
    public static class PluginHubConfig
    {
        private static bool initialized = false;
        private static string configPath;
        private static INIParser iniParser;

        static PluginHubConfig()
        {
            if (!initialized)
            {
                //eg: E:\unityproject\TopwellCustomPattern\ProjectSettings\PHConfig.ini
                configPath = Application.dataPath + "/../ProjectSettings/PHConfig.ini";
                // Debug.Log(configPath);
                iniParser = new INIParser();
                initialized = true;
            }
        }


        public static string ReadConfig(string section, string key, string defaultValue)
        {
            iniParser.Open(configPath);
            string value = iniParser.ReadValue(section, key, defaultValue);
            iniParser.Close();
            return value;
        }

        public static void WriteConfig(string section, string key, string value)
        {
            iniParser.Open(configPath);
            iniParser.WriteValue(section, key, value);
            iniParser.Close();
        }
    }
}