using System;
using UnityEngine;
using static PluginHub.Runtime.Debugger;

namespace PluginHub.Runtime
{
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UnmannedDetection))]
    public class UnmannedDetectionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUI.enabled = false;
            UnityEditor.EditorGUILayout.FloatField("Timer", UnmannedDetection.timer.ToFloat(1));
            GUI.enabled = true;
            // 刷新检视面板以便实时看到timer的变化
            UnityEditor.EditorUtility.SetDirty(target);
        }
    }

#endif


    //无人检测
    //很多应用需要在没人操作时自动回到主页或者执行一些其他操作，该脚本用于检测无人状态。只需订阅 OnHumanGone 事件即可。
    public class UnmannedDetection : MonoBehaviour, IDebuggerOnScreenUI
    {
        [Tooltip("该时长无操作后认为是无人状态,单位秒")]
        [SerializeField]
        private float noOperationTime = 60;

        public bool showDebugUI = true;

#region 提供事件接口

        //【无人状态下】每隔 noOperationTime 秒触发一次
        public static event Action OnUnmanedTrigger;
        // 刚【进入无人状态】时触发一次
        public static event Action OnHumanGone;
        // 刚【有人回来】时触发一次
        public static event Action OnHumanBack;
        // 外部代码提供的是否有人的检测函数，例如使用LeapMotion，Kinect等，可以传递该函数表示有人存在。
        // 有人存在时应返回true，无人存在时应返回false。
        public static Func<bool> ExternalHumanExistFunc = () => false;

#endregion

        private static bool humanExist = false;
        public static float timer { get; private set; } = 0;

        private void Update()
        {
            timer += Time.deltaTime;

            //任何键盘、鼠标、触摸、滚轮操作都会重置计时器
            if (InputEx.anyKey || InputEx.touchCount > 0 || InputEx.GetMouseButton(0) || InputEx.GetMouseButton(1) ||
                InputEx.GetMouseButton(2) || InputEx.GetAxis("Mouse ScrollWheel") != 0 || InputEx.GetAxis("Mouse X") != 0 ||
                InputEx.GetAxis("Mouse Y") != 0 || ExternalHumanExistFunc?.Invoke() == true)
            {
                timer = 0;

                if (!humanExist)
                {
                    humanExist = true;
                    OnHumanBack?.Invoke();
                }
            }

            if (timer > noOperationTime)
            {
                timer = 0;
                OnUnmanedTrigger?.Invoke();
                if (humanExist)
                {
                    humanExist = false;
                    OnHumanGone?.Invoke();
                }
            }
        }

        public int OnScreenUIOrder => -1000;
        public void OnScreenUIDraw(float globalGUIScale)
        {
            if (!showDebugUI)
            {
                return;
            }
            GUILayout.Label($"UnmannedDetection: humanExist: {humanExist}, Timer: {timer:F1} / {noOperationTime:F1} s");
        }
    }
}