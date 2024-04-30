using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PluginHub.Editor
{
    //无碰撞器的射线检测
    public static class RaycastWithoutCollider
    {
        // 保存成功击中的信息
        public class RaycastResult
        {
            public Vector3 hitPoint;
            public Vector3 hitNormal;
            public MeshRenderer meshRenderer;
            public int triangleIndex;//击中的是第几个三角形，从0开始。
            public float distance;//射线起点到击中点的距离
        }

        // 通用射线检测，先检测MeshRenderer，再检测Terrain
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastResult result)
        {
            bool raycastResult = RaycastMeshRenderer(origin,direction,out result);
            if (raycastResult)
                return true;
            raycastResult = RaycastTerrain(origin,direction,out result);
            return raycastResult;
        }

        // 检查射线是否与场景中的Terrain相交,由于Terrain自带碰撞器，所以使用Unity自带的射线检测方法
        public static bool RaycastTerrain(Vector3 origin, Vector3 direction, out RaycastResult result)
        {
            result = null;
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit))
            {
                result = new RaycastResult()
                {
                    hitPoint = hit.point,
                    hitNormal = hit.normal,
                    meshRenderer = null,
                    triangleIndex = -1,
                    distance = hit.distance
                };
                return true;
            }
            return false;
        }

        // 检查射线是否与场景中的MeshRenderer相交
        // 并返回: 是否发生相交,交点坐标、交点法线、MeshRenderer、碰撞点到射线起点的距离
        public static bool RaycastMeshRenderer(Vector3 origin,Vector3 direction,out RaycastResult result)
        {
            PerformanceTest.Start();
            //默认值
            result = null;

            List<RaycastResult> hitResults = new List<RaycastResult>();
            Ray ray = new Ray(origin,direction);
            // DebugEx.DebugRay(ray,9999,Color.green,3f);

            // 获取场景中所有MeshRenderer
            MeshRenderer[] meshRenderers = Object.FindObjectsOfType<MeshRenderer>();
            // 遍历所有MeshRenderer
            // Debug.Log($"meshRenderers.Length: {meshRenderers.Length}");
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                Bounds bounds = meshRenderer.bounds;

                //排除连边界框都不相交的情况
                if (!bounds.IntersectRay(ray))
                    continue;

                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    continue;
                Mesh mesh = meshFilter.sharedMesh;
                // Debug.Log($"mesh.triangles.Length: {mesh.triangles.Length}");

                // 先缓存起来对性能非常重要
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                for (int j = 0; j < triangles.Length; j+=3)
                {
                    Vector3 v0 = vertices[triangles[j]];
                    Vector3 v1 = vertices[triangles[j+1]];
                    Vector3 v2 = vertices[triangles[j+2]];
                    // 将顶点从模型空间转换到世界空间
                    v0 = meshRenderer.transform.TransformPoint(v0);
                    v1 = meshRenderer.transform.TransformPoint(v1);
                    v2 = meshRenderer.transform.TransformPoint(v2);

                    if (RayIntersectsTriangle(ray.origin,ray.direction,v0,v1,v2,
                            out Vector3 hitPoint,out Vector3 normal,true))
                    {
                        // DebugEx.DebugPointArrow(hitPoint,normal,Color.red,0.2f,3f);
                        // 收集这个碰撞结果
                        hitResults.Add(new RaycastResult()
                        {
                            hitPoint = hitPoint,
                            hitNormal = normal,
                            meshRenderer = meshRenderer,
                            triangleIndex = j / 3,//第0个三角形，第1个三角形 。。。
                            distance = Vector3.Distance(origin,hitPoint)
                        });
                        // break;//下一个MeshRenderer
                    }
                }
            }
            PerformanceTest.End("RaycastMeshRenderer");
            if(hitResults.Count == 0)
                return false;
            //寻找距离origin最近的碰撞结果
            result = hitResults.OrderBy(r => r.distance).First();

            //绘制一个箭头指示击中点
            // DebugEx.DebugPointArrow(result.hitPoint,result.hitNormal,Color.red,0.2f,3f);
            return true;
        }

        // 核心算法
        // 检查[射线]与[三角形]是否相交
        public static bool RayIntersectsTriangle(Vector3 origin, Vector3 direction, Vector3 v0, Vector3 v1, Vector3 v2,
            out Vector3 hitPoint, out Vector3 normal, bool ignoreBackFace = true)
        {
            hitPoint = new Vector3(0, 0, 0);
            normal = new Vector3(0, 0, 0);
            const float EPSILON = 1e-8f;

            Vector3 edge1, edge2, h, s, q;
            float a, f, u, v;
            edge1 = v1 - v0;
            edge2 = v2 - v0;

            h = Vector3.Cross(direction, edge2);
            a = Vector3.Dot(edge1, h);

            if (ignoreBackFace)
            {
                if (a > -EPSILON && a < EPSILON)
                    return false;    // This means that the ray is parallel to the triangle.
            }
            else
            {
                if (a > -EPSILON && a < EPSILON)
                    return false;    // This means that the ray is parallel to the triangle.
            }

            f = 1.0f / a;
            s = origin - v0;
            u = f * Vector3.Dot(s, h);

            if (u < 0.0 || u > 1.0)
                return false;

            q = Vector3.Cross(s, edge1);
            v = f * Vector3.Dot(direction, q);

            if (v < 0.0 || u + v > 1.0)
                return false;

            // At this stage we can compute t to find out where the intersection point is on the line.
            float t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON) // ray intersection
            {
                hitPoint = origin + direction * t;
                normal = Vector3.Cross(edge1, edge2);
                if (Vector3.Dot(normal, direction) > 0 && ignoreBackFace)
                {
                    // The normal and the ray direction are in the same direction,
                    // meaning the intersection is happening at the back of the triangle,
                    // so we ignore it if ignoreBackFace is true.
                    return false;
                }
                normal = Vector3.Normalize(normal);
                return true;
            }
            else // This means that there is a line intersection but not a ray intersection.
                return false;
        }



    }
}