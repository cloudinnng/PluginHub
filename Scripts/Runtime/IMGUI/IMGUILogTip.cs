using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
// 显示在最上层的提示语,一段时间后消失
// 捕获错误日志来显示，以提示用户
// 想法来自ToastManager
public class IMGUILogTip : IMGUIManager.IIMGUI
{
    public bool alwaysShow = false;// 测试用
    public string tipText = "Default Tip";
    private Color _tipColor = Color.white;
    private GUIContent tempContent = new GUIContent();
    private float _alpha = 0;


    private void OnEnable()
    {
        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
    {
        Color color;
        switch (type)
        {
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                color = Color.red;
                break;
            case LogType.Warning:
                color = Color.yellow;
                break;
            case LogType.Log:
                color = Color.white;
                break;
            default:
                color = Color.white;
                break;
        }
        Show(condition, color);
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    public void Show(string text, Color color)
    {
        tipText = text;
        _tipColor = color;
        _alpha = 0;
        StartCoroutine(Show());
    }

    private IEnumerator Show()
    {
        yield return new WaitForEndOfFrame();
        _alpha = 2;
    }

    public override void IMGUIDraw()
    {
        tempContent.text = tipText;
        Vector2 textSize = GUI.skin.label.CalcSize(tempContent);
        Vector2 areaSize = textSize + new Vector2(GUI.skin.box.padding.horizontal, GUI.skin.box.padding.vertical);

        Vector2 screenSize = IMGUIManager.Instance.ScreenSize(localGUIScale);
        Rect area = new Rect(screenSize.x / 2 - areaSize.x / 2, screenSize.y / 2 - areaSize.y / 2, areaSize.x, areaSize.y);

        _tipColor.a = alwaysShow ? 1 : Mathf.Clamp01(_alpha);
        GUI.color = _tipColor;
        GUILayout.BeginArea(area, GUI.skin.box);
        {
            GUILayout.Label(tipText, GUILayout.Width(areaSize.x), GUILayout.Height(areaSize.y));
        }
        GUILayout.EndArea();
        GUI.color = Color.white;
    }

    public override bool IMGUIOfferLeftSideDraw => false;

    private void Update()
    {
        if (_alpha > 0)
        {
            _alpha -= Time.deltaTime;
            if (_alpha < 0)
            {
                _alpha = 0;
            }
        }
    }

}
