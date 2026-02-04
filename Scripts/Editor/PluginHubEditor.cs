using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace PluginHub.Editor
{
    public static class PluginHubEditor
    {
        #region 常量定义 (Constants)

        //选中的颜色
        public static readonly Color SelectedColor = new Color(0.572549f, 0.7960784f, 1f, 1f);
        public static readonly Color Red = new Color(0.8f, 0.2f, 0.2f, 1f);
        public static readonly float NormalBtnHeight = 19f;
        private static readonly Vector2 IconBtnSize = new Vector2(28f, 19f);

        //ICON按钮的LayoutOption
        //用法示例： GUILayout.Button("X", PluginHubEditor.IconBtnLayoutOptions);
        public static readonly GUILayoutOption[] IconBtnLayoutOptions = new[] { GUILayout.Width(IconBtnSize.x), GUILayout.Height(IconBtnSize.y) };

        //项目唯一前缀(每个项目不一样，这样可以为每个项目存储不同的偏好)，用于存储 EditorPrefs
        public static readonly string ProjectUniquePrefix = $"PH_{Application.companyName}_{Application.productName}";

        #endregion


        #region GUI皮肤和样式 (GUI Skin / Style)

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

        #region 布局辅助函数 (Layout Helper Functions)

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


        #region GUI内容创建

        private static readonly GUIContent tempTextContent = new GUIContent();
        private static GUIContent tempIconContent;

        //获取一个带有tooltip的GuiContent
        public static GUIContent GuiContent(string text, string tooltip = "")
        {
            tempTextContent.text = text;
            tempTextContent.tooltip = tooltip;
            return tempTextContent;
        }

        //获取一个带Icon的GUIContent，也可以附加tooltip
        //各种icon参见   https://unitylist.com/p/5c3/Unity-editor-icons
        public static GUIContent IconContent(string iconStr, string text = "", string tooltip = "")
        {
            tempIconContent = EditorGUIUtility.IconContent(iconStr);
            tempIconContent.text = text;
            tempIconContent.tooltip = tooltip;
            return tempIconContent;
        }

        private readonly static GUIContent sceneViewTextGUIContentTemp = new();
        //在场景视图中画出文字
        // 需要使用下面的代码包围起来
        // OnSceneGUI(SceneView sceneView){
        // Handles.BeginGUI();
        // here
        // Handles.EndGUI();
        // }
        public static void DrawSceneViewText(Vector3 worldPos, string text, Vector2 screenOffset = default)
        {
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos) + screenOffset;

            sceneViewTextGUIContentTemp.text = text;
            //caculate text width
            Vector2 textSize = EditorStyles.boldLabel.CalcSize(sceneViewTextGUIContentTemp);

            Rect rect = new Rect(screenPos.x - textSize.x / 2, screenPos.y - textSize.y / 2, textSize.x, textSize.y);
            GUI.color = Color.black;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, text, EditorStyles.boldLabel);
        }

        #endregion


        #region 场景视图相机动画

        private static Quaternion _startRotation;
        private static Quaternion _rotateTarget;
        private static float _animationTime = 0;
        private static bool _isAnimationRunning = false;

        //让场景视图相机看向目标物体，且有补间动画
        public static void RotateSceneViewCameraToTarget(Transform target)
        {
            if (SceneView.lastActiveSceneView == null) return; //编辑器不存在场景视图
            if (_isAnimationRunning) return;

            InitializeCameraAnimation(target);
        }

        private static void InitializeCameraAnimation(Transform target)
        {
            Camera camera = SceneView.lastActiveSceneView.camera;
            Vector3 positionSave = camera.transform.position;
            _startRotation = SceneView.lastActiveSceneView.rotation;
            _rotateTarget = Quaternion.LookRotation(target.position - camera.transform.position);
            _animationTime = 0;
            SceneView.lastActiveSceneView.size = 0.01f;
            SceneView.lastActiveSceneView.pivot = positionSave;
            EditorApplication.update += UpdateCameraRotation;
            _isAnimationRunning = true;
        }

        private static void UpdateCameraRotation()
        {
            _animationTime += 0.02f; //Anim speed
            if (_animationTime > 1) _animationTime = 1;

            Quaternion rotation = Quaternion.Slerp(_startRotation, _rotateTarget, _animationTime);
            rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0); //去除z旋转
            SceneView.lastActiveSceneView.rotation = rotation;

            if (_animationTime == 1)
            {
                EditorApplication.update -= UpdateCameraRotation;
                _isAnimationRunning = false;
            }
        }

        #endregion


        #region 开发中功能 (In Development)

        private static Rect _viewWidthRect;

        //获取布局空间的可用宽度
        //实现原理：丢一个看不见的假label进去占满宽度，然后用GetlastRect获取其宽度
        //来自：https://forum.unity.com/threads/editorguilayout-get-width-of-inspector-window-area.82068/。最后一个留言
        public static float GetViewWidth()
        {
            PlaceInvisibleLabelForWidthCalculation();
            CaptureViewWidthOnRepaint();
            return _viewWidthRect.width;
        }

        private static void PlaceInvisibleLabelForWidthCalculation()
        {
            GUILayout.Label("hack", GUILayout.MaxHeight(0));
        }

        private static void CaptureViewWidthOnRepaint()
        {
            if (Event.current.type == EventType.Repaint)
            {
                // hack to get real view width
                _viewWidthRect = GUILayoutUtility.GetLastRect();
            }
        }

        #endregion
    }
}