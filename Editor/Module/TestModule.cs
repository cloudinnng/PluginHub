using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class TestModule : PluginHubModuleBase
    {

        private Material _material;


        public override string moduleDescription { get; } = "测试";
        protected override void DrawGuiContent()
        {
            _material = EditorGUILayout.ObjectField(_material, typeof(Material), true) as Material;

            if (GUILayout.Button("Set Smoothness"))
            {
                _material.SetFloat("_Smoothness", 0.0f);
            }
        }
    }
}