using System.Linq;
using PluginHub.Runtime;
using UnityEditor.Overlays;

namespace PluginHub.Editor
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.EditorTools;

    // 这个注册的是编辑器工具（与移动，旋转工具同级），在Tools Overlay中会出现一个Icon
    [EditorTool("PH SceneView Tool")]
    public class PHSceneViewTool : EditorTool
    {
        // 为了能在 Tools 菜单里看到一个图标，可以指定一个 Texture2D
        // [SerializeField] Texture2D m_ToolIcon;

        //场景视图游标（跟blender学习），用于辅助其他功能
        public static Vector3 sceneViewCursor { get; private set; }
        public static Vector3 lastSceneViewCursor { get; private set; }
        public static Vector2 mouseDownPosition { get; private set; }
        public static Vector2 mouseCurrPosition { get; private set; }


        /// <summary>
        /// 当此工具被选中并且处于激活状态时，每帧都会调用该方法
        /// </summary>
        /// <param name="window">当前 EditorWindow，一般就是 SceneView</param>
        public override void OnToolGUI(EditorWindow window)
        {
            Event e = Event.current;

            // 右键菜单功能
            mouseCurrPosition = e.mousePosition;
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                // 记录鼠标按下的位置
                mouseDownPosition = e.mousePosition;
            }else if (e.type == EventType.MouseUp && e.button == 1)
            {
                // 无修饰键
                if(e.control || e.alt || e.shift || e.command)
                    return;

                // 被认为是右键单击的条件
                if ((e.mousePosition - mouseDownPosition).magnitude < 3 )
                {
                    PHSceneViewMenu.ShowContextMenu(e, mouseDownPosition);
                    e.Use();
                }
            }
        }


    }
}