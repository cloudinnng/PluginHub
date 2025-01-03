using System.Text;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    //定义了一个类别书签的UI绘制
    public abstract class IBookmarkUIRow
    {
        #region style
        protected static GUIStyle _bookmarkButtonStyle;
        protected static GUIStyle BookmarkButtonStyle
        {
            get
            {
                if (_bookmarkButtonStyle == null)
                {
                    _bookmarkButtonStyle = new GUIStyle("button");
                    _bookmarkButtonStyle.normal.textColor = Color.white;
                    _bookmarkButtonStyle.hover.textColor = Color.cyan;
                    _bookmarkButtonStyle.margin = new RectOffset(4, 4, 4, 4);
                    _bookmarkButtonStyle.padding = new RectOffset(4, 4, 3, 3);
                    _bookmarkButtonStyle.fontStyle = FontStyle.Normal;
                    _bookmarkButtonStyle.alignment = TextAnchor.MiddleCenter;
                    _bookmarkButtonStyle.border = new RectOffset(0, 0, 0, 0);
                    _bookmarkButtonStyle.clipping = TextClipping.Overflow;
                }
                return _bookmarkButtonStyle;
            }
        }
        #endregion

        protected abstract void DrawHorizontalInnerGUI(SceneBookmarkGroup group);

        public void DrawGUI(SceneBookmarkGroup group)
        {
            GUILayout.BeginHorizontal();
            {
                DrawHorizontalInnerGUI(group);
            }
            GUILayout.EndHorizontal();
        }
    }
}