using UnityEngine;

namespace PluginHub.Runtime
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ToastManager))]
    public class ToastManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("发射一个测试Toast"))
            {
                ToastManager.Instance.Show("Test Toast with default time");
            }

            if (GUILayout.Button("发射测试Toast"))
            {
                ToastManager.Instance.Show("Test Toast with default time");
                ToastManager.Instance.Show("中文字测试");
                ToastManager.Instance.Show("，。、；‘、【】");
            }

            GUI.enabled = true;
        }
    }

#endif


    //模仿安卓手机的toast 可用于显示提示信息，一段时间后自动消失
    [DefaultExecutionOrder(300)]
    [RequireComponent(typeof(UIDocument))]
    public class ToastManager : SceneSingleton<ToastManager>
    {
        public float DefaultShowTime = 5;

        private UIDocument uiDocument;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            
        }

        private void Update()
        {
        }


        public void Show(string text, float duration = -1, bool alsoLogToConsole = false)
        {
            print(text);
        }



    }
}