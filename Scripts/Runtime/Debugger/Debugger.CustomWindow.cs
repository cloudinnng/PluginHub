using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Runtime
{
    public partial class Debugger
    {
        /// <summary>
        /// CustomWindow 是给应用程序实现的。通过实现ICustomWindowGUI接口,可将自定义GUI绘制到Debugger中的CustomWindow。
        /// </summary>
        public class CustomWindow : ScrollableDebuggerWindowBase
        {
            private readonly Dictionary<string, IDebuggerCustomWindowGUI> _guiClientsDic =
                new Dictionary<string, IDebuggerCustomWindowGUI>();


            public interface IDebuggerCustomWindowGUI
            {
                ///客户端调试UI在Debugger CustomWindow中的绘制顺序，越小越靠前。可选择性实现
                ///具体绘制函数，由客户端程序实现

#if UNITY_2021_1_OR_NEWER
                public int DebuggerDrawOrder => 0;
                public bool IsVisible => true;// GUI是否可见

                public void OnDrawDebuggerGUI();
#elif UNITY_2020_1_OR_NEWER
                public int DebuggerDrawOrder { get; set; }
                public void OnDrawDebuggerGUI();
#else
                int DebuggerDrawOrder { get; set; }
                void OnDrawDebuggerGUI();
#endif
            }

            protected override void OnDrawScrollableWindow()
            {
                if (_guiClientsDic.Count == 0)
                    return;

                #if UNITY_EDITOR
                InnerDraw();
                #else
                //这里进行异常捕获，避免CustomWindowGUI的异常导致整个Debugger无法绘制
                try
                {
                    InnerDraw();
                }
                catch (Exception e)
                {
                    GUILayout.EndVertical();
                    Debug.LogError($"{e.Message}\n{e.StackTrace}\n{e.Source}\n{e.TargetSite}\n{e.InnerException}\n{e.Data}");
                    GUILayout.Label($"Error: {e.Message}");
                    GUILayout.Label("Debugger CustomWindow 出现异常！ 您可以尝试以下操作");
                    if (GUILayout.Button("重启场景"))
                    {
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
                    if (GUILayout.Button("清除 PlayerPrefs"))
                    {
                        PlayerPrefs.DeleteAll();
                    }
                }
                #endif
            }

            private void InnerDraw()
            {
                foreach (KeyValuePair<string, IDebuggerCustomWindowGUI> item in _guiClientsDic)
                {
                    if (!item.Value.IsVisible)
                        continue;
                    GUILayout.BeginVertical("box");
                    {
                        //绘制客户端名字和优先级
                        GUILayout.Button($"{item.Key} ({item.Value.DebuggerDrawOrder})", "Box");
                        item.Value.OnDrawDebuggerGUI();
                    }
                    GUILayout.EndVertical();
                }
            }

            public void RefreshDebuggerClientRoutine()
            {
                if (_refreshContiune)
                    return;
                Debugger.Instance.StartCoroutine(RefreshDebuggerClient());
            }


            private bool _refreshContiune = false;
            //调试器默认只在加载场景之后重新尝试获取注入到自身的调试UI。
            //若调试UI对象在运行期间动态激活、加载，则需要调用该方法以重新寻找
            private IEnumerator RefreshDebuggerClient()
            {
                _refreshContiune = true;
                yield return new WaitForSeconds(0.2f);
                //重新寻找客户端
                _guiClientsDic.Clear();
                MonoBehaviour[] monos = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                List<IDebuggerCustomWindowGUI> clients = new List<IDebuggerCustomWindowGUI>();
                for (int i = 0; i < monos.Length; i++)
                {
                    MonoBehaviour mono = monos[i];
                    IDebuggerCustomWindowGUI client = mono as IDebuggerCustomWindowGUI;
                    if (client != null)
                        clients.Add(client);
                }

                clients = clients.OrderBy(x => x.DebuggerDrawOrder).ToList();
                for (int i = 0; i < clients.Count; i++)
                {
                    string key = clients[i].GetType().ToString();
                    IDebuggerCustomWindowGUI value = clients[i];
                    if (_guiClientsDic.ContainsKey(key))//如果有重复的key，加上序号
                        key += i;
                    _guiClientsDic.Add(key, value);
                }
                _refreshContiune = false;
            }
        }
    }
}