using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    public class ConvertHeightToNormalModule : PluginHubModuleBase
    {
        private Texture2D _heightTex;


        public override string moduleDescription => "";

        protected override void DrawGuiContent()
        {
            _heightTex =
                EditorGUILayout.ObjectField("Height Texture", _heightTex, typeof(Texture2D), false) as Texture2D;



            if (GUILayout.Button("Convert"))
            {
                Texture2D oldTex = _heightTex;
                Vector2 newTexSize = new Vector2(oldTex.width, oldTex.height);
                Texture2D normalMap = new Texture2D((int)newTexSize.x, (int)newTexSize.y, TextureFormat.RGB24, false);

                for (int x = 0; x < oldTex.width; x++)
                {
                    for (int y = 0; y < oldTex.height; y++)
                    {
                        Color tmpColor = oldTex.GetPixel(x, y);

                        //算法来自：《计算机图形学-基于3D图形开发技术 刘鹏译》章节9.2.1
                        Color xPlus1 = oldTex.GetPixel(x + 1, y);
                        Color xMinus1 = oldTex.GetPixel(x - 1, y);
                        Color yPlus1 = oldTex.GetPixel(x, y + 1);
                        Color yMinus1 = oldTex.GetPixel(x, y - 1);

                        //偏导数
                        float pdsX = (ChannelSum(xPlus1) - ChannelSum(xMinus1)) / 2;
                        float pdsY = (ChannelSum(yPlus1) - ChannelSum(yMinus1)) / 2;

                        Vector3 vecX = new Vector3(1, 0, pdsX);
                        Vector3 vecY = new Vector3(0, 1, pdsY);
                        Vector3 cross = Vector3.Cross(vecX, vecY); //cross 求垂直向量
                        cross = cross.normalized;
                        // Debug.Log(cross);

                        //映射到颜色值
                        tmpColor = new Color(cross.x * 0.5f + 0.5f, cross.y * 0.5f + 0.5f, cross.z * 0.5f + 0.5f);

                        normalMap.SetPixel(x, y, tmpColor);
                    }
                }



                normalMap.Apply(false);

                string savePath = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                    AssetDatabase.GetAssetPath(_heightTex));
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(savePath);
                savePath = savePath.Replace(fileNameWithoutExtension, fileNameWithoutExtension + "_normal");
                Debug.Log($"The file will be saved to {savePath}");
                File.WriteAllBytes(savePath, normalMap.EncodeToPNG());
                AssetDatabase.Refresh();
                Object.DestroyImmediate(normalMap);
            }
        }

        public float ChannelSum(Color color)
        {
            return color.r + color.g + color.b;
        }

    }
}