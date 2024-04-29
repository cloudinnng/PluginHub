using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR

[CustomEditor(typeof(ToastManager))]
public class ToastManagerEditor : Editor
{
    private SerializedProperty _autoGuiScale;
    private SerializedProperty _guiScale;
    private SerializedProperty _guiScaleMultiplier;
    private SerializedProperty _horizontalScaleFac;
    private SerializedProperty _verticalScaleFac;
    private SerializedProperty _DefaultShowTime;
    
    
    private void OnEnable()
    {
        _autoGuiScale = serializedObject.FindProperty("autoGuiScale");
        _guiScale = serializedObject.FindProperty("guiScale");
        _guiScaleMultiplier = serializedObject.FindProperty("guiScaleMultiplier");
        _horizontalScaleFac = serializedObject.FindProperty("horizontalScaleFac");
        _verticalScaleFac = serializedObject.FindProperty("verticalScaleFac");
        _DefaultShowTime = serializedObject.FindProperty("DefaultShowTime");
        
    }

    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();
        
        //画脚本行
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((ToastManager)target), typeof(ToastManager), false);
        GUI.enabled = true;
        
        
        serializedObject.Update();
        
        _autoGuiScale.boolValue = EditorGUILayout.Toggle("Auto Gui Scale", _autoGuiScale.boolValue);

        if (_autoGuiScale.boolValue)
        {
            _guiScaleMultiplier.floatValue = EditorGUILayout.FloatField("Gui Scale Multiplier", _guiScaleMultiplier.floatValue);
            _horizontalScaleFac.floatValue = EditorGUILayout.FloatField("Horizontal Scale Factor", _horizontalScaleFac.floatValue);
            _verticalScaleFac.floatValue = EditorGUILayout.FloatField("Vertical Scale Factor", _verticalScaleFac.floatValue);

            GUI.enabled = false;
            _guiScale.floatValue = EditorGUILayout.FloatField("Gui Scale", _guiScale.floatValue);
            GUI.enabled = true;
        }
        else
        {
            _guiScale.floatValue = EditorGUILayout.FloatField("Gui Scale", _guiScale.floatValue);
        }
        
        _DefaultShowTime.floatValue = EditorGUILayout.FloatField("Default Show Time", _DefaultShowTime.floatValue);

        serializedObject.ApplyModifiedProperties();

        GUI.enabled = false;
        EditorGUILayout.Vector2Field("ScreenSizeScaled", ToastManager.ScreenSizeScaled);
        GUI.enabled = true;

        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("发射一个测试Toast"))
        {
            ToastManager.Instance.Show("Test Toast with default time");
        }

        if (GUILayout.Button("发射测试Toast"))
        {
            ToastManager.Instance.Show("Test Toast with default time");
            ToastManager.Instance.Show("中文字测试", 10);
            ToastManager.Instance.Show("，。、；‘、【】", 10);
        }
        GUI.enabled = true;
    }
}

#endif
    
//2022年6月17日 更新
//现在改为继承findsimgleton，需要将其挂在场景游戏对象上
//2022年6月19日 更新
//支持多行toast


//模仿安卓手机的toast 可用于显示提示信息，一段时间后自动消失
[DefaultExecutionOrder(300)]
public class ToastManager : SceneSingleton<ToastManager>
{
    public enum ToastMode
    {
        Time,
        OneFrame,
    }

    public bool autoGuiScale = true;
    public float guiScale = 1;
    [Range(0.01f,2)]
    public float guiScaleMultiplier = 1;
    
    [Tooltip("横屏下toast的缩放比例计算因子")]
    public float horizontalScaleFac = 800;
    [Tooltip("竖屏下toast的缩放比例计算因子")]
    public float verticalScaleFac = 400;
    
    public float DefaultShowTime = 5;
    
    
    private static GUISkin _myCustonSkin;
    private static readonly List<ToastText> ToastTextList = new List<ToastText>();
    private static Vector2 screenSizeScaled;//经过缩放的屏幕分辨率
    public static Vector2 ScreenSizeScaled {get { return screenSizeScaled; }}


    protected void Awake()
    {
        // _myCustonSkin = GUI.skin;
    }

    private void Update()
    {
        foreach (var toast in ToastTextList)
        {
            toast.Update();
        }

        //删除所有应该删除的
        for (int i = ToastTextList.Count - 1; i >= 0; i--)
        {
            if (ToastTextList[i].IsDestory)
            {
                ToastTextList.RemoveAt(i);
            }
        }
    }
    public void OnGUI()
    {
        if (autoGuiScale)
        {
            //按照屏幕分辨率自动计算gui缩放
            if (Screen.width < Screen.height)
                guiScale = Screen.width / verticalScaleFac * guiScaleMultiplier;
            else
                guiScale = Screen.height / horizontalScaleFac * guiScaleMultiplier;
        }
        screenSizeScaled = new Vector2(Screen.width,Screen.height) / guiScale;
        
        //绘制之前进行布局
        Relayout();
        
        Matrix4x4 tmp = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, guiScale));
        
        foreach (var toast in ToastTextList)
        {
            toast.Draw();
        }

        GUI.matrix = tmp;
    }

    public void Show(string text, float duration = -1, bool alsoLogToConsole = false)
    {
        ToastText toast = new ToastText(ToastMode.Time, text);
        toast.duration = duration == -1 ? DefaultShowTime : duration;
        ToastTextList.Add(toast);
        
        if(alsoLogToConsole)
            print(text);
    }
    
    public void Show(string text, bool alsoLogToConsole = false)
    {
        Show(text, -1, alsoLogToConsole);
    }
    
    public void Show(string text)
    {
        Show(text, -1, false);
    }
    

    public void ShowOneFrame(string text, bool alsoLogToConsole = false)
    {
        ToastText toast = new ToastText(ToastMode.OneFrame, text);
        ToastTextList.Add(toast);
        
        if(alsoLogToConsole)
            print(text);
    }


    //计算每一个toast的坐标
    public void Relayout()
    {
        float heightSum = 10;
        float margin = 5;

        for (int i = ToastTextList.Count-1; i >= 0; i--)
        {
            float boxH = ToastTextList[i].boxRect.height;
            heightSum += boxH;
            heightSum += margin;

            ToastTextList[i].Layout(heightSum);
        }
    }
    
    private class ToastText
    {
        private ToastMode mode;
        public float duration;
        private readonly string Text;
        public Rect boxRect;
        private Rect _labelRect;
        public bool IsDestory = false;
        private Vector2 textSize;
        public ToastText(ToastMode mode ,string text)
        {
            this.mode = mode;
            this.Text = text;
        }

        //布局这个toast文本的坐标
        //startY 屏幕底边为0，往上为正
        public void Layout(float startY)
        {
            this.textSize = GUI.skin.label.CalcSize(new GUIContent(this.Text));
            //padding 是盒子内填充的空间大小
            float padding = 15;
            //外面的盒子  左上角是0，0
            boxRect = new Rect((ToastManager.screenSizeScaled.x - textSize.x) / 2 - padding, ToastManager.screenSizeScaled.y - startY, textSize.x + padding * 2,textSize.y + padding * 2);

            float textWidthOffset = 6;
            //中间的文字大小
            _labelRect = new Rect(boxRect.x + padding - textWidthOffset, boxRect.y + padding , boxRect.width - padding * 2 + textWidthOffset * 2, boxRect.height - padding * 2);
        }
        
        public void Update()
        {
            if (mode == ToastMode.Time)
            {
                duration -= Time.deltaTime;
                if (duration <= 0)
                    IsDestory = true;
            }
        }
        public void Draw()
        {
            GUI.color = Color.white;
            GUI.Box(boxRect, "", GUI.skin.box);
            GUI.Label(_labelRect, Text, GUI.skin.label);

            if (mode == ToastMode.OneFrame)
            {
                IsDestory = true;
            }
        }
    
    }
}

