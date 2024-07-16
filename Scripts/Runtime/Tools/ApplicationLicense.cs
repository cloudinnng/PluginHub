using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Runtime
{

    #if UNITY_EDITOR
    [CustomEditor(typeof(ApplicationLicense))]
    public class ApplicationLicenseEditor : Editor
    {
        private string userInputAppIdentifier;
        private string userInputMachineCode;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var applicationLicense = target as ApplicationLicense;

            if (GUILayout.Button($"本机机器码: {applicationLicense.machineCode}",GUI.skin.label))
                GUIUtility.systemCopyBuffer = applicationLicense.machineCode;
            if (GUILayout.Button($"应用标识符: {applicationLicense.ApplicationIdentifier}",GUI.skin.label))
                GUIUtility.systemCopyBuffer = applicationLicense.ApplicationIdentifier;
            if (GUILayout.Button($"本机正确License: {ApplicationLicense.GetLicenseCorrect(applicationLicense.ApplicationIdentifier,applicationLicense.machineCode)}",GUI.skin.label))
                GUIUtility.systemCopyBuffer = ApplicationLicense.GetLicenseCorrect(applicationLicense.ApplicationIdentifier,applicationLicense.machineCode);

            GUILayout.Label($"Licence存储Key: {applicationLicense.saveKey}");
            GUILayout.Label($"License存储值: {applicationLicense.licenseUserSaved}");

            bool hasLicense = applicationLicense.IsLicenseValid();
            GUILayout.Label($"本机License是否有效: {hasLicense}");

            GUILayout.Space(20);

            GUILayout.Label("为其它设备生成License,输入机器码");
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("输入应用标识符", GUILayout.Width(80));
                userInputAppIdentifier = GUILayout.TextField(userInputAppIdentifier, GUILayout.Width(150));
                if (GUILayout.Button("粘贴", GUILayout.ExpandWidth(false)))
                    userInputAppIdentifier = GUIUtility.systemCopyBuffer;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("输入机器码", GUILayout.Width(80));
                userInputMachineCode = GUILayout.TextField(userInputMachineCode, GUILayout.Width(150));
                if (GUILayout.Button("粘贴", GUILayout.ExpandWidth(false)))
                    userInputMachineCode = GUIUtility.systemCopyBuffer;
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("粘贴应用标识符和机器码", GUILayout.ExpandWidth(false)))
            {
                string identifierAndMachineCode = GUIUtility.systemCopyBuffer;
                string[] split = identifierAndMachineCode.Split('\n');
                userInputAppIdentifier = split[0];
                userInputMachineCode = split[1];
            }


            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("License", GUILayout.Width(80));
                GUILayout.TextField(ApplicationLicense.GetLicenseCorrect(userInputAppIdentifier,userInputMachineCode), GUILayout.Width(150));
                if (GUILayout.Button("复制",GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = ApplicationLicense.GetLicenseCorrect(userInputAppIdentifier,userInputMachineCode);
            }
            GUILayout.EndHorizontal();


            if (GUILayout.Button("删除本机该应用编辑器 License"))
                PlayerPrefs.DeleteKey(applicationLicense.saveKey);
        }
    }
    #endif


    // 将用户输入的 License 保存到 PlayerPrefs 中
    // 注意：PlayerPrefs在编辑器和构建版本中访问的内容不同
    // 在编辑器中，PlayerPrefs 的路径是 HKEY_CURRENT_USER\SOFTWARE\Unity\UnityEditor\CompanyName\ProjectName，
    // 在构建中，路径是 HKEY_CURRENT_USER\SOFTWARE\CompanyName\ProjectName
    // 因此，在Unity编辑器中输入正确的License注册后，构建版本需要重新输入License注册。

    // 注意，不要使用Application.identifier
    // PC平台编辑器与构建中的Application.identifier不同。PC构建中的Application.identifier是None

    public class ApplicationLicense : MonoBehaviour
    {

        private static string licensePrivateKey = "hellottw.pluginhub";
        public string machineCode => SystemInfo.deviceUniqueIdentifier;
        public string saveKey => $"{Application.companyName}_{Application.productName}_License";
        public string licenseUserSaved
        {
            get { return PlayerPrefs.GetString(saveKey); }
            set { PlayerPrefs.SetString(saveKey, value); }
        }

        private void Start()
        {
            VerifyLicense();
        }

        public static string GetLicenseCorrect(string appIdentifier, string machineCode)
        {
            // License 与以下信息有关：
            // 机器码 + 应用标识符 + 私钥
            string input = appIdentifier + machineCode + licensePrivateKey;

            using (SHA256 sha256 = SHA256.Create())
            {
                // 将输入字符串转换为字节数组
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                // 计算哈希值
                byte[] hashBytes = sha256.ComputeHash(bytes);
                // 将字节数组转换为字符组成的字符串
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }


        public bool IsLicenseValid()
        {
            Debug.Log($"{licenseUserSaved}");
            Debug.Log($"{GetLicenseCorrect(ApplicationIdentifier, machineCode)}");
            return licenseUserSaved == GetLicenseCorrect(ApplicationIdentifier, machineCode);
        }

        public bool VerifyLicense()
        {
            if (IsLicenseValid())
            {
                Debug.Log("License 已验证通过");
                Destroy(this);
                return true;
            }
            return false;
        }

        public string ApplicationIdentifier => $"com.{Application.companyName}.{Application.productName}";

        private string systemInfo;// 系统提示信息
        private void OnGUI()
        {
            for (int i = 0; i < 10; i++)
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

            GUILayout.Label("License无效,请联系开发者获取License");
            GUILayout.Label($"应用程序标识符: {ApplicationIdentifier}");
            GUILayout.Label($"本机机器码: {machineCode}");
            if (GUILayout.Button("复制应用标识符和机器码", GUILayout.ExpandWidth(false)))
                GUIUtility.systemCopyBuffer = $"{ApplicationIdentifier}\n{machineCode}";

            licenseUserSaved = GUILayout.TextField(licenseUserSaved);
            if (GUILayout.Button("粘贴", GUILayout.ExpandWidth(false)))
                licenseUserSaved = GUIUtility.systemCopyBuffer;

            if (GUILayout.Button("Submit"))
            {
                if (!VerifyLicense())
                    systemInfo = "License无效";
            }

            GUILayout.Label(systemInfo);
            // GUILayout.Label($"正确License: {GetLicenseCorrect(ApplicationIdentifier, machineCode)}");

        }
    }
}