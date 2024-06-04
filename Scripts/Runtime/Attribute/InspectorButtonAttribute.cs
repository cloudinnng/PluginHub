using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
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

        #region 构造方法
        public InspectorButtonAttribute(string buttonName, params Object[] parameters)
        {
            this.buttonName = buttonName;
            this.parameters = parameters;
        }
        public InspectorButtonAttribute(params Object[] parameters)
        {
            this.buttonName = "";
            this.parameters = parameters;
        }
        #endregion
    }

    // 可以与[InspectorButton]特性混合使用，以在检视面板对应位置添加一个标题，用于分组以便识别
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class InspectorButtonHeaderAttribute : PropertyAttribute
    {
        public readonly string headerLabel;
        public InspectorButtonHeaderAttribute(string headerLabel)
        {
            this.headerLabel = headerLabel;
        }
    }

#if UNITY_EDITOR
    public class InspectorButtonEditor<T> : UnityEditor.Editor where T : MonoBehaviour
    {

        private List<List<PropertyAttribute>> _attributes = new List<List<PropertyAttribute>>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            T myComponent = (T)target;
            // 获取所有方法
            MethodInfo[] methods = myComponent.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            // 遍历每一个方法
            foreach (MethodInfo method in methods)
            {

                Object[] customAttribute = method.GetCustomAttributes(true);
                // 获取所有[InspectorButton]和[InspectorButtonHeader]特性
                customAttribute = Array.FindAll(customAttribute, (obj) => obj is InspectorButtonAttribute || obj is InspectorButtonHeaderAttribute);
                if (customAttribute.Length == 0) // 没有[InspectorButton]或[InspectorButtonHeader]特性
                    continue;

                // 先整理到一个List<List<PropertyAttribute>>中
                _attributes.Clear();
                for (int i = 0; i < customAttribute.Length; i++)
                {
                    PropertyAttribute attribute = (PropertyAttribute)customAttribute[i];
                    PropertyAttribute lastAttribute=null;
                    if(i >0)
                        lastAttribute = (PropertyAttribute)customAttribute[i - 1];

                    if (_attributes.Count == 0)
                    {
                        _attributes.Add(new List<PropertyAttribute>());
                        _attributes[0].Add(attribute);
                    }
                    else
                    {
                        if(attribute.GetType() != lastAttribute.GetType()){
                            _attributes.Add(new List<PropertyAttribute>());
                            _attributes[_attributes.Count-1].Add(attribute);
                        }else
                        {
                            _attributes[_attributes.Count-1].Add(attribute);
                        }
                    }
                }

                //进行绘制
                for (int i = 0; i < _attributes.Count; i++)
                {
                    // 绘制Header标题
                    if (_attributes[i][0] is InspectorButtonHeaderAttribute headerAttribute)
                    {
                        GUILayout.Label(headerAttribute.headerLabel, EditorStyles.boldLabel);
                    }
                    else// 绘制按钮
                    {
                        GUILayout.BeginHorizontal();
                        {
                            for(int j = 0; j < _attributes[i].Count; j++)
                            {
                                InspectorButtonAttribute buttonAttribute = (InspectorButtonAttribute)_attributes[i][j];
                                // 检查参数有效性
                                bool buttonValid = true;
                                ParameterInfo[] methodParameters = method.GetParameters();
                                if (methodParameters.Length != buttonAttribute.parameters.Length)// 参数数量不匹配
                                    buttonValid = false;
                                if (buttonValid)
                                {
                                    for (int k = 0; k < methodParameters.Length; k++)
                                    {
                                        // 参数类型不匹配
                                        if (methodParameters[k].ParameterType != buttonAttribute.parameters[k].GetType())
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