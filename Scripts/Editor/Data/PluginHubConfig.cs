﻿using UnityEngine;
using PluginHub.Runtime;

namespace PluginHub.Editor
{
    //项目独立的ini配置文件
    //与EditorPrefs不同，保存在ProjectSettings目录下，因此它可以被版本控制系统跟踪
    public static class PluginHubConfig
    {
        internal const string ASSET_DIR = "Assets/Plugins/PluginHubAssets";
        internal const string MODULECONFIT_ASSET_PATH = ASSET_DIR + "/PH_ModuleConfigSO.asset";
        public static string configPath { get; private set; }
        private static bool initialized = false;
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