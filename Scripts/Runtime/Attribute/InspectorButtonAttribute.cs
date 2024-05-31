using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = System.Object;

// 放在Runtime命名空间以免打包时[InspectorButton]特性报错
namespace PluginHub.Runtime
{
    // 在MonoBehaviour类中添加一个属性到方法上面，自动在inspector底部生成一个按钮，点击后执行该方法.便于开发调试
    // 在希望在inspector中生成按钮的方法上添加[InspectorButton("按钮名")]特性
    // 添加下面的代码到组件脚本文件的顶端即可使得[InspectorButton]特性生效
    // 例子:
    // #if UNITY_EDITOR
    // using UnityEditor;
    // [CustomEditor(typeof(YouComponentType))]
    // public class YouComponentTypeEditor : InspectorButtonEditor<YouComponentType> { }
    // #endif
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class InspectorButtonAttribute : PropertyAttribute
    {
        // 如不传,则按钮名字默认为方法名字+参数列表
        public readonly string buttonName;
        // 传入此按钮传入到方法的参数
        public readonly Object[] parameters;

        public InspectorButtonAttribute(string buttonName = "", params Object[] parameters)
        {
            this.buttonName = buttonName;
            this.parameters = parameters;
        }
    }

#if UNITY_EDITOR
    public class InspectorButtonEditor<T> : UnityEditor.Editor where T : MonoBehaviour
    {


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            T myComponent = (T)target;
            // 获取所有方法
            MethodInfo[] methods = myComponent.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            // 遍历所有方法
            foreach (MethodInfo method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(InspectorButtonAttribute), true);
                // 该方法被添加了[InspectorButton]特性
                if (attributes.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    {
                        ParameterInfo[] parameters = method.GetParameters();

                        foreach (var attribute in attributes)
                        {
                            InspectorButtonAttribute buttonAttribute = (InspectorButtonAttribute)attribute;

                            // 检查参数有效性
                            bool buttonValid = true;
                            if (parameters.Length != buttonAttribute.parameters.Length)// 参数数量不匹配
                                buttonValid = false;
                            if (buttonValid)
                            {
                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    // 参数类型不匹配
                                    if (parameters[i].ParameterType != buttonAttribute.parameters[i].GetType())
                                    {
                                        buttonValid = false;
                                        break;
                                    }
                                }
                            }


                            // 绘制按钮
                            string btnName = string.IsNullOrWhiteSpace(buttonAttribute.buttonName) ? $"{method.Name}{GetParametersString(buttonAttribute)}" : buttonAttribute.buttonName;
                            GUI.enabled = buttonValid;
                            if (GUILayout.Button(btnName))
                                method.Invoke(myComponent, buttonAttribute.parameters);
                            GUI.enabled = true;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

            }
        }


        static StringBuilder sb = new StringBuilder();
        // 拼接表示参数的字符串
        // 例如返回: (1,3,"string")
        private string GetParametersString(InspectorButtonAttribute buttonAttribute)
        {
            // 拼接表示参数的字符串
            sb.Clear();
            sb.Append("(");
            for (int i = 0; i < buttonAttribute.parameters.Length; i++)
            {
                sb.Append(buttonAttribute.parameters[i]);
                if (i != buttonAttribute.parameters.Length - 1)
                    sb.Append(", ");
            }
            sb.Append(")");
            return sb.ToString();
        }

    }
#endif
}