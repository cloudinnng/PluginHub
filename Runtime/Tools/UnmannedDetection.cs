using System;
using PluginHub.Runtime.Extends;
using UnityEngine;
using UnityEngine.Serialization;

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
        //repaint
        UnityEditor.EditorUtility.SetDirty(target);
    }
}

#endif


//无人检测
//很多应用需要在没人操作时自动回到主页或者执行一些其他操作，该脚本用于检测无人状态。只需订阅 OnHumanGone 事件即可。
public class UnmannedDetection : MonoBehaviour
{
    [Tooltip("该时长无操作后认为是无人状态,单位秒")]
    [SerializeField]
    private float noOperationTime = 60;

    //每隔 noOperationTime 秒触发一次，如果有操作则会重置计时器。
    public static event Action OnUnmanedTrigger;

    // 刚进入无人状态时触发一次
    public static event Action OnHumanGone;
    // 有人回来时触发一次
    public static event Action OnHumanBack;


    private static bool humanExist = true;
    public static float timer { get; private set; } = 0;

    private void Update()
    {
        timer += Time.deltaTime;

        //任何键盘、鼠标、触摸、滚轮操作都会重置计时器
        if (Input.anyKey || Input.touchCount > 0 || Input.GetMouseButton(0) || Input.GetMouseButton(1) ||
            Input.GetMouseButton(2) || Input.GetAxis("Mouse ScrollWheel") != 0 || Input.GetAxis("Mouse X") != 0 ||
            Input.GetAxis("Mouse Y") != 0)
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
}