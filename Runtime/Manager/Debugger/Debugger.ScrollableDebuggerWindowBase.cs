using UnityEngine;

namespace PluginHub.Runtime
{
    public partial class Debugger
    {
        /// <summary>
        /// 可滚动的调试器窗口抽象基类
        /// </summary>
        public abstract class ScrollableDebuggerWindowBase : IDebuggerWindow
        {
            private Vector2 m_ScrollPosition = Vector2.zero;

            public virtual void OnStart()
            {
            }

            public virtual void OnDrawToolbar()
            {
            }
            
            public void OnDraw()
            {
                OnDrawToolbar();
                m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
                {
                    OnDrawScrollableWindow();
                }
                GUILayout.EndScrollView();
            }

            protected abstract void OnDrawScrollableWindow();
        }
    }
}