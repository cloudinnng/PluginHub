using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Runtime
{
//所有的GUI绘制集中在这里进行,mono实现IGuiClient接口即可
//Debugger的前身
    public class GuiManager : SceneSingleton<GuiManager>
    {
        public interface IGUIClient
        {
            void OnDrawGUI();
            int GetGuiPriority(); //优先级
        }

        public bool ShowGui = true;
        [Range(0, 1280)] public float guiWidth = 300;
        public bool ShowClientName = true;
        public Font font;

        [Range(1, 5)] public float guiscale = 1;
        private readonly Dictionary<string, IGUIClient> _guiClientsDic = new Dictionary<string, IGUIClient>();

        //鼠标悬浮控制ui显示与隐藏
        public bool MouseHoverShow = false;
        private Rect showHoverRange = new Rect(0, 0, 200, 200);
        private bool showHoverRangeLastState;
        private Rect hideHoverRange = new Rect(Screen.width / 3f, 0, Screen.width / 3f * 2f, Screen.height);
        private bool hideHoverRangeLastState;

        private void Start()
        {
            Reflash();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Update()
        {
            if (MouseHoverShow)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                if (showHoverRange.Contains(mousePos))
                {
                    if (!showHoverRangeLastState)
                    {
                        ShowGui = true;
                    }

                    showHoverRangeLastState = true;
                }
                else
                {
                    showHoverRangeLastState = false;
                }

                if (hideHoverRange.Contains(mousePos))
                {
                    if (!hideHoverRangeLastState)
                    {
                        ShowGui = false;
                    }

                    hideHoverRangeLastState = true;
                }
                else
                {
                    hideHoverRangeLastState = false;
                }
            }

            // if (Input.GetKeyDown(KeyCode.BackQuote))
            // {
            // 	ShowGui = !ShowGui;
            // }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        //当场景加载完成会调用该函数
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(RefreshC());
        }

        IEnumerator RefreshC()
        {
            yield return new WaitForSeconds(0.2f);
            Reflash();
        }

        private void Reflash()
        {
            //重新调整大小
            //guiscale = GetPreferredGuiScale();

            //重新寻找客户端
            _guiClientsDic.Clear();
            MonoBehaviour[] monos = FindObjectsOfType<MonoBehaviour>();
            List<IGUIClient> clients = new List<IGUIClient>();
            for (int i = 0; i < monos.Length; i++)
            {
                MonoBehaviour mono = monos[i];
                IGUIClient client = mono as IGUIClient;
                if (client != null)
                {
                    clients.Add(client);
                }
            }

            clients.Sort((t, t1) => t.GetGuiPriority() > t1.GetGuiPriority() ? 1 : -1);
            for (int i = 0; i < clients.Count; i++)
            {
                _guiClientsDic.Add($"Obj:{(clients[i] as MonoBehaviour).name} Script:{clients[i].GetType()}",
                    clients[i]);
            }
        }

        private static bool _initGuiOnce = true;

        //统一在这里绘制gui
        private void OnGUI()
        {
            if (_initGuiOnce)
            {
                //修改全局样式
                // GUI.skin.box = TheGuiStyleAsset.BoxStyle;
                // GUI.skin.button = TheGuiStyleAsset.DefaultButtonStyle;
                GUI.skin.font = font;
                _initGuiOnce = false;
            }

            if (_guiClientsDic.Count == 0)
                return;

            if (!ShowGui)
            {
                return;
            }

            Matrix4x4 tmp = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiscale, guiscale, 1));

//		GUILayout.BeginVertical("Box");
//		{
            foreach (KeyValuePair<string, IGUIClient> item in _guiClientsDic)
            {
                if (Mathf.Approximately(guiWidth, 0))
                    GUILayout.BeginVertical("box");
                else
                    GUILayout.BeginVertical("box", GUILayout.Width(guiWidth));
                {
                    if (ShowClientName)
                        GUILayout.Label(item.Key);
                    item.Value.OnDrawGUI();
                }
                GUILayout.EndVertical();
            }
//		}
//		GUILayout.EndVertical();

            GUI.matrix = tmp;
        }

        public static float GetPreferredGuiScale()
        {
            return Mathf.Clamp(Screen.width / 1080f, 1f, 3f);
        }
    }
}