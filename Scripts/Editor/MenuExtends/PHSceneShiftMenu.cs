using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PluginHub.Editor
{

    // 按下shift键时，右键出现的菜单是PH菜单，不按shift键时，右键出现的菜单是Unity默认菜单
    public class PHSceneShiftMenu
    {
        public static bool NoNeedShift
        {
            get => EditorPrefs.GetBool("PHSceneShiftMenu_NoNeedShift", false);
            set => EditorPrefs.SetBool("PHSceneShiftMenu_NoNeedShift", value);
        }

        [InitializeOnLoadMethod]
        public static void Init()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static Vector3 sceneViewCursor { get; private set; }
        public static Vector3 lastSceneViewCursor { get; private set; }
        public static Vector2 mouseDownPosition { get; private set; }
        public static Vector2 mouseCurrPosition { get; private set; }
        private static void OnSceneGUI(SceneView sceneView)
        {
            // Debug.Log("PHSceneAltMenu Update");
            Event e = Event.current;

            // 右键菜单功能
            mouseCurrPosition = e.mousePosition;
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                // 记录鼠标按下的位置
                mouseDownPosition = e.mousePosition;
            }
            else if (e.type == EventType.MouseUp && e.button == 1)
            {
                if (!e.shift && !NoNeedShift)
                    return;

                // 被认为是右键单击的条件
                if ((e.mousePosition - mouseDownPosition).magnitude < 3)
                {
                    PHSceneViewMenu.ShowContextMenu(e, mouseDownPosition);
                    e.Use();
                }
            }
        }
        // [MenuItem("CONTEXT/GameObjectToolContext/Test", false, int.MinValue)]
        // static void Test()
        // {
        //
        // }
    }

}