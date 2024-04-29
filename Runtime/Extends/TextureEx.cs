using UnityEngine;

namespace PluginHub.Runtime.Extends
{
    public static class TextureEx
    {
        //会创建一个新的Texture2D，请自行管理内存
        public static Texture2D ResizeTex(this Texture2D originalTexture, int width, int height)
        {
            // 创建一个RenderTexture对象，用于渲染原始纹理
            RenderTexture rt = new RenderTexture(width, height, 24);

            // 将原始纹理渲染到RenderTexture对象上
            Graphics.Blit(originalTexture, rt);

            // 创建一个新的Texture2D对象，用于保存调整大小后的纹理
            Texture2D resizedTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);

            // 将RenderTexture对象的像素数据复制到新的Texture2D对象上
            RenderTexture.active = rt;
            resizedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            resizedTexture.Apply();

            // 清理RenderTexture对象
            RenderTexture.active = null;
            rt.Release();

            // 返回调整大小后的新纹理
            return resizedTexture;
        }

        //会创建一个新的Texture2D，请自行管理内存
        public static Texture2D RotateTexAntiClockwise90(this Texture2D originalTexture)
        {
            Texture2D tex = new Texture2D(originalTexture.height, originalTexture.width);
            for (int i = 0; i < originalTexture.width; i++)
            {
                for (int j = 0; j < originalTexture.height; j++)
                {
                    tex.SetPixel(j, originalTexture.width - i - 1, originalTexture.GetPixel(i, j));
                }
            }

            tex.Apply();
            return tex;
        }

        //会创建一个新的Texture2D，请自行管理内存
        public static Texture2D RotateTexClockwise90(this Texture2D originalTexture)
        {
            Texture2D tex = new Texture2D(originalTexture.height, originalTexture.width);
            for (int i = 0; i < originalTexture.width; i++)
            {
                for (int j = 0; j < originalTexture.height; j++)
                {
                    tex.SetPixel(originalTexture.height - j - 1, i, originalTexture.GetPixel(i, j));
                }
            }

            tex.Apply();
            return tex;
        }

        //会创建一个新的Texture2D，请自行管理内存
        public static Texture2D RotateTex180(this Texture2D originalTexture)
        {
            Texture2D tex = new Texture2D(originalTexture.width, originalTexture.height);
            for (int i = 0; i < originalTexture.width; i++)
            {
                for (int j = 0; j < originalTexture.height; j++)
                {
                    tex.SetPixel(originalTexture.width - i - 1, originalTexture.height - j - 1,
                        originalTexture.GetPixel(i, j));
                }
            }

            tex.Apply();
            return tex;
        }

        //会创建一个新的Texture2D，请自行管理内存
        public static Texture2D Crop(this Texture2D originalTexture, int x, int y, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                tex.SetPixel(i, j, originalTexture.GetPixel(x + i, y + j));
            tex.Apply();
            return tex;
        }
    }
}