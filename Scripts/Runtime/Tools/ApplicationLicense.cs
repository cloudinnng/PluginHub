using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PluginHub.Runtime
{

    #if UNITY_EDITOR
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build.Reporting;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [CustomEditor(typeof(ApplicationLicense))]
    public class ApplicationLicenseEditor : Editor
    {
        private const string TempSceneFolder = "Assets/Temp/ApplicationLicenseAdminBuild";
        private const string TempScenePath = TempSceneFolder + "/AdminLicense.unity";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ApplicationLicense applicationLicense = target as ApplicationLicense;
            applicationLicense.DrawAdminGUI();

            if (GUILayout.Button("构建管理员包"))
            {
                BuildAdminPackage();
            }
        }

        /// <summary>
        /// 创建仅含 ApplicationLicense（adminMode=true）的临时场景，单独打包后打开输出目录。
        /// </summary>
        private static void BuildAdminPackage()
        {
            Scene previousActiveScene = default;
            bool restorePreviousScene = false;
            try
            {
                previousActiveScene = SceneManager.GetActiveScene();
                restorePreviousScene = previousActiveScene.IsValid();

                if (!Directory.Exists(TempSceneFolder))
                    Directory.CreateDirectory(TempSceneFolder);

                // 附加空场景，避免打断当前正在编辑的场景
                Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SetActiveScene(tempScene);

                GameObject go = new GameObject("ApplicationLicense");
                ApplicationLicense license = go.AddComponent<ApplicationLicense>();

                SerializedObject so = new SerializedObject(license);
                SerializedProperty adminModeProp = so.FindProperty("adminMode");
                if (adminModeProp != null)
                    adminModeProp.boolValue = true;
                else
                    Debug.LogWarning("[ApplicationLicense] 未找到 adminMode 字段，管理员包可能无法正常工作");
                so.ApplyModifiedPropertiesWithoutUndo();

                if (!EditorSceneManager.SaveScene(tempScene, TempScenePath))
                {
                    Debug.LogError("[ApplicationLicense] 临时场景保存失败，已取消构建");
                    EditorSceneManager.CloseScene(tempScene, true);
                    return;
                }

                EditorSceneManager.CloseScene(tempScene, true);
                AssetDatabase.Refresh();

                string exeName = $"{Application.productName}_LicenseAdmin";
                string buildFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "LicenseAdmin"));
                Directory.CreateDirectory(buildFolder);

                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                string locationPathName = GetBuildLocationPath(buildFolder, exeName, target);

                Debug.Log($"[ApplicationLicense] 开始构建管理员包，场景={TempScenePath}，输出={locationPathName}");

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = new[] { TempScenePath },
                    locationPathName = locationPathName,
                    target = target,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[ApplicationLicense] 管理员包构建成功: {buildFolder}");
                    EditorUtility.RevealInFinder(buildFolder);
                }
                else
                {
                    Debug.LogError($"[ApplicationLicense] 管理员包构建失败: {report.summary.result}");
                }
            }
            finally
            {
                CleanupTempAdminScene();

                if (restorePreviousScene)
                    EditorSceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private static string GetBuildLocationPath(string buildFolder, string exeName, BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(buildFolder, $"{exeName}.app");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(buildFolder, exeName);
                default:
                    return Path.Combine(buildFolder, $"{exeName}.exe");
            }
        }

        private static void CleanupTempAdminScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TempScenePath) != null)
                AssetDatabase.DeleteAsset(TempScenePath);

            if (Directory.Exists(TempSceneFolder)
                && Directory.GetFileSystemEntries(TempSceneFolder).Length == 0)
            {
                AssetDatabase.DeleteAsset(TempSceneFolder);
            }
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
        public bool adminMode = false;
        private static string licensePrivateKey = "hellottw.pluginhub";
        public string machineCode => SystemInfo.deviceUniqueIdentifier;
        public string saveKey => $"{Application.companyName}_{Application.productName}_License";
        public string licenseUserSaved
        {
            get { return PlayerPrefs.GetString(saveKey); }
            set { PlayerPrefs.SetString(saveKey, value); }
        }

        private string userInputAppIdentifier;
        private string userInputMachineCode;

        private void Start()
        {
            // 管理员包跳过 License 校验，避免本机已有 License 时组件被 Destroy 导致工具界面消失
            if (adminMode)
            {
                Debug.Log("[ApplicationLicense] 管理员模式，跳过 License 校验");
                return;
            }

            VerifyLicense();
        }

        public void DrawAdminGUI()
        {
            ApplicationLicense applicationLicense = this;
            if (GUILayout.Button($"本机机器码: {applicationLicense.machineCode}", GUI.skin.label))
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
                GUILayout.Label("输入应用标识符", GUILayout.Width(100));
                userInputAppIdentifier = GUILayout.TextField(userInputAppIdentifier, GUILayout.Width(150));
                if (GUILayout.Button("粘贴", GUILayout.ExpandWidth(false)))
                    userInputAppIdentifier = GUIUtility.systemCopyBuffer;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("输入机器码", GUILayout.Width(100));
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
                GUILayout.Label("License", GUILayout.Width(100));
                GUILayout.TextField(ApplicationLicense.GetLicenseCorrect(userInputAppIdentifier,userInputMachineCode), GUILayout.Width(150));
                if (GUILayout.Button("复制",GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = ApplicationLicense.GetLicenseCorrect(userInputAppIdentifier,userInputMachineCode);
            }
            GUILayout.EndHorizontal();


            if (GUILayout.Button("删除本机该应用编辑器 License"))
                PlayerPrefs.DeleteKey(applicationLicense.saveKey);
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
            // Debug.Log($"licenseUserSaved：{licenseUserSaved}");
            // Debug.Log($"Correct License：{GetLicenseCorrect(ApplicationIdentifier, machineCode)}");
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

        // 参考分辨率（类似 Canvas Scaler → Scale With Screen Size）
        private const float RefScreenWidth = 800f;
        private const float RefScreenHeight = 600f;
        // 0=偏宽，1=偏高，0.5=折中（类似 Match Width Or Height）
        [SerializeField]
        [Range(0, 1)]
        private float matchWidthOrHeight = 0.5f;

        private const float PanelWidth = 500f;
        private const float PanelHeight = 500f;
        private const float PanelPadding = 24f;
        private const float RowHeight = 32f;
        private const float TextFieldHeight = 36f;

        // License 面板 GUIStyle 懒加载缓存（OnGUI 内创建，不写入 GUI.skin）
        private static GUIStyle licenseTitleStyle;
        private static GUIStyle licenseBodyStyle;
        private static GUIStyle licenseInfoStyle;

        private static void EnsureLicensePanelStyles()
        {
            if (licenseTitleStyle != null)
                return;

            licenseTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            licenseBodyStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 14
            };
            licenseInfoStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 14
            };
        }

        /// <summary>进入参考分辨率坐标系（GUI.matrix 缩放并居中）。</summary>
        private void BeginScreenScaleGui(out Matrix4x4 prevMatrix)
        {
            float scaleW = Screen.width / RefScreenWidth;
            float scaleH = Screen.height / RefScreenHeight;
            float scale = Mathf.Lerp(scaleW, scaleH, matchWidthOrHeight);
            float offsetX = (Screen.width - RefScreenWidth * scale) * 0.5f;
            float offsetY = (Screen.height - RefScreenHeight * scale) * 0.5f;

            prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));
        }

        private static void EndScreenScaleGui(Matrix4x4 prevMatrix)
        {
            GUI.matrix = prevMatrix;
        }

        /// <summary>在参考分辨率下计算居中面板（固定设计尺寸，由 matrix 负责缩放）。</summary>
        private static Rect GetLicensePanelRect(out Rect contentRect)
        {
            float panelX = (RefScreenWidth - PanelWidth) * 0.5f;
            float panelY = (RefScreenHeight - PanelHeight) * 0.5f;
            Rect panelRect = new Rect(panelX, panelY, PanelWidth, PanelHeight);
            contentRect = new Rect(
                panelRect.x + PanelPadding,
                panelRect.y + PanelPadding,
                panelRect.width - PanelPadding * 2f,
                panelRect.height - PanelPadding * 2f);
            return panelRect;
        }

        private void OnGUI()
        {
            // 遮罩用屏幕像素绘制，不受 GUI.matrix 影响
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            for (int i = 0; i < 10; i++)
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.color = prevColor;

            BeginScreenScaleGui(out Matrix4x4 prevMatrix);
            try
            {
                Rect panelRect = GetLicensePanelRect(out Rect contentRect);
                GUI.Box(panelRect, GUIContent.none);

                GUILayout.BeginArea(contentRect);
                GUILayout.BeginVertical();

                if (adminMode)
                {
                    // 管理员模式：仅绘制 Inspector 工具面板，不显示 License 注册 UI
                    DrawAdminGUI();
                }
                else
                {
                    EnsureLicensePanelStyles();

                    GUILayout.Label("License无效,请联系开发者获取License", licenseTitleStyle);
                    GUILayout.Space(8f);
                    GUILayout.Label($"应用程序标识符: {ApplicationIdentifier}", licenseBodyStyle);
                    GUILayout.Space(4f);
                    GUILayout.Label($"本机机器码: {machineCode}", licenseBodyStyle);
                    GUILayout.Space(10f);

                    if (GUILayout.Button("复制应用标识符和机器码", GUILayout.Height(RowHeight)))
                        GUIUtility.systemCopyBuffer = $"{ApplicationIdentifier}\n{machineCode}";

                    GUILayout.Space(12f);
                    GUILayout.Label("请输入 License", licenseBodyStyle);
                    licenseUserSaved = GUILayout.TextField(licenseUserSaved, GUILayout.Height(TextFieldHeight));

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("粘贴", GUILayout.Height(RowHeight), GUILayout.Width(contentRect.width * 0.28f)))
                        licenseUserSaved = GUIUtility.systemCopyBuffer;
                    if (GUILayout.Button("Submit", GUILayout.Height(RowHeight)))
                    {
                        if (!VerifyLicense())
                            systemInfo = "License无效";
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6f);
                    if (!string.IsNullOrEmpty(systemInfo))
                        GUILayout.Label(systemInfo, licenseInfoStyle);
                }

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
            finally
            {
                EndScreenScaleGui(prevMatrix);
            }
            // GUILayout.Label($"正确License: {GetLicenseCorrect(ApplicationIdentifier, machineCode)}");
        }
    }
}