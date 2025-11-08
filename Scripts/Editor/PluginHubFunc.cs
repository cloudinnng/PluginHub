using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

# if PH_WINFORMS
using System;
using System.Windows.Forms;
#endif

namespace PluginHub.Editor
{
    //TODO 这个类可以整理一下
    public static class PluginHubFunc
    {
        // static PluginHubFunc() { }

        #region Const

        //可以使用EditorGUIUtility.whiteTexture获取纹理
        // public static readonly Texture2D WhiteTexture = EditorGUIUtility.whiteTexture;

        //选中的颜色
        public static readonly Color SelectedColor = new Color(0.572549f, 0.7960784f, 1f, 1f);
        public static readonly Color Red = new Color(0.8f, 0.2f, 0.2f, 1f); 

        public static readonly float NormalBtnHeight = 19f;
        //定义一个仅能容纳一个icon的小型按钮的尺寸，icon按钮专用
        private static readonly Vector2 iconBtnSize = new Vector2(28f, 19f);

        //ICON按钮的LayoutOption
        //用法示例： GUILayout.Button("X", PluginHubFunc.IconBtnLayoutOptions);
        public static readonly GUILayoutOption[] IconBtnLayoutOptions = new[]
            { GUILayout.Width(iconBtnSize.x), GUILayout.Height(iconBtnSize.y) };

        //项目唯一前缀(每个项目不一样，这样可以为每个项目存储不同的偏好)，用于存储 EditorPrefs
        public static readonly string ProjectUniquePrefix = $"PH_{Application.companyName}_{Application.productName}";

        #endregion


        #region GUI Skin / Style

        private static GUISkin _skinUse;//do not call this, use PHGUISkin instead

        //Resources文件夹中的那个GUISkin
        public static GUISkin PHGUISkinUse
        {
            get
            {
                if (_skinUse == null)
                {
                    GUISkin originSkin = Resources.Load<GUISkin>("PluginHubGUISkin");
                    //复制一份，避免修改原文件;
                    _skinUse = Object.Instantiate(originSkin);
                    Resources.UnloadAsset(originSkin);
                }
                return _skinUse;
            }
        }

        public static GUIStyle GetCustomStyle(string styleName)
        {
            IEnumerable<GUIStyle> styles = PHGUISkinUse.customStyles.Where(s => s.name.Equals(styleName));
            GUIStyle style = styles.FirstOrDefault();
            if (style == null)
                Debug.LogError($"找不到样式：{styleName}");
            return style;
        }

        #endregion

        #region Draw




        //绘制一个拷贝文件按钮，点击后会将文件拷贝到剪贴板,便于粘贴到支持Ctrl+v的应用程序中，如微信或文件管理器。
        //需要注意的是，这个功能只能在Windows平台下使用，因为使用了Windows.Forms的API
        //TODO
        public static void DrawCopyFileButton(string filePath)
        {
# if PH_WINFORMS
            if (GUILayout.Button(PluginHubFunc.Icon("d_TreeEditor.Duplicate", "", $"Duplicate file\n {filePath} to clipboard"),
                    PluginHubFunc.IconBtnLayoutOptions))
            {
                // 创建一个包含文件路径的StringCollection
                System.Collections.Specialized.StringCollection paths = new System.Collections.Specialized.StringCollection();
                paths.Add(filePath);

                // 将文件路径设置到剪切板
                Clipboard.SetFileDropList(paths);
            }
#else
            Debug.LogError("no PH_WINFORMS");
#endif
        }

        #endregion

        #region Layout Helper Functions

        /// <summary>
        /// 在for循环中使用BeginHorizontal()进行水平布局，每行显示numberOfLines个对象，使用该方法判断是否应该开始水平布局
        /// </summary>
        /// <param name="i">循环索引</param>
        /// <param name="numberOfLines">一行按钮的个数，设置一行几个</param>
        /// <returns></returns>
        public static bool ShouldBeginHorizontal(int i, int numberOfLines)
        {
            return i % numberOfLines == 0;
        }

        public static bool ShouldEndHorizontal(int i, int numberOfLines)
        {
            return (i + 1) % numberOfLines == 0;
        }

        #endregion

        #region Function

        //使用递归获取一个Transform的查找路径
        public static void GetFindPath(Transform transform, StringBuilder sb)
        {
            if (transform.parent == null)
            {
                sb.Insert(0, $"/{transform.name}");
                return;
            }

            sb.Insert(0, $"/{transform.name}");
            GetFindPath(transform.parent, sb);
        }

        public static void GC()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private static readonly GUIContent tempTextContent = new GUIContent();

        //获取一个带有tooltip的GuiContent
        public static GUIContent GuiContent(string text, string tooltip = "")
        {
            tempTextContent.text = text;
            tempTextContent.tooltip = tooltip;
            return tempTextContent;
        }

        private static GUIContent tempIconContent;
        //获取一个带Icon的GUIContent，也可以附加tooltip
        public static GUIContent IconContent(string iconStr, string text = "", string tooltip = "")
        {
            //各种icon参见   https://unitylist.com/p/5c3/Unity-editor-icons
            tempIconContent = EditorGUIUtility.IconContent(iconStr);
            tempIconContent.text = text;
            tempIconContent.tooltip = tooltip;
            return tempIconContent;
        }

        #endregion

        #region Alpha Function 测试函数，可能不稳定

        private static Quaternion startRotation;
        private static Quaternion rotateTarget;
        private static float t = 0;
        private static bool animWasRunning = false;

        //让场景视图相机看向目标物体，且有补间动画
        public static void RotateSceneViewCameraToTarget(Transform target)
        {
            if (SceneView.lastActiveSceneView == null) return; //编辑器不存在场景视图
            if (animWasRunning) return;

            Camera camera = SceneView.lastActiveSceneView.camera;
            Vector3 positionSave = camera.transform.position;
            startRotation = SceneView.lastActiveSceneView.rotation;
            rotateTarget = Quaternion.LookRotation(target.position - camera.transform.position);
            t = 0;
            SceneView.lastActiveSceneView.size = 0.01f;
            SceneView.lastActiveSceneView.pivot = positionSave;
            EditorApplication.update += RotateUpdate; //订阅更新函数  得以更新
            animWasRunning = true;
        }

        private static void RotateUpdate()
        {
            //Debug.Log("Anim Slerp update");
            t += 0.02f; //Anim speed
            if (t > 1) t = 1;
            Quaternion rotation = Quaternion.Slerp(startRotation, rotateTarget, t);
            rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0); //去除z旋转
            SceneView.lastActiveSceneView.rotation = rotation;
            if (t == 1)
            {
                EditorApplication.update -= RotateUpdate;
                animWasRunning = false;
            }
        }
        #endregion


        #region in development

        private static Rect _rect;

        //获取布局空间的可用宽度
        //实现原理：丢一个看不见的假label进去占满宽度，然后用GetlastRect获取其宽度
        //来自：https://forum.unity.com/threads/editorguilayout-get-width-of-inspector-window-area.82068/。最后一个留言
        public static float GetViewWidth()
        {
            GUILayout.Label("hack", GUILayout.MaxHeight(0));
            if (Event.current.type == EventType.Repaint)
            {
                // hack to get real view width
                _rect = GUILayoutUtility.GetLastRect();
            }

            return _rect.width;
        }

        #endregion
    }
}