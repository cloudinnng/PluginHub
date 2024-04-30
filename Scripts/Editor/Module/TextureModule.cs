using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    //纹理处理
    public class TextureModule : PluginHubModuleBase
    {
        public override string moduleName { get { return "纹理模块"; } }
        public override ModuleType moduleType => ModuleType.Tool;

        private Texture2D inputTexture0;
        private Texture2D inputTexture1;
        private Texture2D inputTexture2;

        //useLeftTextureChannels[0] = true 表示R使用纹理1
        //useLeftTextureChannels[0] = false 表示R使用纹理2
        //useLeftTextureChannels[2] = true 表示B使用纹理1
        bool[] useLeftTextureChannels = new bool[] { true, true, true, false };

        //useChannel[0] = 0 表示R使用R通道
        //useChannel[0] = 1 表示R使用G通道
        //useChannel[2] = 2 表示B使用B通道
        int[] useChannel = new int[] { 0, 1, 2, 3 };


        string[] textureChannelNames = new string[] { "R", "G", "B", "A" };

        protected override void DrawGuiContent()
        {
            GUILayout.BeginVertical("Box");
            {
                DrawSplitLine("纹理翻转");

                EditorGUILayout.HelpBox("这里可以将纹理翻转后保存成新纹理资产。Unity接受Smoothness纹理，这在下载的纹理是Roughness的时候很有用。", MessageType.Info);

                inputTexture0 = EditorGUILayout.ObjectField("纹理", inputTexture0, typeof(Texture2D), false) as Texture2D;
                GUI.enabled = inputTexture0 != null;
                if (GUILayout.Button("翻转纹理颜色"))
                {
                    string path = AssetDatabase.GetAssetPath(inputTexture0);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    //先设置为可读写
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    Texture2D newTexture = new Texture2D(inputTexture0.width, inputTexture0.height);
                    for (int i = 0; i < inputTexture0.width; i++)
                    {
                        for (int j = 0; j < inputTexture0.height; j++)
                        {
                            Color color = inputTexture0.GetPixel(i, j);
                            color.r = 1 - color.r;
                            color.g = 1 - color.g;
                            color.b = 1 - color.b;
                            newTexture.SetPixel(i, j, color);
                        }
                    }
                    newTexture.Apply();
                    byte[] bytes = newTexture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path.Replace(".png", "_InvertColor.png"), bytes);
                    AssetDatabase.Refresh();
                }
                GUI.enabled = true;


                DrawSplitLine("纹理合并");

                EditorGUILayout.HelpBox("这里可以将两张纹理选择的通道合并为一张纹理。在HDRP中，通常要求金属度贴图的Alpha通道保存光滑度信息。", MessageType.Info);

                GUILayout.BeginHorizontal();
                {
                    float size = 65;
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("纹理1");
                        inputTexture1 = EditorGUILayout.ObjectField(inputTexture1, typeof(Texture2D), false,GUILayout.Width(size),GUILayout.Height(size)) as Texture2D;
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();


                    GUILayout.BeginVertical();
                    {
                        for (int i = 0; i < textureChannelNames.Length; i++)
                        {
                            string text = useLeftTextureChannels[i] ? $"←{textureChannelNames[i]}" : $"{textureChannelNames[i]}→";
                            if(GUILayout.Button(text, GUILayout.Width(35)))
                                useLeftTextureChannels[i] = !useLeftTextureChannels[i];
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    {
                        for (int i = 0; i < textureChannelNames.Length; i++)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                for (int j = 0; j < textureChannelNames.Length; j++)
                                {
                                    GUI.color = useChannel[i] == j ? Color.green : Color.white;
                                    if (GUILayout.Button(textureChannelNames[j], GUILayout.Width(30)))
                                    {
                                        useChannel[i] = j;
                                    }
                                    GUI.color = Color.white;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("纹理2");
                        inputTexture2 = EditorGUILayout.ObjectField(inputTexture2, typeof(Texture2D), false,GUILayout.Width(size),GUILayout.Height(size)) as Texture2D;
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();



                GUI.enabled = inputTexture1 != null && inputTexture2 != null;
                if (GUILayout.Button("合并纹理"))
                {
                    string path1 = AssetDatabase.GetAssetPath(inputTexture1);
                    string path2 = AssetDatabase.GetAssetPath(inputTexture2);
                    TextureImporter importer1 = AssetImporter.GetAtPath(path1) as TextureImporter;
                    TextureImporter importer2 = AssetImporter.GetAtPath(path2) as TextureImporter;
                    //先设置为可读写
                    importer1.isReadable = true;
                    importer2.isReadable = true;
                    importer1.SaveAndReimport();
                    importer2.SaveAndReimport();
                    Texture2D newTexture = new Texture2D(inputTexture1.width, inputTexture1.height);
                    for (int i = 0; i < inputTexture1.width; i++)
                    {
                        for (int j = 0; j < inputTexture1.height; j++)
                        {
                            Color color1 = inputTexture1.GetPixel(i, j);
                            Color color2 = inputTexture2.GetPixel(i, j);
                            Color newColor = new Color();
                            newColor.r = useLeftTextureChannels[0] ? color1[useChannel[0]] : color2[useChannel[0]];
                            newColor.g = useLeftTextureChannels[1] ? color1[useChannel[1]] : color2[useChannel[1]];
                            newColor.b = useLeftTextureChannels[2] ? color1[useChannel[2]] : color2[useChannel[2]];
                            newColor.a = useLeftTextureChannels[3] ? color1[useChannel[3]] : color2[useChannel[3]];
                            newTexture.SetPixel(i, j, newColor);
                        }
                    }
                    newTexture.Apply();
                    byte[] bytes = newTexture.EncodeToPNG();
                    string exName = System.IO.Path.GetExtension(path1);
                    string dir = System.IO.Path.GetDirectoryName(path1);
                    string savePath = System.IO.Path.Combine(dir, "_Combined.png");
                    System.IO.File.WriteAllBytes(savePath, bytes);
                    //pin asset
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(savePath));
                    AssetDatabase.Refresh();
                }
                GUI.enabled = true;

                DrawSplitLine("纹理镜像");

                EditorGUILayout.HelpBox("这里可以将纹理镜像后保存成新纹理资产。", MessageType.Info);
                inputTexture0 = EditorGUILayout.ObjectField("纹理", inputTexture0, typeof(Texture2D), false) as Texture2D;
                if (GUILayout.Button("镜像纹理"))
                {
                    string path = AssetDatabase.GetAssetPath(inputTexture0);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    //先设置为可读写
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    Texture2D newTexture = new Texture2D(inputTexture0.width, inputTexture0.height);
                    for (int i = 0; i < inputTexture0.width; i++)
                    {
                        for (int j = 0; j < inputTexture0.height; j++)
                        {
                            Color color = inputTexture0.GetPixel(i, j);
                            newTexture.SetPixel(inputTexture0.width - i - 1, j, color);
                        }
                    }
                    newTexture.Apply();
                    byte[] bytes = newTexture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path.Replace(".png", "_Mirror.png"), bytes);
                    AssetDatabase.Refresh();
                }
            }
            GUILayout.EndVertical();

        }
    }
}