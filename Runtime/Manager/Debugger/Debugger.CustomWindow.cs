using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cloudinnng.CFramework
{
    public partial class Debugger
    {
        /// <summary>
        /// CustomWindow 是给应用程序实现的。通过实现ICustomWindowGUI接口,可将自定义GUI绘制到Debugger中的CustomWindow。
        /// </summary>
        public class CustomWindow : ScrollableDebuggerWindowBase
        {
            private readonly Dictionary<string, ICustomWindowGUI> _guiClientsDic =
                new Dictionary<string, ICustomWindowGUI>();

            public interface ICustomWindowGUI
            {
                ///客户端调试UI在Debugger CustomWindow中的绘制顺序，越小越靠前。可选择性实现
                ///具体绘制函数，由客户端程序实现
      
#if UNITY_2021_1_OR_NEWER
                public int DebuggerDrawOrder => 0;
                public void OnDrawDebuggerGUI();
#elif UNITY_2020_1_OR_NEWER
                public int DebuggerDrawOrder { get; set; }
                public void OnDrawDebuggerGUI();
#else
                int DebuggerDrawOrder { get; set; }
                void OnDrawDebuggerGUI();
#endif
            }

            // private bool _showDetail = false;

            protected override void OnDrawScrollableWindow()
            {
                if (_guiClientsDic.Count == 0)
                    return;

                // if (GUILayout.Button("ButtonName"))
                // {
                //     RefreshDebuggerClientRoutine();
                // }

                foreach (KeyValuePair<string, ICustomWindowGUI> item in _guiClientsDic)
                {
                    GUILayout.BeginVertical("box");
                    {
                        //绘制客户端名字和优先级
                        if (GUILayout.Button($"{item.Key} ({item.Value.DebuggerDrawOrder})","Box"))
                        {
                            // _showDetail = !_showDetail;
                        }
                        item.Value.OnDrawDebuggerGUI();
                    }
                    GUILayout.EndVertical();
                }
            }

            public void RefreshDebuggerClientRoutine()
            {
                Debugger.Instance.StartCoroutine(RefreshDebuggerClient());
            }

            //调试器默认只在加载场景之后重新尝试获取注入到自身的调试UI。
            //若调试UI对象在运行期间动态激活、加载，则需要调用该方法以重新寻找
            private IEnumerator RefreshDebuggerClient()
            {
                yield return new WaitForSeconds(0.2f);
                //重新寻找客户端
                _guiClientsDic.Clear();
                MonoBehaviour[] monos = FindObjectsOfType<MonoBehaviour>();
                List<ICustomWindowGUI> clients = new List<ICustomWindowGUI>();
                for (int i = 0; i < monos.Length; i++)
                {
                    MonoBehaviour mono = monos[i];
                    ICustomWindowGUI client = mono as ICustomWindowGUI;
                    if (client != null)
                    {
                        clients.Add(client);
                    }
                }

                clients = clients.OrderBy(x => x.DebuggerDrawOrder).ToList();
                for (int i = 0; i < clients.Count; i++)
                {
                    _guiClientsDic.Add($"{clients[i].GetType()}", clients[i]);
                }
            }
        }
    }
}