using UnityEngine;

namespace PluginHub.Runtime
{
#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(PHComment))]
    public class PHCommentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // DrawDefaultInspector();
            // 使用TextArea显示description供编写
            PHComment phComment = (PHComment)target;
            phComment.description = EditorGUILayout.TextArea(phComment.description, GUILayout.MinHeight(60));
            if (GUI.changed)
            {
                EditorUtility.SetDirty(phComment);
            }
        }
    }
#endif
    // 使用这个组件来为对象添加注释
    public class PHComment : MonoBehaviour
    {
        public string description;
        void Awake()
        {
            if (!Application.isEditor)
            {
                Destroy(this);
            }
        }
    }
}