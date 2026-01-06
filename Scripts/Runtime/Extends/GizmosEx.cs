using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// Gizmos的扩展
    /// 本类中的方法应该在OnDrawGizmos()/OnDrawGizmosSelected()方法里调用
    /// </summary>
    public static class GizmosEx
    {
        private static readonly GUIStyle _style;

        static GizmosEx()
        {
            _style = new GUIStyle();
            Font font = Font.CreateDynamicFontFromOSFont(new string[]
            {
				// "Microsoft YaHei Bold",
				// "Microsoft YaHei",  
				// "Arial Bold",
				"Arial"
            }, 12);
            _style.font = font;
        }

        private static Color tempColor = Color.black;

        /// <summary>
        /// 在场景视图给定世界坐标绘制文字
        /// 该文字是基于Handles.BeginGUI()在场景视图中绘制的，关闭场景视图的Gizmos按钮可关闭其绘制。
        /// 但尽管开启Game视图的Gizmos按钮，该文字也不会在Game视图中绘制。
        /// 如果想要在Game视图中的世界坐标绘制文字，使用GUIEx.DrawString()方法
        /// </summary>
        public static void DrawString(string text, Vector3 worldPos, Color textColor, float bgAlpha = 0.9f)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.BeginGUI();
            {
                GUI.color = textColor;
                {
                    _style.normal.textColor = textColor;

                    SceneView view = UnityEditor.SceneView.currentDrawingSceneView;
                    if (view != null)
                    {
                        float angle = Vector3.Angle(view.camera.transform.forward, worldPos - view.camera.transform.position);

                        if (angle < 90) //避免相机朝向文字的反方向也显示文字
                        {
                            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);

                            // macOS下的坐标系和Windows下的坐标系不同，macOS下的y轴是反的, 后来发现在windows编辑器下也是反的
                            // if(Application.platform == RuntimePlatform.OSXEditor)
                            screenPos.y = view.position.height - screenPos.y;
                            //绘制
                            Vector2 size = _style.CalcSize(new GUIContent(text));
                            Rect bgRect = new Rect(screenPos.x - (size.x / 2) - 10, +view.position.height - screenPos.y - 5,
                                size.x + 20, size.y + 10);
                            GUI.color = tempColor;
                            tempColor.a = bgAlpha;
                            GUI.DrawTexture(bgRect, UnityEditor.EditorGUIUtility.whiteTexture);
                            GUI.color = Color.white;
                            Rect textRect = new Rect(screenPos.x - (size.x / 2), +view.position.height - screenPos.y,
                                size.x, size.y);
                            GUI.Label(textRect, text, _style);
                        }
                    }
                }
                GUI.color = Color.white;
            }
            UnityEditor.Handles.EndGUI();
#endif
        }

        //在场景视图中给定Rect中绘制文字
        public static void DrawString(Rect drawRect, string text, int fontSize = 12, Color? colour = null)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.BeginGUI();

            if (!colour.HasValue)
                colour = Color.white;
            GUIStyle style = new GUIStyle(_style);
            style.normal.textColor = colour.Value;
            style.fontSize = fontSize;
            style.wordWrap = true;
            //绘制
            Vector2 size = style.CalcSize(new GUIContent(text));
            Rect bgRect = drawRect;
            GUI.Box(bgRect, "", PHHelper.GetGUISkin().box);
            Rect textRect = drawRect;
            GUI.Label(textRect, text, style);
            UnityEditor.Handles.EndGUI();
#endif
        }


        #region GizmoDrawFunctions

        /// <summary>
        /// 	- Draws a point.
        /// </summary>
        /// <param name='position'>
        /// 	- The point to draw.
        /// </param>
        ///  <param name='color'>
        /// 	- The color of the drawn point.
        /// </param>
        /// <param name='scale'>
        /// 	- The size of the drawn point.
        /// </param>
        public static void DrawPoint(Vector3 position, Color color, float scale = 1.0f)
        {
            Color oldColor = Gizmos.color;

            Gizmos.color = color;
            Gizmos.DrawRay(position + (Vector3.up * (scale * 0.5f)), -Vector3.up * scale);
            Gizmos.DrawRay(position + (Vector3.right * (scale * 0.5f)), -Vector3.right * scale);
            Gizmos.DrawRay(position + (Vector3.forward * (scale * 0.5f)), -Vector3.forward * scale);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws a point.
        /// </summary>
        /// <param name='position'>
        /// 	- The point to draw.
        /// </param>
        /// <param name='scale'>
        /// 	- The size of the drawn point.
        /// </param>
        public static void DrawPoint(Vector3 position, float scale = 1.0f)
        {
            DrawPoint(position, Color.white, scale);
        }

        public static void DrawSphere(Vector3 position, float radius, Color color)
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawSphere(position, radius);
            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws an axis-aligned bounding box.
        /// </summary>
        /// <param name='bounds'>
        /// 	- The bounds to draw.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the bounds.
        /// </param>
        public static void DrawBounds(Bounds bounds, Color color)
        {
            Vector3 center = bounds.center;

            float x = bounds.extents.x;
            float y = bounds.extents.y;
            float z = bounds.extents.z;

            Vector3 ruf = center + new Vector3(x, y, z);
            Vector3 rub = center + new Vector3(x, y, -z);
            Vector3 luf = center + new Vector3(-x, y, z);
            Vector3 lub = center + new Vector3(-x, y, -z);

            Vector3 rdf = center + new Vector3(x, -y, z);
            Vector3 rdb = center + new Vector3(x, -y, -z);
            Vector3 lfd = center + new Vector3(-x, -y, z);
            Vector3 lbd = center + new Vector3(-x, -y, -z);

            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            Gizmos.DrawLine(ruf, luf);
            Gizmos.DrawLine(ruf, rub);
            Gizmos.DrawLine(luf, lub);
            Gizmos.DrawLine(rub, lub);

            Gizmos.DrawLine(ruf, rdf);
            Gizmos.DrawLine(rub, rdb);
            Gizmos.DrawLine(luf, lfd);
            Gizmos.DrawLine(lub, lbd);

            Gizmos.DrawLine(rdf, lfd);
            Gizmos.DrawLine(rdf, rdb);
            Gizmos.DrawLine(lfd, lbd);
            Gizmos.DrawLine(lbd, rdb);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws an axis-aligned bounding box.
        /// </summary>
        /// <param name='bounds'>
        /// 	- The bounds to draw.
        /// </param>
        public static void DrawBounds(Bounds bounds)
        {
            DrawBounds(bounds, Color.white);
        }

        /// <summary>
        /// 	- Draws a local cube.
        /// </summary>
        /// <param name='transform'>
        /// 	- The transform the cube will be local to.
        /// </param>
        /// <param name='size'>
        /// 	- The local size of the cube.
        /// </param>
        /// <param name='center'>
        ///		- The local position of the cube.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the cube.
        /// </param>
        public static void DrawLocalCube(Transform transform, Vector3 size, Color color,
            Vector3 center = default(Vector3))
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            Vector3 lbb = transform.TransformPoint(center + ((-size) * 0.5f));
            Vector3 rbb = transform.TransformPoint(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

            Vector3 lbf = transform.TransformPoint(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
            Vector3 rbf = transform.TransformPoint(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

            Vector3 lub = transform.TransformPoint(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
            Vector3 rub = transform.TransformPoint(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

            Vector3 luf = transform.TransformPoint(center + ((size) * 0.5f));
            Vector3 ruf = transform.TransformPoint(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

            Gizmos.DrawLine(lbb, rbb);
            Gizmos.DrawLine(rbb, lbf);
            Gizmos.DrawLine(lbf, rbf);
            Gizmos.DrawLine(rbf, lbb);

            Gizmos.DrawLine(lub, rub);
            Gizmos.DrawLine(rub, luf);
            Gizmos.DrawLine(luf, ruf);
            Gizmos.DrawLine(ruf, lub);

            Gizmos.DrawLine(lbb, lub);
            Gizmos.DrawLine(rbb, rub);
            Gizmos.DrawLine(lbf, luf);
            Gizmos.DrawLine(rbf, ruf);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws a local cube.
        /// </summary>
        /// <param name='transform'>
        /// 	- The transform the cube will be local to.
        /// </param>
        /// <param name='size'>
        /// 	- The local size of the cube.
        /// </param>
        /// <param name='center'>
        ///		- The local position of the cube.
        /// </param>
        public static void DrawLocalCube(Transform transform, Vector3 size, Vector3 center = default(Vector3))
        {
            DrawLocalCube(transform, size, Color.white, center);
        }

        /// <summary>
        /// 	- Draws a local cube.
        /// </summary>
        /// <param name='space'>
        /// 	- The space the cube will be local to.
        /// </param>
        /// <param name='size'>
        /// 	- The local size of the cube.
        /// </param>
        /// <param name='center'>
        /// 	- The local position of the cube.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the cube.
        /// </param>
        public static void DrawLocalCube(Matrix4x4 space, Vector3 size, Color color, Vector3 center = default(Vector3))
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            Vector3 lbb = space.MultiplyPoint3x4(center + ((-size) * 0.5f));
            Vector3 rbb = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

            Vector3 lbf = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
            Vector3 rbf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

            Vector3 lub = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
            Vector3 rub = space.MultiplyPoint3x4(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

            Vector3 luf = space.MultiplyPoint3x4(center + ((size) * 0.5f));
            Vector3 ruf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

            Gizmos.DrawLine(lbb, rbb);
            Gizmos.DrawLine(rbb, lbf);
            Gizmos.DrawLine(lbf, rbf);
            Gizmos.DrawLine(rbf, lbb);

            Gizmos.DrawLine(lub, rub);
            Gizmos.DrawLine(rub, luf);
            Gizmos.DrawLine(luf, ruf);
            Gizmos.DrawLine(ruf, lub);

            Gizmos.DrawLine(lbb, lub);
            Gizmos.DrawLine(rbb, rub);
            Gizmos.DrawLine(lbf, luf);
            Gizmos.DrawLine(rbf, ruf);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws a local cube.
        /// </summary>
        /// <param name='space'>
        /// 	- The space the cube will be local to.
        /// </param>
        /// <param name='size'>
        /// 	- The local size of the cube.
        /// </param>
        /// <param name='center'>
        /// 	- The local position of the cube.
        /// </param>
        public static void DrawLocalCube(Matrix4x4 space, Vector3 size, Vector3 center = default(Vector3))
        {
            DrawLocalCube(space, size, Color.white, center);
        }

        /// <summary>
        /// 	- Draws a circle.
        /// </summary>
        /// <param name='position'>
        /// 	- Where the center of the circle will be positioned.
        /// </param>
        /// <param name='up'>
        /// 	- The direction perpendicular to the surface of the circle.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the circle.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the circle.
        /// </param>
        public static void DrawCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f)
        {
            up = ((up == Vector3.zero) ? Vector3.up : up).normalized * radius;
            Vector3 _forward = Vector3.Slerp(up, -up, 0.5f);
            Vector3 _right = Vector3.Cross(up, _forward).normalized * radius;

            Matrix4x4 matrix = new Matrix4x4();

            matrix[0] = _right.x;
            matrix[1] = _right.y;
            matrix[2] = _right.z;

            matrix[4] = up.x;
            matrix[5] = up.y;
            matrix[6] = up.z;

            matrix[8] = _forward.x;
            matrix[9] = _forward.y;
            matrix[10] = _forward.z;

            Vector3 _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
            Vector3 _nextPoint = Vector3.zero;

            Color oldColor = Gizmos.color;
            Gizmos.color = (color == default(Color)) ? Color.white : color;

            for (var i = 0; i < 91; i++)
            {
                _nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
                _nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
                _nextPoint.y = 0;

                _nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);

                Gizmos.DrawLine(_lastPoint, _nextPoint);
                _lastPoint = _nextPoint;
            }

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws a circle.
        /// </summary>
        /// <param name='position'>
        /// 	- Where the center of the circle will be positioned.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the circle.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the circle.
        /// </param>
        public static void DrawCircle(Vector3 position, Color color, float radius = 1.0f)
        {
            DrawCircle(position, Vector3.up, color, radius);
        }

        /// <summary>
        /// 	- Draws a circle.
        /// </summary>
        /// <param name='position'>
        /// 	- Where the center of the circle will be positioned.
        /// </param>
        /// <param name='up'>
        /// 	- The direction perpendicular to the surface of the circle.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the circle.
        /// </param>
        public static void DrawCircle(Vector3 position, Vector3 up, float radius = 1.0f)
        {
            DrawCircle(position, up, Color.white, radius);
        }

        /// <summary>
        /// 	- Draws a circle.
        /// </summary>
        /// <param name='position'>
        /// 	- Where the center of the circle will be positioned.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the circle.
        /// </param>
        public static void DrawCircle(Vector3 position, float radius = 1.0f)
        {
            DrawCircle(position, Vector3.up, Color.white, radius);
        }

        //Wiresphere already exists

        /// <summary>
        /// 	- Draws a cylinder.
        /// </summary>
        /// <param name='start'>
        /// 	- The position of one end of the cylinder.
        /// </param>
        /// <param name='end'>
        /// 	- The position of the other end of the cylinder.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the cylinder.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the cylinder.
        /// </param>
        public static void DrawCylinder(Vector3 start, Vector3 end, Color color, float radius = 1.0f)
        {
            Vector3 up = (end - start).normalized * radius;
            Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            //Radial circles
            GizmosEx.DrawCircle(start, up, color, radius);
            GizmosEx.DrawCircle(end, -up, color, radius);
            GizmosEx.DrawCircle((start + end) * 0.5f, up, color, radius);

            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            //Side lines
            Gizmos.DrawLine(start + right, end + right);
            Gizmos.DrawLine(start - right, end - right);

            Gizmos.DrawLine(start + forward, end + forward);
            Gizmos.DrawLine(start - forward, end - forward);

            //Start endcap
            Gizmos.DrawLine(start - right, start + right);
            Gizmos.DrawLine(start - forward, start + forward);

            //End endcap
            Gizmos.DrawLine(end - right, end + right);
            Gizmos.DrawLine(end - forward, end + forward);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws a cylinder.
        /// </summary>
        /// <param name='start'>
        /// 	- The position of one end of the cylinder.
        /// </param>
        /// <param name='end'>
        /// 	- The position of the other end of the cylinder.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the cylinder.
        /// </param>
        public static void DrawCylinder(Vector3 start, Vector3 end, float radius = 1.0f)
        {
            DrawCylinder(start, end, Color.white, radius);
        }

        /// <summary>
        /// 	- Draws a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='direction'>
        /// 	- The direction for the cone to get wider in.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the cone.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        public static void DrawCone(Vector3 position, Vector3 direction, Color color, float angle = 45)
        {
            float length = direction.magnitude;

            Vector3 _forward = direction;
            Vector3 _up = Vector3.Slerp(_forward, -_forward, 0.5f);
            Vector3 _right = Vector3.Cross(_forward, _up).normalized * length;

            direction = direction.normalized;

            Vector3 slerpedVector = Vector3.Slerp(_forward, _up, angle / 90.0f);

            float dist;
            var farPlane = new Plane(-direction, position + _forward);
            var distRay = new Ray(position, slerpedVector);

            farPlane.Raycast(distRay, out dist);

            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            Gizmos.DrawRay(position, slerpedVector.normalized * dist);
            Gizmos.DrawRay(position, Vector3.Slerp(_forward, -_up, angle / 90.0f).normalized * dist);
            Gizmos.DrawRay(position, Vector3.Slerp(_forward, _right, angle / 90.0f).normalized * dist);
            Gizmos.DrawRay(position, Vector3.Slerp(_forward, -_right, angle / 90.0f).normalized * dist);

            GizmosEx.DrawCircle(position + _forward, direction, color,
                (_forward - (slerpedVector.normalized * dist)).magnitude);
            GizmosEx.DrawCircle(position + (_forward * 0.5f), direction, color,
                ((_forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='direction'>
        /// 	- The direction for the cone to get wider in.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        public static void DrawCone(Vector3 position, Vector3 direction, float angle = 45)
        {
            DrawCone(position, direction, Color.white, angle);
        }

        /// <summary>
        /// 	- Draws a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the cone.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        public static void DrawCone(Vector3 position, Color color, float angle = 45)
        {
            DrawCone(position, Vector3.up, color, angle);
        }

        /// <summary>
        /// 	- Draws a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        public static void DrawCone(Vector3 position, float angle = 45)
        {
            DrawCone(position, Vector3.up, Color.white, angle);
        }

        /// <summary>
        /// 	- Draws an arrow.
        /// </summary>
        /// <param name='position'>
        /// 	- The start position of the arrow.
        /// </param>
        /// <param name='direction'>
        /// 	- The direction the arrow will point in.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the arrow.
        /// </param>
        public static void DrawArrow(Vector3 position, Vector3 direction, Color color)
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            Gizmos.DrawRay(position, direction);
            GizmosEx.DrawCone(position + direction, -direction * 0.333f, color, 15);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws an arrow.
        /// </summary>
        /// <param name='position'>
        /// 	- The start position of the arrow.
        /// </param>
        /// <param name='direction'>
        /// 	- The direction the arrow will point in.
        /// </param>
        public static void DrawArrow(Vector3 position, Vector3 direction)
        {
            DrawArrow(position, direction, Color.white);
        }

        /// <summary>
        /// 	- Draws a capsule.
        /// </summary>
        /// <param name='start'>
        /// 	- The position of one end of the capsule.
        /// </param>
        /// <param name='end'>
        /// 	- The position of the other end of the capsule.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the capsule.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the capsule.
        /// </param>
        public static void DrawCapsule(Vector3 start, Vector3 end, Color color, float radius = 1)
        {
            Vector3 up = (end - start).normalized * radius;
            Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            Color oldColor = Gizmos.color;
            Gizmos.color = color;

            float height = (start - end).magnitude;
            float sideLength = Mathf.Max(0, (height * 0.5f) - radius);
            Vector3 middle = (end + start) * 0.5f;

            start = middle + ((start - middle).normalized * sideLength);
            end = middle + ((end - middle).normalized * sideLength);

            //Radial circles
            GizmosEx.DrawCircle(start, up, color, radius);
            GizmosEx.DrawCircle(end, -up, color, radius);

            //Side lines
            Gizmos.DrawLine(start + right, end + right);
            Gizmos.DrawLine(start - right, end - right);

            Gizmos.DrawLine(start + forward, end + forward);
            Gizmos.DrawLine(start - forward, end - forward);

            for (int i = 1; i < 26; i++)
            {

                //Start endcap
                Gizmos.DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + start,
                    Vector3.Slerp(right, -up, (i - 1) / 25.0f) + start);
                Gizmos.DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + start,
                    Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + start);
                Gizmos.DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + start,
                    Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + start);
                Gizmos.DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + start,
                    Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + start);

                //End endcap
                Gizmos.DrawLine(Vector3.Slerp(right, up, i / 25.0f) + end,
                    Vector3.Slerp(right, up, (i - 1) / 25.0f) + end);
                Gizmos.DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + end,
                    Vector3.Slerp(-right, up, (i - 1) / 25.0f) + end);
                Gizmos.DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + end,
                    Vector3.Slerp(forward, up, (i - 1) / 25.0f) + end);
                Gizmos.DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + end,
                    Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + end);
            }

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 	- Draws a capsule.
        /// </summary>
        /// <param name='start'>
        /// 	- The position of one end of the capsule.
        /// </param>
        /// <param name='end'>
        /// 	- The position of the other end of the capsule.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the capsule.
        /// </param>
        public static void DrawCapsule(Vector3 start, Vector3 end, float radius = 1)
        {
            DrawCapsule(start, end, Color.white, radius);
        }

        public static void DrawPlane(Vector3 planePosition, Vector3 planeNormal, Color color, float size = 1)
        {
            Quaternion rotation = Quaternion.LookRotation(planeNormal);
            Matrix4x4 trs = Matrix4x4.TRS(planePosition, rotation, Vector3.one);
            Gizmos.matrix = trs;
            Color drawColor = color;
            if (color.a == 0 || color.a == 1)
                drawColor.a = .5f;
            Gizmos.color = drawColor;
            Gizmos.DrawCube(Vector3.zero, new Vector3(size, size, 0.0001f));
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }

        #endregion

    }
}
