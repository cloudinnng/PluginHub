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
            SceneView.duringSceneGui += OnSceneGUI;
        }
        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            instance = null;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public static GUIContent tempTipContent = new GUIContent();
        public static string tipContentKey = "";

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!isDisplayed) return;

            // PerformanceTest.Start();
            // {
                Vector2 position = floating ? floatingPosition : rootVisualElement.worldBound.position;
                if (floating) position.y += 25;
                Vector2 size = this.size;
                position.x += 4;
                position.y += size.y - 50;
                size.y = size.x;
                // Debug.Log($"position: {position}, size: {size}");

                Handles.BeginGUI();
                Rect rect = new Rect(position, size);
                GUILayout.BeginArea(rect);
                {
                    GUILayout.Label(tempTipContent);
                }
                GUILayout.EndArea();
                Handles.EndGUI();
            // }
            // PerformanceTest.End("PHSceneOverlay.OnSceneGUI");
        }

        public override void OnGUI()
        {
            // PerformanceTest.Start();
            // {
                SceneViewBookmark.DrawSceneBookmark();
                GUILayout.Space(5);
                SelectionTools.DrawSelectionTools();
            // }
            // PerformanceTest.End("PHSceneOverlay.OnGUI");
        }
    }
}