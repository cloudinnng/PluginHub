using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 网格顶点法线 Gizmo 可视化器。
    /// 用法：把本组件挂载到任意含有 MeshFilter 的 GameObject 上，
    ///       即可在 SceneView 中以 Gizmo 形式看到该网格所有顶点的法线。
    /// 注意：本脚本位于 Editor 程序集（Hellottw.PluginHub.Editor），
    ///       仅在编辑器中可用；如果直接挂在场景对象上参与 Build，
    ///       打包后该挂载引用会变成 missing script，请知悉。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("PluginHub/Tools/Mesh Normal Gizmo")]
    public class MeshNormalGizmo : MonoBehaviour
    {
        [Tooltip("顶点法线 Gizmo 的颜色")]
        public Color normalColor = new Color(0.2f, 0.7f, 1f, 1f);

        [Tooltip("法线长度相对于 mesh.bounds 对角线（已换算到世界空间）的比例。0.05 表示 5%")]
        [Range(0.001f, 1f)]
        public float lengthRatio = 0.05f;

        [Tooltip("是否仅在选中该对象时才绘制（顶点数大时建议开启，节省 SceneView 性能）")]
        public bool drawOnlyWhenSelected = false;

        [Tooltip("最多绘制的顶点数。超过该值会按等步长抽样；设为 0 表示不限制")]
        [Min(0)]
        public int maxVertexCount = 0;

        [Tooltip("启用/挂载时输出统计日志，避免 OnDrawGizmos 每帧打日志刷屏")]
        public bool logOnEnable = true;

        // 缓存 MeshFilter 引用，OnEnable / OnValidate 时刷新一次，OnDrawGizmos 中按需补取
        private MeshFilter _cachedMeshFilter;

        private void OnEnable()
        {
            _cachedMeshFilter = GetComponent<MeshFilter>();

            if (!logOnEnable) return;

            if (_cachedMeshFilter == null)
            {
                Debug.LogWarning($"[MeshNormalGizmo] '{name}' 上未找到 MeshFilter，无法绘制法线。", this);
                return;
            }

            Mesh mesh = _cachedMeshFilter.sharedMesh;
            if (mesh == null)
            {
                Debug.LogWarning($"[MeshNormalGizmo] '{name}' 的 MeshFilter.sharedMesh 为空。", this);
                return;
            }

            int normalCount = mesh.normals != null ? mesh.normals.Length : 0;
            Debug.Log(
                $"[MeshNormalGizmo] 已启用：obj='{name}', mesh='{mesh.name}', vertices={mesh.vertexCount}, normals={normalCount}",
                this);
        }

        // Inspector 中改字段会触发，这里只刷新缓存，不打日志（避免反复触发）
        private void OnValidate()
        {
            _cachedMeshFilter = GetComponent<MeshFilter>();
        }

        private void OnDrawGizmos()
        {
            if (drawOnlyWhenSelected) return;
            DrawNormals();
        }

        private void OnDrawGizmosSelected()
        {
            // 选中模式下走 OnDrawGizmosSelected，避免与 OnDrawGizmos 重复绘制
            if (!drawOnlyWhenSelected) return;
            DrawNormals();
        }

        /// <summary>
        /// 实际的法线绘制逻辑。
        /// 关键步骤：
        /// 1) 取 sharedMesh 的顶点与法线（模型空间）
        /// 2) 以 mesh.bounds.size 经 TransformVector 换算出"世界空间对角线"作为长度基准
        /// 3) 顶点用 TransformPoint，法线用 TransformDirection（处理旋转/非均匀缩放后再 normalize）
        /// </summary>
        private void DrawNormals()
        {
            if (_cachedMeshFilter == null) _cachedMeshFilter = GetComponent<MeshFilter>();
            if (_cachedMeshFilter == null) return;

            Mesh mesh = _cachedMeshFilter.sharedMesh;
            if (mesh == null) return;

            // 注意：mesh.vertices / mesh.normals 每次访问都会拷贝一份数组，频繁调用有 GC 压力，
            // 但 Gizmo 场景下可接受；如需优化可换 mesh.GetVertices/GetNormals + List 缓存。
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            if (vertices == null || vertices.Length == 0) return;
            // 没有法线数据（自定义 Mesh 未写入），无法绘制
            if (normals == null || normals.Length != vertices.Length) return;

            // mesh.bounds 是模型空间，转成世界空间向量后取模长当作"自适应基准长度"，
            // 这样无论缩放/朝向如何，法线长度看上去都比较合适
            Vector3 worldDiag = transform.TransformVector(mesh.bounds.size);
            float length = worldDiag.magnitude * lengthRatio;
            if (length <= 0f) return;

            // 顶点过多时启用等步长抽样，防止 SceneView 卡顿
            int step = 1;
            if (maxVertexCount > 0 && vertices.Length > maxVertexCount)
            {
                step = Mathf.CeilToInt(vertices.Length / (float)maxVertexCount);
            }

            Color prevColor = Gizmos.color;
            Gizmos.color = normalColor;

            for (int i = 0; i < vertices.Length; i += step)
            {
                Vector3 vWorld = transform.TransformPoint(vertices[i]);
                // TransformDirection 不含位移、含旋转与（非均匀）缩放；做完再 normalize 保证长度由 length 控制
                Vector3 nWorld = transform.TransformDirection(normals[i]).normalized;
                Gizmos.DrawLine(vWorld, vWorld + nWorld * length);
            }

            Gizmos.color = prevColor;
        }
    }
}
