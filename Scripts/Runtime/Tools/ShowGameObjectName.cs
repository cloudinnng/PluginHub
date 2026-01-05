using System.Collections;
using System.Collections.Generic;
using PluginHub.Runtime;
using UnityEngine;


namespace PluginHub.Runtime
{
    /// <summary>
    /// 编辑器中的助手组件，用于在场景视图中显示游戏对象的名称。
    /// 该组件为编辑器脚本，因此不应参与构建
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class ShowGameObjectName : MonoBehaviour
    {
        [Range(0f, 5f)] public float OffsetY = 1;

        private void OnDrawGizmos()
        {
            GizmosEx.DrawString($"{gameObject.name}", transform.position + OffsetY * Vector3.up,Color.white);
        }
    }
}
