using System;
using PluginHub.Runtime;
using UnityEngine;

namespace PluginHub.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(PHTestComponent),true)]
    [CanEditMultipleObjects]
    public class PHTestComponentEditor : Editor
    {
        private Editor _editor;
        private void OnEnable()
        {
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            foreach (var target in targets)
            {
                GUILayout.Label(target.name);
                EditorGUILayout.ObjectField("Script", target, typeof(MonoScript), false);
            }
        }
    }
}