namespace PluginHub.Extends
{
    using UnityEngine;
    using System.Reflection;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Debug的draw扩展，可以在程序任意地方调用，以在一段时间内绘制几何图形。
    /// Debug的draw虽然可以在任意地方绘制，但其本身属于Gizmos，所以在game视图中看不见。若要在所有相机可见，使用DrawAllCamera系列方法。
    /// </summary>
    public static class DebugEx
    {
        /// <summary>
        /// 用于调试，在世界空间中画一个点的位置，VR中可见。因为实际存在，所有相机都可见
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="size"></param>
        /// <param name="duration"></param>
        public static void DrawAllCameraPoint(Vector3 worldPos, float size, float duration, Color color,
            string objName = null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (!string.IsNullOrWhiteSpace(objName)) //设置名字用于识别
                sphere.name = objName;
            Collider collider = sphere.GetComponent<Collider>(); //移除碰撞器（如果有）
            if (collider != null)
                GameObject.Destroy(collider);
            sphere.transform.localScale = size * Vector3.one;
            sphere.transform.position = worldPos;
            if (color != Color.white) sphere.GetComponent<MeshRenderer>().material.color = color;
            if (duration <= 0) duration = .1f;
            GameObject.Destroy(sphere, duration);
        }

        /// <summary>
        /// 用于调试，在世界空间中画一条线，VR中可见。因为实际存在，所有相机都可见
        /// </summary>
        public static void DrawAllCameraLine(Vector3 startPos, Vector3 endPos, Color color, float width = 0.01f,
            float duration = 0.01f)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            GameObject cubeLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider collider = cubeLine.GetComponent<Collider>();
            if (collider != null)
                GameObject.Destroy(collider);
            cubeLine.name = "DebugEx all camera line";
            cubeLine.transform.localScale = new Vector3(width, width, Vector3.Distance(startPos, endPos));
            cubeLine.transform.rotation = Quaternion.LookRotation(endPos - startPos);
            cubeLine.transform.position = (startPos + endPos) / 2f;
            if (color != Color.white) cubeLine.GetComponent<MeshRenderer>().material.color = color;
            if (duration <= 0) duration = .1f;
            GameObject.Destroy(cubeLine, duration);
        }

        #region DebugDrawFunctions

        /// <summary>
        /// 	- Debugs a point.
        /// </summary>
        /// <param name='position'>
        /// 	- The point to debug.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the point.
        /// </param>
        /// <param name='scale'>
        /// 	- The size of the point.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the point.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not this point should be faded when behind other objects.
        /// </param>
        public static void DebugPoint(Vector3 position, Color color, float scale = 1.0f, float duration = 0,
            bool depthTest = true)
        {
            color = (color == default(Color)) ? Color.white : color;

            Debug.DrawRay(position + (Vector3.up * (scale * 0.5f)), -Vector3.up * scale, color, duration, depthTest);
            Debug.DrawRay(position + (Vector3.right * (scale * 0.5f)), -Vector3.right * scale, color, duration,
                depthTest);
            Debug.DrawRay(position + (Vector3.forward * (scale * 0.5f)), -Vector3.forward * scale, color, duration,
                depthTest);
        }

        /// <summary>
        /// 	- Debugs a point.
        /// </summary>
        /// <param name='position'>
        /// 	- The point to debug.
        /// </param>
        /// <param name='scale'>
        /// 	- The size of the point.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the point.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not this point should be faded when behind other objects.
        /// </param>
        public static void DebugPoint(Vector3 position, float scale = 1.0f, float duration = 0, bool depthTest = true)
        {
            DebugPoint(position, Color.white, scale, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs an axis-aligned bounding box.
        /// </summary>
        /// <param name='bounds'>
        /// 	- The bounds to debug.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the bounds.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the bounds.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the bounds should be faded when behind other objects.
        /// </param>
        public static void DebugBounds(Bounds bounds, Color color, float duration = 0, bool depthTest = true)
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

            Debug.DrawLine(ruf, luf, color, duration, depthTest);
            Debug.DrawLine(ruf, rub, color, duration, depthTest);
            Debug.DrawLine(luf, lub, color, duration, depthTest);
            Debug.DrawLine(rub, lub, color, duration, depthTest);

            Debug.DrawLine(ruf, rdf, color, duration, depthTest);
            Debug.DrawLine(rub, rdb, color, duration, depthTest);
            Debug.DrawLine(luf, lfd, color, duration, depthTest);
            Debug.DrawLine(lub, lbd, color, duration, depthTest);

            Debug.DrawLine(rdf, lfd, color, duration, depthTest);
            Debug.DrawLine(rdf, rdb, color, duration, depthTest);
            Debug.DrawLine(lfd, lbd, color, duration, depthTest);
            Debug.DrawLine(lbd, rdb, color, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs an axis-aligned bounding box.
        /// </summary>
        /// <param name='bounds'>
        /// 	- The bounds to debug.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the bounds.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the bounds should be faded when behind other objects.
        /// </param>
        public static void DebugBounds(Bounds bounds, float duration = 0, bool depthTest = true)
        {
            DebugBounds(bounds, Color.white, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a local cube.
        /// </summary>
        /// <param name='transform'>
        /// 	- The transform that the cube will be local to.
        /// </param>
        /// <param name='size'>
        /// 	- The size of the cube.
        /// </param>
        /// <param name='color'>
        /// 	- Color of the cube.
        /// </param>
        /// <param name='center'>
        /// 	- The position (relative to transform) where the cube will be debugged.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cube.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cube should be faded when behind other objects.
        /// </param>
        public static void DebugLocalCube(Transform transform, Vector3 size, Color color,
            Vector3 center = default(Vector3), float duration = 0, bool depthTest = true)
        {
            Vector3 lbb = transform.TransformPoint(center + ((-size) * 0.5f));
            Vector3 rbb = transform.TransformPoint(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

            Vector3 lbf = transform.TransformPoint(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
            Vector3 rbf = transform.TransformPoint(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

            Vector3 lub = transform.TransformPoint(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
            Vector3 rub = transform.TransformPoint(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

            Vector3 luf = transform.TransformPoint(center + ((size) * 0.5f));
            Vector3 ruf = transform.TransformPoint(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

            Debug.DrawLine(lbb, rbb, color, duration, depthTest);
            Debug.DrawLine(rbb, lbf, color, duration, depthTest);
            Debug.DrawLine(lbf, rbf, color, duration, depthTest);
            Debug.DrawLine(rbf, lbb, color, duration, depthTest);

            Debug.DrawLine(lub, rub, color, duration, depthTest);
            Debug.DrawLine(rub, luf, color, duration, depthTest);
            Debug.DrawLine(luf, ruf, color, duration, depthTest);
            Debug.DrawLine(ruf, lub, color, duration, depthTest);

            Debug.DrawLine(lbb, lub, color, duration, depthTest);
            Debug.DrawLine(rbb, rub, color, duration, depthTest);
            Debug.DrawLine(lbf, luf, color, duration, depthTest);
            Debug.DrawLine(rbf, ruf, color, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a local cube.
        /// </summary>
        /// <param name='transform'>
        /// 	- The transform that the cube will be local to.
        /// </param>
        /// <param name='size'>
        /// 	- The size of the cube.
        /// </param>
        /// <param name='center'>
        /// 	- The position (relative to transform) where the cube will be debugged.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cube.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cube should be faded when behind other objects.
        /// </param>
        public static void DebugLocalCube(Transform transform, Vector3 size, Vector3 center = default(Vector3),
            float duration = 0, bool depthTest = true)
        {
            DebugLocalCube(transform, size, Color.white, center, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a local cube.
        /// </summary>
        /// <param name='space'>
        /// 	- The space the cube will be local to.
        /// </param>
        /// <param name='size'>
        ///		- The size of the cube.
        /// </param>
        /// <param name='color'>
        /// 	- Color of the cube.
        /// </param>
        /// <param name='center'>
        /// 	- The position (relative to transform) where the cube will be debugged.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cube.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cube should be faded when behind other objects.
        /// </param>
        public static void DebugLocalCube(Matrix4x4 space, Vector3 size, Color color, Vector3 center = default(Vector3),
            float duration = 0, bool depthTest = true)
        {
            color = (color == default(Color)) ? Color.white : color;

            Vector3 lbb = space.MultiplyPoint3x4(center + ((-size) * 0.5f));
            Vector3 rbb = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

            Vector3 lbf = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
            Vector3 rbf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

            Vector3 lub = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
            Vector3 rub = space.MultiplyPoint3x4(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

            Vector3 luf = space.MultiplyPoint3x4(center + ((size) * 0.5f));
            Vector3 ruf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

            Debug.DrawLine(lbb, rbb, color, duration, depthTest);
            Debug.DrawLine(rbb, lbf, color, duration, depthTest);
            Debug.DrawLine(lbf, rbf, color, duration, depthTest);
            Debug.DrawLine(rbf, lbb, color, duration, depthTest);

            Debug.DrawLine(lub, rub, color, duration, depthTest);
            Debug.DrawLine(rub, luf, color, duration, depthTest);
            Debug.DrawLine(luf, ruf, color, duration, depthTest);
            Debug.DrawLine(ruf, lub, color, duration, depthTest);

            Debug.DrawLine(lbb, lub, color, duration, depthTest);
            Debug.DrawLine(rbb, rub, color, duration, depthTest);
            Debug.DrawLine(lbf, luf, color, duration, depthTest);
            Debug.DrawLine(rbf, ruf, color, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a local cube.
        /// </summary>
        /// <param name='space'>
        /// 	- The space the cube will be local to.
        /// </param>
        /// <param name='size'>
        ///		- The size of the cube.
        /// </param>
        /// <param name='center'>
        /// 	- The position (relative to transform) where the cube will be debugged.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cube.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cube should be faded when behind other objects.
        /// </param>
        public static void DebugLocalCube(Matrix4x4 space, Vector3 size, Vector3 center = default(Vector3),
            float duration = 0, bool depthTest = true)
        {
            DebugLocalCube(space, size, Color.white, center, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a circle.
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
        /// <param name='duration'>
        /// 	- How long to draw the circle.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the circle should be faded when behind other objects.
        /// </param>
        public static void DebugCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f,
            float duration = 0, bool depthTest = true)
        {
            Vector3 _up = up.normalized * radius;
            Vector3 _forward = Vector3.Slerp(_up, -_up, 0.5f);
            Vector3 _right = Vector3.Cross(_up, _forward).normalized * radius;

            Matrix4x4 matrix = new Matrix4x4();

            matrix[0] = _right.x;
            matrix[1] = _right.y;
            matrix[2] = _right.z;

            matrix[4] = _up.x;
            matrix[5] = _up.y;
            matrix[6] = _up.z;

            matrix[8] = _forward.x;
            matrix[9] = _forward.y;
            matrix[10] = _forward.z;

            Vector3 _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
            Vector3 _nextPoint = Vector3.zero;

            color = (color == default(Color)) ? Color.white : color;

            for (var i = 0; i < 91; i++)
            {
                _nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
                _nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
                _nextPoint.y = 0;

                _nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);

                Debug.DrawLine(_lastPoint, _nextPoint, color, duration, depthTest);
                _lastPoint = _nextPoint;
            }
        }

        /// <summary>
        /// 	- Debugs a circle.
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
        /// <param name='duration'>
        /// 	- How long to draw the circle.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the circle should be faded when behind other objects.
        /// </param>
        public static void DebugCircle(Vector3 position, Color color, float radius = 1.0f, float duration = 0,
            bool depthTest = true)
        {
            DebugCircle(position, Vector3.up, color, radius, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a circle.
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
        /// <param name='duration'>
        /// 	- How long to draw the circle.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the circle should be faded when behind other objects.
        /// </param>
        public static void DebugCircle(Vector3 position, Vector3 up, float radius = 1.0f, float duration = 0,
            bool depthTest = true)
        {
            DebugCircle(position, up, Color.white, radius, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a circle.
        /// </summary>
        /// <param name='position'>
        /// 	- Where the center of the circle will be positioned.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the circle.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the circle.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the circle should be faded when behind other objects.
        /// </param>
        public static void DebugCircle(Vector3 position, float radius = 1.0f, float duration = 0, bool depthTest = true)
        {
            DebugCircle(position, Vector3.up, Color.white, radius, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a wire sphere.
        /// </summary>
        /// <param name='position'>
        /// 	- The position of the center of the sphere.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the sphere.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the sphere.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the sphere.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the sphere should be faded when behind other objects.
        /// </param>
        public static void DebugWireSphere(Vector3 position, Color color, float radius = 1.0f, float duration = 0,
            bool depthTest = true)
        {
            float angle = 10.0f;

            Vector3 x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0));
            Vector3 y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0));
            Vector3 z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z);

            Vector3 new_x;
            Vector3 new_y;
            Vector3 new_z;

            for (int i = 1; i < 37; i++)
            {
                new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad),
                    position.z + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad));
                new_y = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y,
                    position.z + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad));
                new_z = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad),
                    position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z);

                Debug.DrawLine(x, new_x, color, duration, depthTest);
                Debug.DrawLine(y, new_y, color, duration, depthTest);
                Debug.DrawLine(z, new_z, color, duration, depthTest);

                x = new_x;
                y = new_y;
                z = new_z;
            }
        }

        /// <summary>
        /// 	- Debugs a wire sphere.
        /// </summary>
        /// <param name='position'>
        /// 	- The position of the center of the sphere.
        /// </param>
        /// <param name='radius'>
        /// 	- The radius of the sphere.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the sphere.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the sphere should be faded when behind other objects.
        /// </param>
        public static void DebugWireSphere(Vector3 position, float radius = 1.0f, float duration = 0,
            bool depthTest = true)
        {
            DebugWireSphere(position, Color.white, radius, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a cylinder.
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
        /// <param name='duration'>
        /// 	- How long to draw the cylinder.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cylinder should be faded when behind other objects.
        /// </param>
        public static void DebugCylinder(Vector3 start, Vector3 end, Color color, float radius = 1, float duration = 0,
            bool depthTest = true)
        {
            Vector3 up = (end - start).normalized * radius;
            Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            //Radial circles
            DebugEx.DebugCircle(start, up, color, radius, duration, depthTest);
            DebugEx.DebugCircle(end, -up, color, radius, duration, depthTest);
            DebugEx.DebugCircle((start + end) * 0.5f, up, color, radius, duration, depthTest);

            //Side lines
            Debug.DrawLine(start + right, end + right, color, duration, depthTest);
            Debug.DrawLine(start - right, end - right, color, duration, depthTest);

            Debug.DrawLine(start + forward, end + forward, color, duration, depthTest);
            Debug.DrawLine(start - forward, end - forward, color, duration, depthTest);

            //Start endcap
            Debug.DrawLine(start - right, start + right, color, duration, depthTest);
            Debug.DrawLine(start - forward, start + forward, color, duration, depthTest);

            //End endcap
            Debug.DrawLine(end - right, end + right, color, duration, depthTest);
            Debug.DrawLine(end - forward, end + forward, color, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a cylinder.
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
        /// <param name='duration'>
        /// 	- How long to draw the cylinder.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cylinder should be faded when behind other objects.
        /// </param>
        public static void DebugCylinder(Vector3 start, Vector3 end, float radius = 1, float duration = 0,
            bool depthTest = true)
        {
            DebugCylinder(start, end, Color.white, radius, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='direction'>
        /// 	- The direction for the cone gets wider in.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the cone.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cone.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cone should be faded when behind other objects.
        /// </param>
        public static void DebugCone(Vector3 position, Vector3 direction, Color color, float angle = 45,
            float duration = 0, bool depthTest = true)
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

            Debug.DrawRay(position, slerpedVector.normalized * dist, color);
            Debug.DrawRay(position, Vector3.Slerp(_forward, -_up, angle / 90.0f).normalized * dist, color, duration,
                depthTest);
            Debug.DrawRay(position, Vector3.Slerp(_forward, _right, angle / 90.0f).normalized * dist, color, duration,
                depthTest);
            Debug.DrawRay(position, Vector3.Slerp(_forward, -_right, angle / 90.0f).normalized * dist, color, duration,
                depthTest);

            DebugEx.DebugCircle(position + _forward, direction, color,
                (_forward - (slerpedVector.normalized * dist)).magnitude, duration, depthTest);
            DebugEx.DebugCircle(position + (_forward * 0.5f), direction, color,
                ((_forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='direction'>
        /// 	- The direction for the cone gets wider in.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cone.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cone should be faded when behind other objects.
        /// </param>
        public static void DebugCone(Vector3 position, Vector3 direction, float angle = 45, float duration = 0,
            bool depthTest = true)
        {
            DebugCone(position, direction, Color.white, angle, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        /// <param name='color'>
        /// 	- The color of the cone.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cone.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cone should be faded when behind other objects.
        /// </param>
        public static void DebugCone(Vector3 position, Color color, float angle = 45, float duration = 0,
            bool depthTest = true)
        {
            DebugCone(position, Vector3.up, color, angle, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a cone.
        /// </summary>
        /// <param name='position'>
        /// 	- The position for the tip of the cone.
        /// </param>
        /// <param name='angle'>
        /// 	- The angle of the cone.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the cone.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the cone should be faded when behind other objects.
        /// </param>
        public static void DebugCone(Vector3 position, float angle = 45, float duration = 0, bool depthTest = true)
        {
            DebugCone(position, Vector3.up, Color.white, angle, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs an arrow.
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
        /// <param name='duration'>
        /// 	- How long to draw the arrow.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the arrow should be faded when behind other objects.
        /// </param>
        public static void DebugArrow(Vector3 position, Vector3 direction, Color color, float duration = 0,
            bool depthTest = true)
        {
            Debug.DrawRay(position, direction, color, duration, depthTest);
            DebugEx.DebugCone(position + direction, -direction * 0.333f, color, 15, duration, depthTest);
        }

        public static void DebugRay(Ray ray, float distance, Color color, float duration = 0, bool depthTest = true)
        {
            Debug.DrawRay(ray.origin, ray.direction * distance, color, duration, depthTest);
            DebugEx.DebugCone(ray.origin + ray.direction * distance, -ray.direction * 0.333f, color, 15, duration,
                depthTest);
        }

        /// <summary>
        /// 	- Debugs an arrow.
        /// </summary>
        /// <param name='position'>
        /// 	- The start position of the arrow.
        /// </param>
        /// <param name='direction'>
        /// 	- The direction the arrow will point in.
        /// </param>
        /// <param name='duration'>
        /// 	- How long to draw the arrow.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the arrow should be faded when behind other objects.
        /// </param>
        public static void DebugArrow(Vector3 position, Vector3 direction, float duration = 0, bool depthTest = true)
        {
            DebugArrow(position, direction, Color.white, duration, depthTest);
        }

        /// <summary>
        /// 	- Debugs a capsule.
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
        /// <param name='duration'>
        /// 	- How long to draw the capsule.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the capsule should be faded when behind other objects.
        /// </param>
        public static void DebugCapsule(Vector3 start, Vector3 end, Color color, float radius = 1, float duration = 0,
            bool depthTest = true)
        {
            Vector3 up = (end - start).normalized * radius;
            Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            float height = (start - end).magnitude;
            float sideLength = Mathf.Max(0, (height * 0.5f) - radius);
            Vector3 middle = (end + start) * 0.5f;

            start = middle + ((start - middle).normalized * sideLength);
            end = middle + ((end - middle).normalized * sideLength);

            //Radial circles
            DebugEx.DebugCircle(start, up, color, radius, duration, depthTest);
            DebugEx.DebugCircle(end, -up, color, radius, duration, depthTest);

            //Side lines
            Debug.DrawLine(start + right, end + right, color, duration, depthTest);
            Debug.DrawLine(start - right, end - right, color, duration, depthTest);

            Debug.DrawLine(start + forward, end + forward, color, duration, depthTest);
            Debug.DrawLine(start - forward, end - forward, color, duration, depthTest);

            for (int i = 1; i < 26; i++)
            {
                //Start endcap
                Debug.DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + start,
                    Vector3.Slerp(right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
                Debug.DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + start,
                    Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
                Debug.DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + start,
                    Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
                Debug.DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + start,
                    Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);

                //End endcap
                Debug.DrawLine(Vector3.Slerp(right, up, i / 25.0f) + end,
                    Vector3.Slerp(right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
                Debug.DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + end,
                    Vector3.Slerp(-right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
                Debug.DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + end,
                    Vector3.Slerp(forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
                Debug.DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + end,
                    Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
            }
        }

        /// <summary>
        /// 	- Debugs a capsule.
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
        /// <param name='duration'>
        /// 	- How long to draw the capsule.
        /// </param>
        /// <param name='depthTest'>
        /// 	- Whether or not the capsule should be faded when behind other objects.
        /// </param>
        public static void DebugCapsule(Vector3 start, Vector3 end, float radius = 1, float duration = 0,
            bool depthTest = true)
        {
            DebugCapsule(start, end, Color.white, radius, duration, depthTest);
        }

        // 绘制带点的箭头
        public static void DebugPointArrow(Vector3 position, Vector3 direction, Color color, float pointScale,
            float duration = 0, bool depthTest = true)
        {
            DebugArrow(position, direction, color, duration, depthTest);
            DebugPoint(position, color, pointScale,duration, depthTest);
        }

        #endregion

        //通过反射获取对象有哪些方法

        #region DebugFunctions

        /// <summary>
        /// 	- Gets the methods of an object. 获取对象的方法。
        /// </summary>
        /// <returns>
        /// 	- A list of methods accessible from this object.
        /// </returns>
        /// <param name='obj'>
        /// 	- The object to get the methods of.
        /// </param>
        /// <param name='includeInfo'>
        /// 	- Whether or not to include each method's method info in the list.
        /// </param>
        public static string MethodsOfObject(System.Object obj, bool includeInfo = false)
        {
            string methods = "";
            MethodInfo[] methodInfos = obj.GetType().GetMethods();
            for (int i = 0; i < methodInfos.Length; i++)
            {
                if (includeInfo)
                {
                    methods += methodInfos[i] + "\n";
                }

                else
                {
                    methods += methodInfos[i].Name + "\n";
                }
            }

            return (methods);
        }

        /// <summary>
        /// 	- Gets the methods of a type.
        /// </summary>
        /// <returns>
        /// 	- A list of methods accessible from this type.
        /// </returns>
        /// <param name='type'>
        /// 	- The type to get the methods of.
        /// </param>
        /// <param name='includeInfo'>
        /// 	- Whether or not to include each method's method info in the list.
        /// </param>
        public static string MethodsOfType(System.Type type, bool includeInfo = false)
        {
            string methods = "";
            MethodInfo[] methodInfos = type.GetMethods();
            for (var i = 0; i < methodInfos.Length; i++)
            {
                if (includeInfo)
                {
                    methods += methodInfos[i] + "\n";
                }

                else
                {
                    methods += methodInfos[i].Name + "\n";
                }
            }

            return (methods);
        }

        #endregion
    }
}