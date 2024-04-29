using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//该特性用来加在属性上，让该属性在检视面板上只读
public class ReadOnlyOnInspectorAttribute : PropertyAttribute
{

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyOnInspectorAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,SerializedProperty property,GUIContent label)
    {
        bool tmp = GUI.enabled;
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = tmp;
    }
}
#endif