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
    [Overlay(typeof(SceneView), "PH Scene Overlay")]
    public class PHSceneOverlay : IMGUIOverlay
    {

        public static PHSceneOverlay instance;
        public bool isDisplayed => displayed;
        public override void OnCreated()
        {
            base.OnCreated();
            instance = this;
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            instance = null;
        }

        public override void OnGUI()
        {
            SceneViewBookmark.DrawSceneBookmark();

            GUILayout.Space(5);

            SelectionTools.DrawSelectionTools();
        }
    }
}