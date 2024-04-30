using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    public class RedeemCodeModule : PluginHubModuleBase
    {
        override public string moduleName
        {
            get { return "兑换码模块"; }
        }
        public override ModuleType moduleType => ModuleType.Tool;
        private string userInput;

        protected override void DrawGuiContent()
        {
            if (string.IsNullOrWhiteSpace(userInput))
                userInput = SystemInfo.deviceUniqueIdentifier;
            
            
            userInput = EditorGUILayout.TextField("输入兑换码", userInput);

            string code = GenerateExchangeCode(userInput, adsSalt);

            EditorGUILayout.LabelField("结果", code);

            if (GUILayout.Button("复制结果"))
            {
                EditorGUIUtility.systemCopyBuffer = code;
            }
        }

        public static readonly string adsSalt = "DisableADS_hellottw"; // 为设备生成广告兑换码

        // SystemInfo.deviceUniqueIdentifier;
        public static string GenerateExchangeCode(string deviceUniqueIdentifier, string salt) {
            deviceUniqueIdentifier = deviceUniqueIdentifier.Replace("-", "");
            byte[] bytes = Encoding.UTF8.GetBytes(deviceUniqueIdentifier + salt); // 加入随机字符串
            using (SHA256 sha256 = SHA256.Create()) {
                byte[] hash = sha256.ComputeHash(bytes);
                string hex = BitConverter.ToString(hash).Replace("-", "");
                return hex;
            }
        }
    }
}