using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    [CustomEditor(typeof(Transform))]
    public class CustomTransformEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor instance;
        private Transform targetTransform;
        private bool showGlobalInfo
        {
            get => EditorPrefs.GetBool("PH_Transform_ShowGlobalInfo", false);
            set => EditorPrefs.SetBool("PH_Transform_ShowGlobalInfo", value);
        }

        private void OnEnable()
        {
            // Try to get the internal 'TransformInspector' type
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var editorType = editorAssembly.GetType("UnityEditor.TransformInspector");
            if (editorType != null)
            {
                instance = CreateEditor(targets, editorType);
            }
            else
            {
                Debug.LogError("Could not find TransformInspector type.");
            }
            targetTransform = target as Transform;
            if (targetTransform == null)
            {
                Debug.LogError($"Target is not a Transform. {target.name}", target);
            }
        }

        private void OnDisable()
        {
            if (instance != null)
                DestroyImmediate(instance);
        }

        [MenuItem("CONTEXT/Transform/PH Transform 切换Transform全局信息")]
        public static void SwitchGlobalInfo()
        {
            Debug.Log("SwitchGlobalInfo");
            EditorPrefs.SetBool("PH_Transform_ShowGlobalInfo", !EditorPrefs.GetBool("PH_Transform_ShowGlobalInfo"));
        }

        public override void OnInspectorGUI()
        {
            // 绘制原有Inspector
            if (instance != null)
                instance.OnInspectorGUI();

            if (showGlobalInfo)
            {
                GUI.enabled = false;
                // 如果position和localPosition不相等，则显示黄色
                GUI.color = targetTransform.position == targetTransform.localPosition ? Color.white : Color.yellow;
                EditorGUILayout.Vector3Field("Global Position", targetTransform.position);
                GUI.color = targetTransform.rotation.eulerAngles == targetTransform.localRotation.eulerAngles ? Color.white : Color.yellow;
                EditorGUILayout.Vector3Field("Global Rotation", targetTransform.rotation.eulerAngles);
                GUI.color = targetTransform.lossyScale == targetTransform.localScale ? Color.white : Color.yellow;
                EditorGUILayout.Vector3Field("Global Scale", targetTransform.lossyScale);
                GUI.color = Color.white;
                GUI.enabled = true;
            }
        }
    }
}
