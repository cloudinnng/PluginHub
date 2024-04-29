using System.Collections;
using System.Collections.Generic;
using System.IO;
using PluginHub.Runtime.Extends;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class ScreenshotUtility : MonoBehaviour
{
    public KeyCode screenshotKey = KeyCode.SysReq;//printscreen按钮
    public bool overlayCapture = true;
    public string savePath;
    public string fileName = "screenpreview.png";
    
    void Update()
    {
        if (string.IsNullOrWhiteSpace(savePath))
            savePath = Directory.GetCurrentDirectory();
        
        if (Application.isPlaying)
        {
            if (Input.GetKeyDown(screenshotKey))
            {
                if(!overlayCapture)
                    fileName = $"screenpreview_{TimeEx.GetPreciseTimeStr()}.png";

                string path = Path.Combine(savePath, fileName);
                //TakeScreenshot
                ScreenCapture.CaptureScreenshot(path);
                StartCoroutine(Delay(path));
            }
        }
    }

    IEnumerator Delay(string path)
    {
        yield return null;
        ToastManager.Instance.Show($"屏幕截屏已保存{path}", -1,true);
    }
}