using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using PluginHub.Helper;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace PluginHub.Module
{
    /// <summary>
    /// 模块是一个可展开的卷展栏
    /// 这个类是基类
    /// 表示插件中心一个模块的基类，所以模块都应继承这个类
    /// </summary>
    [System.Serializable]
    public abstract class PluginHubModuleBase
    {
        public int tabIndex { get; private set; } //该模块位于插件中心窗口中的Tab索引

        //模块名称
        protected string m_ModuleName;

        //若不覆写moduleName属性，则默认使用类名作为模块名称
        public virtual string moduleName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(m_ModuleName))
                {
                    string className = this.GetType().Name;
                    //add space to each word
                    //e.g make "ModuleFoldoutBase" to "Module Foldout Base"
                    MatchCollection matchCollection = Regex.Matches(className, "[A-Z]");
                    for (int i = 1; i < matchCollection.Count; i++)
                    {
                        Match match = matchCollection[i];
                        className = className.Replace(match.Value, $" {match.Value}");
                    }
                    m_ModuleName = className;
                }

                return m_ModuleName;
            }
        }

        public bool expand //展开状态
        {
            get { return EditorPrefs.GetBool($"{GetType().Name}_ExpandState", true); }
            set { EditorPrefs.SetBool($"{GetType().Name}_ExpandState", value); }
        }

        protected bool moduleDebug//单独模块的debug模式
        {
            get { return EditorPrefs.GetBool($"{GetType().Name}_moduleDebug", false); }
            set { EditorPrefs.SetBool($"{GetType().Name}_moduleDebug", value); }
        }
            
            
        private bool isDataDirty = true; //为真，该帧则会刷新数据
        // protected PluginHubWindow pluginHubWindow;
        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch(); //秒表对象 用于计算代码执行时间
        private float guiTimeLastFrame;
        private bool isDrawingSceneGUI = false; //指示该模块是否正在绘制SceneGUI

        //data
        private Object scriptObj; //脚本对象,指示该脚本自身对象


        public PluginHubModuleBase() { }

        public PluginHubModuleBase Init(int tabIndex)
        {
            this.tabIndex = tabIndex;
            return this;
        }

        //外部调用
        public void DrawModule()
        {
            if (moduleDebug)
            {
                stopwatch.Restart();
                stopwatch.Start();
            }

            RefreshData();


            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    string charStr = expand ? "▼" : "▶";
                    GUIContent guiContent = PluginHubFunc.GuiContent($"{charStr} {moduleName}", "展开/收起");
                    //模块折叠按钮
                    GUI.color = expand ? PluginHubFunc.SelectedColor : Color.white;
                    if (GUILayout.Button(guiContent, GUILayout.Height(19)))
                    {
                        expand = !expand;
                        if (expand)
                            OnFoldoutExpand();
                        else
                            OnFoldoutCollapse();
                    }
                    GUI.color = Color.white;
                    // 指示该模块是否正在绘制SceneGUI
                    if (isDrawingSceneGUI)
                    {
                        GUILayout.Label(
                            PluginHubFunc.Icon("ParticleSystemForceField Gizmo", "", "this module is drawing SceneGUI"),
                            GUILayout.Width(19), GUILayout.Height(19));
                    }

                    //debug 模式切换按钮
                    Color oldColor = GUI.color;
                    //针对开启debug的模块  绘制按钮时给个颜色，突出显示
                    if (moduleDebug)
                        GUI.color = Color.red;
                    if (GUILayout.Button(PluginHubFunc.Icon("DebuggerDisabled", "", "Enable Module Debug"),
                            GUILayout.Height(19), GUILayout.ExpandWidth(false)))
                    {
                        moduleDebug = !moduleDebug;
                    }

                    GUI.color = oldColor;
                }
                GUILayout.EndHorizontal();

                if (expand)
                {
                    if (moduleDebug) //画模块debug内容  Draw Debug
                    {
                        GUILayout.BeginVertical(PluginHubFunc.GetCustomStyle("DebugPanel"));
                        {
                            //画脚本行便于快速进入
                            //draw script line for quick enter by double-click
                            GUILayout.BeginHorizontal();
                            {
                                GUI.enabled = false;
                                GUILayout.Label("Script File:");
                                EditorGUILayout.ObjectField("", scriptObj, typeof(PluginHubModuleBase), false); //画出这个脚本对象
                                GUI.enabled = true;
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.Label($"模块GUI时间：{guiTimeLastFrame}ms");
                            DrawModuleDebug();
                        }
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginVertical(PluginHubFunc.GetCustomStyle("ModulePanel"));
                    {
                        //画模块内容
                        DrawGuiContent();
                    }
                    GUILayout.EndVertical();

                    //draw split line
                    // GUIStyle centerLabel = PluginHubFunc.PHGUISkin.label;
                    // centerLabel.alignment = TextAnchor.MiddleCenter;
                    // GUILayout.Label($"------{moduleName} END------", centerLabel, GUILayout.ExpandWidth(true));
                }
            }
            GUILayout.EndVertical();

            if (moduleDebug)
            {
                stopwatch.Stop(); //  停止监视
                guiTimeLastFrame = (float)stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        //刷新数据，为避免每一个GUI绘制都进行数据更新，浪费性能，所以刷新数据统一在这个方法内执行
        protected virtual void RefreshData()
        {
            if (!isDataDirty) return;
            isDataDirty = false;


            //获取脚本对象，绘制，用于快速进入脚本。
            string filter = $"t:Script {this.GetType()}";//Cloudinnng.CFramework.Editor.MaterialInspectModule
            filter = filter.Substring(filter.LastIndexOf('.')+1);//MaterialInspectModule
            // Debug.Log(filter);
            string[] guids = AssetDatabase.FindAssets(filter); //找出这个脚本文件对象
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            scriptObj = AssetDatabase.LoadAssetAtPath<Object>(path);
        }

        //draw module own debug content
        protected virtual void DrawModuleDebug()
        {
        }

        //draw module gui content
        //子类实现 每个模块实现不同
        protected abstract void DrawGuiContent();

        #region Event Function

        //当模块展开时调用
        public virtual void OnFoldoutExpand()
        {
            SceneView.duringSceneGui -= this.m_OnSceneGUI;
            SceneView.duringSceneGui += this.m_OnSceneGUI;
        }

        //当模块折叠时调用
        public virtual void OnFoldoutCollapse()
        {
            SceneView.duringSceneGui -= this.m_OnSceneGUI;
            isDrawingSceneGUI = false;
        }

        private void m_OnSceneGUI(SceneView sceneView)
        {
            isDrawingSceneGUI = OnSceneGUI(sceneView);
        }

        //实现这个方法以绘制场景GUI
        public virtual bool OnSceneGUI(SceneView sceneView)
        {
            //绘制成功返回true
            return false;
        }

        #endregion

        #region EditorWindow function 这些方法让模块内部可以使用EditorWindow的内置函数

        public virtual void OnEnable()
        {
            if (moduleDebug) Debug.Log($"{moduleName} mudule : OnEnable");

            if (expand)
                OnFoldoutExpand();
        }

        public virtual void OnDisable()
        {
            if (moduleDebug) Debug.Log($"{moduleName} mudule : OnDisable");
        }

        public virtual void OnUpdate()
        {
            InitRecordableObjects();

            //if(muduleDebug)Debug.Log($"{muduleName} mudule : OnUpdate");
        }

        public virtual void OnFocus()
        {
            if (moduleDebug) Debug.Log($"{moduleName} module : OnFocus");

            isDataDirty = true;
        }

        public virtual void OnDestroy()
        {
            if (moduleDebug) Debug.Log($"{moduleName} mudule : OnDestroy");

            SceneView.duringSceneGui -= this.m_OnSceneGUI;
        }

        #endregion

        #region 为子模块提供记录 Object 功能，存储到EditorPrefs

        public List<Object> RecordableObjects => recordableObjects;

        //可记录对象,用于为模块保存每个模块可能用到的默认值。例如场景模块中，喜爱的场景作为可记录对象存储到了EditorPrefs中永久保存。
        private List<Object> recordableObjects = new List<Object>();
        private string recordableObjectsKey = ""; //用于存取EditorPrefs的key

        //子类不需要调用
        protected void InitRecordableObjects()
        {
            //初始化所有的记录对象
            if (string.IsNullOrWhiteSpace(recordableObjectsKey))
            {
                recordableObjectsKey = $"{PluginHubFunc.ProjectUniquePrefix}_{GetType().Name}_RecordableObjectsKey";

                recordableObjects.Clear();
                string savedDataStr = EditorPrefs.GetString(recordableObjectsKey, "");
                //存储格式 guid;guid;...
                string[] guids = savedDataStr.Split(';');
                //load 
                for (int i = 0; i < guids.Length; i++)
                {
                    string guid = guids[i];
                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Object objAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
                        recordableObjects.Add(objAsset);
                    }
                }
            }
        }

        public void SyncRecordableObjectsToEditorPrefs()
        {
            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < recordableObjects.Count; j++)
            {
                string assetPath = AssetDatabase.GetAssetPath(recordableObjects[j]);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    sb.Append(guid);
                    sb.Append(";");
                }
            }

            EditorPrefs.SetString(recordableObjectsKey, sb.ToString());
        }

        protected bool RecordableObjectsContain(Object obj)
        {
            return recordableObjects.Contains(obj);
        }

        protected void AddRecordableObject(Object obj)
        {
            recordableObjects.Add(obj);
            SyncRecordableObjectsToEditorPrefs();
        }

        protected void RemoveRecordableObject(Object obj)
        {
            recordableObjects.Remove(obj);
            SyncRecordableObjectsToEditorPrefs();
        }

        #endregion

        #region Layout Helper Function  布局助手函数
        private const float titleWidth = 100;

        //绘制一个标签项目行，包含标题和内容，以及可选的复制内容按钮
        public void DrawRow(string title, string content, bool copyBtn = false)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(titleWidth));

                //使用这个label，因为它是可以自动换行的
                GUILayout.Label(content, PluginHubFunc.PHGUISkin.label);

                GUILayout.FlexibleSpace();
                //拷贝按钮
                if (copyBtn && GUILayout.Button(PluginHubFunc.Icon("d_TreeEditor.Duplicate", "", "Duplicate"),
                        PluginHubFunc.IconBtnLayoutOptions))
                {
                    EditorGUIUtility.systemCopyBuffer = content;
                }
            }
            GUILayout.EndHorizontal();
        }

        #endregion
    }
}