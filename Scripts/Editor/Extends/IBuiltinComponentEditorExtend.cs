using System.Linq;
using System.Reflection;

namespace PluginHub.Editor
{
    using UnityEditor;

    // 这个类其实可以删，先留着
    // 内置组件的编辑器类扩展，包含Inspector扩展和SceneGUI扩展
    // 继承这个类，可以方便的扩展Unity内置组件的Inspector面板
    // 该类由 《Unity编辑器开发与拓展》 P70页提到，保持原有Inspector的同时，为系统组件添加新的Inspector功能
    public abstract class IBuiltinComponentEditorExtend : Editor
    {
        // 覆盖这个属性，返回Unity内置组件的检视面板Editor类名
        protected abstract string editorTypeName { get; }
        private Editor instance;

        // private MethodInfo onSceneGUIMethod;
        // private static readonly object[] emptyArray = new object[0];


        protected virtual void OnEnable()
        {
            // 不要用下面这句，因为某些内置组件的Editor类是internal定义的，无法直接访问
            // var editorType = typeof(T);
            // 使用这句
            var editorType = Assembly.GetAssembly(typeof(Editor)).GetTypes()
                .FirstOrDefault(t => t.Name == editorTypeName);
            instance = CreateEditor(targets, editorType);
            // onSceneGUIMethod = editorType.GetMethod("OnSceneGUI", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        protected virtual void OnDisable()
        {
            if (instance != null)
                DestroyImmediate(instance);
        }

        public override void OnInspectorGUI()
        {
            // 绘制原有Inspector
            if (instance != null)
                instance.OnInspectorGUI();
        }

        // protected virtual void OnSceneGUI()
        // {
        //     if (instance != null && onSceneGUIMethod != null)
        //     {
        //         onSceneGUIMethod.Invoke(instance, emptyArray);
        //     }
        // }
    }
}