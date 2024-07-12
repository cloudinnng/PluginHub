using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

//
[CustomEditor(typeof(CanvasGroup))]
public class CustomCanvasGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //DrawDefaultInspector();

        CanvasGroup canvasGroupComponent = target as CanvasGroup;
        bool isAlpha0 = Mathf.Approximately(canvasGroupComponent.alpha, 0);
        if (GUILayout.Button(isAlpha0?"Alpha = 1":"Alpha = 0"))
        {
            if (isAlpha0)
                canvasGroupComponent.alpha = 1;
            else
                canvasGroupComponent.alpha = 0;

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}

