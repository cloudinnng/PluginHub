using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    public static class GLEx
    {
        public static void DrawFace(Vector3[] vertexs, Color color, Material material)
        {
            material.SetColor("_Color", color);
            material.SetPass(0);

            // Draw the line
            GL.Begin(GL.QUADS);
            GL.Color(color);
            for (int i = 0; i < vertexs.Length; i++)
            {
                GL.Vertex3(vertexs[i].x, vertexs[i].y, vertexs[i].z);
            }

            GL.End();
        }

        public static void DrawTriangleStrip(Vector3[] vertexs, Color color, Material material)
        {
            material.SetColor("_Color", color);
            material.SetPass(0);

            // Draw the line
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.Color(color);
            for (int i = 0; i < vertexs.Length; i++)
            {
                GL.Vertex3(vertexs[i].x, vertexs[i].y, vertexs[i].z);
            }

            GL.End();
        }

        public static void DrawTriangles(Vector3[] vertexs, Color color, Material material)
        {
            material.SetColor("_Color", color);
            material.SetPass(0);

            // Draw the line
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            for (int i = 0; i < vertexs.Length; i++)
            {
                GL.Vertex3(vertexs[i].x, vertexs[i].y, vertexs[i].z);
            }

            GL.End();
        }
    }
}
