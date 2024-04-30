using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PluginHub.Runtime
{
    public static class HandlesEx
    {
        public static void DrawSphere(Vector3 position,float radius)
        {
            Handles.DrawWireDisc(position,Vector3.up,radius);
            Handles.DrawWireDisc(position,Vector3.forward,radius);
            Handles.DrawWireDisc(position,Vector3.right,radius);
        }

        public static void DrawRect(Vector3 position,float width,float height,Vector3 rotation)
        {
            Matrix4x4 m = Matrix4x4.TRS(position,Quaternion.Euler(rotation),Vector3.one);
            Vector3 p0 = m.MultiplyPoint(new Vector3(-width/2,-height/2,0));
            Vector3 p1 = m.MultiplyPoint(new Vector3(width/2,-height/2,0));
            Vector3 p2 = m.MultiplyPoint(new Vector3(width/2,height/2,0));
            Vector3 p3 = m.MultiplyPoint(new Vector3(-width/2,height/2,0));
            Handles.DrawLine(p0,p1);
            Handles.DrawLine(p1,p2);
            Handles.DrawLine(p2,p3);
            Handles.DrawLine(p3,p0);
        }
    }
}