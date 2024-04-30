using UnityEngine;

namespace PluginHub.Runtime
{
//画出bounds
    public class DrawBoundsForTest : MonoBehaviour
    {

        [Header("Mesh Bounds 用蓝色绘出")]
        [Header("Collider Bounds 用绿色绘出")]
        [Header("Renderer Bounds 用红色绘出")]
        [Range(.01f, 1f)]
        public float radius = .2f;

        private MeshFilter _meshFilter;
        private Collider _collider;
        private MeshRenderer _meshRenderer;

        private void GetReference()
        {
            if (_meshFilter == null) _meshFilter = GetComponentInChildren<MeshFilter>();
            if (_collider == null) _collider = GetComponentInChildren<Collider>();
            if (_meshRenderer == null) _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        private void OnDrawGizmosSelected()
        {
            GetReference();
            if (_meshFilter)
            {
                Gizmos.color = Color.blue;
                DrawBounds(_meshFilter.sharedMesh.bounds);
            }

            if (_collider)
            {
                Gizmos.color = Color.green;
                DrawBounds(_collider.bounds);
            }

            if (_meshRenderer)
            {
                Gizmos.color = Color.red;
                DrawBounds(_meshRenderer.bounds);
            }
        }

        void DrawBounds(Bounds bounds)
        {
            Gizmos.DrawSphere(bounds.center, radius);

            Gizmos.DrawSphere(bounds.max, radius);
            Gizmos.DrawSphere(bounds.min, radius);
            Gizmos.DrawSphere(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), radius);
            Gizmos.DrawSphere(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), radius);
            Gizmos.DrawSphere(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), radius);
            Gizmos.DrawSphere(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z), radius);
            Gizmos.DrawSphere(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z), radius);
            Gizmos.DrawSphere(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), radius);

            Gizmos.DrawLine(bounds.max, new Vector3(bounds.max.x, bounds.min.y, bounds.max.z));
            Gizmos.DrawLine(bounds.max, new Vector3(bounds.max.x, bounds.max.y, bounds.min.z));
            Gizmos.DrawLine(bounds.max, new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));

            Gizmos.DrawLine(bounds.min, new Vector3(bounds.max.x, bounds.min.y, bounds.min.z));
            Gizmos.DrawLine(bounds.min, new Vector3(bounds.min.x, bounds.max.y, bounds.min.z));
            Gizmos.DrawLine(bounds.min, new Vector3(bounds.min.x, bounds.min.y, bounds.max.z));

            Gizmos.DrawLine(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z));
            Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));
            Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));

            Gizmos.DrawLine(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z));
            Gizmos.DrawLine(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z));
            Gizmos.DrawLine(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z));
        }
    }
}