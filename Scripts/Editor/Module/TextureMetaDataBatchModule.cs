using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginHub.Editor
{
    //纹理元数据批处理
    //这个模块想制作：拖一个路径进来后一键设置这个路径里所有子纹理的元数据
    public class TextureMetaDataBatchModule : PluginHubModuleBase
    {
        private Object folderObj;
        private string[] texturePaths;

        private int[] textureSizeOptions = new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };

        private TextureImporterType textureImporterType;
        private int maxTextureSize = 2048;

        public override string moduleDescription { get; } = "纹理元数据批处理,这个模块想制作：拖一个路径进来后一键设置这个路径里所有子纹理的元数据";
        public override ModuleType moduleType => ModuleType.Tool;
        protected override void DrawGuiContent()
        {
            if (moduleDebug)
            {
                //show info box
                EditorGUILayout.HelpBox("This module batch all sub texture in this folder to special setting",
                    MessageType.Info);
            }


            if (folderObj == null)
            {
                folderObj = EditorGUILayout.ObjectField(folderObj, typeof(Object), true);
            }

            string path = AssetDatabase.GetAssetPath(folderObj);

            if (!string.IsNullOrWhiteSpace(path))
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(path);
                    if (GUILayout.Button("X", PluginHubEditor.IconBtnLayoutOptions))
                    {
                        folderObj = null;
                    }
                }
                GUILayout.EndHorizontal();
            }


            if (!string.IsNullOrWhiteSpace(path))
            {
                //目录下的所有纹理
                texturePaths = AssetDatabase.FindAssets("t:texture", new string[] { path }); //这个返回的是GUID
                texturePaths = texturePaths.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray(); //转换成资产路径

                GUILayout.Label($"该目录及其子目录下共{texturePaths.Length}张纹理");
            }


            // textureImporterType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", textureImporterType);
            maxTextureSize = EditorGUILayout.IntPopup("Max Texture Size", maxTextureSize,
                textureSizeOptions.Select(i => i.ToString()).ToArray(), textureSizeOptions);


            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;


            GUILayout.Label($"Overrider for {activeBuildTarget}");
            if (GUILayout.Button("Override to Texture"))
            {
                for (int i = 0; i < texturePaths.Length; i++)
                {
                    string cpath = texturePaths[i];
                    TextureImporter texImporter = AssetImporter.GetAtPath(cpath) as TextureImporter;
                    TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                    settings.name = activeBuildTarget.ToString();
                    settings.overridden = true;
                    settings.maxTextureSize = maxTextureSize;
                    texImporter.SetPlatformTextureSettings(settings);
                    AssetDatabase.ImportAsset(cpath);
                }

                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("Deselect Override"))
            {
                for (int i = 0; i < texturePaths.Length; i++)
                {
                    string cpath = texturePaths[i];
                    TextureImporter texImporter = AssetImporter.GetAtPath(cpath) as TextureImporter;
                    TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                    settings.name = activeBuildTarget.ToString();
                    settings.overridden = false;
                    texImporter.SetPlatformTextureSettings(settings);
                    AssetDatabase.ImportAsset(cpath);
                }

                AssetDatabase.Refresh();
            }
        }
    }
}