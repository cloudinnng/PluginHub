#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PluginHub.Runtime
{
    [CustomEditor(typeof(PHComment))]
    public class PHCommentEditor : UnityEditor.Editor
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
    // 使用这个组件来为对象添加注释
    public class PHComment : MonoBehaviour
    {
        public string description;
    }
}
#endif