using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PluginHub.Editor
{
    // 这是一个 SceneView Overlay条
    // 在场景视图中按～键（波浪线键）然后选择PH Scene Overlay 可以显示这个条
    [Overlay(typeof(SceneView), "PluginHub Scene Overlay")]
    public class PHSceneOverlay : IMGUIOverlay
    {
        public static PHSceneOverlay instance;
        public bool isDisplayed => displayed;
        public override void OnCreated()
        {
            base.OnCreated();
            instance = this;
            SceneView.duringSceneGui += OnSceneGUIHoverTip;

            // 移除 Unity Overlay 自带的 Hover 提示，以免遮挡我们的Hover提示
            rootVisualElement.tooltip = "";
        }
        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            instance = null;
            SceneView.duringSceneGui -= OnSceneGUIHoverTip;
        }

        // 绘制鼠标悬停提示
        public static GUIContent tempTipContent = new GUIContent();
        public static string tipContentKey = "";
        private void OnSceneGUIHoverTip(SceneView sceneView)
        {
            if (!isDisplayed) return;
            if (tempTipContent == null || (tempTipContent.image == null && string.IsNullOrEmpty(tempTipContent.text))) return;

            // 计算显示原点
            Vector2 originPosition = floating ? floatingPosition : rootVisualElement.worldBound.position;
            // Vector2 originPosition = floatingPosition;
            Vector2 overlaySize = this.size;
            originPosition.x += 2;
            originPosition.y += overlaySize.y - 20;
            if (!floating)
                originPosition.y -= 25;

            // Debug.Log($"position: {position}, size: {size}");
            Handles.BeginGUI();
            {
                if (!string.IsNullOrEmpty(tempTipContent.text))// 画文字
                {
                    Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(tempTipContent.text));
                    Rect backRect = new Rect(originPosition.x, originPosition.y, textSize.x + 10, textSize.y + 10);
                    Rect textRect = new Rect(backRect.x + 5, backRect.y + 5, backRect.width - 10, backRect.height - 10);
                    GUI.Button(backRect, "");
                    GUI.Label(textRect, tempTipContent);
                }
                else // 画图片
                {
                    overlaySize.y = overlaySize.x;
                    Rect backRect = new Rect(originPosition, overlaySize);
                    Rect imageRect = new Rect(originPosition.x + 5, originPosition.y + 5, overlaySize.x - 10, overlaySize.y - 10);
                    GUI.Button(backRect, "");
                    GUI.Label(imageRect, tempTipContent);
                }
            }
            Handles.EndGUI();
        }

        public override void OnGUI()
        {
            SceneViewBookmark.DrawSceneBookmark();
            GUILayout.Space(5);
            SelectionTools.DrawSelectionTools();
        }
    }
}