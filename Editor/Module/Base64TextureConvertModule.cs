using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace PluginHub.Module
{
    public class Base64TextureConvertModule : PluginHubModuleBase
    {
        private string _textTmp;
        private Texture2D _texture2DTmp;

        protected override void DrawGuiContent()
        {
            GUIStyle style = new GUIStyle(EditorStyles.textField);
            style.wordWrap = true;
            _textTmp = EditorGUILayout.TextArea(_textTmp, style, GUILayout.Height(80));


            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Copy"))
                {
                    //copy to system clipboard
                    EditorGUIUtility.systemCopyBuffer = _textTmp;
                }

                if (GUILayout.Button("Clear"))
                {
                    Clear();
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("To Base64"))
            {
                //show select file dialog
                string path = EditorUtility.OpenFilePanel("Select Image", "", "png;jpg;jpeg");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _textTmp = Convert.ToBase64String(File.ReadAllBytes(path));
                }
            }

            if (GUILayout.Button("To Image"))
            {
                if (_texture2DTmp != null)
                    UnityEngine.Object.DestroyImmediate(_texture2DTmp);

                _texture2DTmp = Base64ToTexture(_textTmp);
            }

            if (_texture2DTmp != null)
                EditorGUILayout.ObjectField(_texture2DTmp, typeof(Texture2D), false);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            Clear();
        }

        private void Clear()
        {
            _textTmp = "";
            if (_texture2DTmp != null)
                UnityEngine.Object.DestroyImmediate(_texture2DTmp);
        }

        //convert base64 to texture
        public Texture2D Base64ToTexture(string base64Str)
        {
            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.LoadImage(Convert.FromBase64String(base64Str));
            return texture;
        }

    }
}