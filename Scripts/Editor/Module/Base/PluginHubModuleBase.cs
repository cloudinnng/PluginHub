using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace PluginHub.Editor
{
    public enum ModuleType
    {
        None,
        Shortcut,
        Tool,
        Construction,
        Analyse,
    }

    /// <summary>
    /// 模块是一个可展开的卷展栏
    /// 这个类是 PluginHub 中模块的基类，所有模块都应继承这个类
    /// </summary>
    [System.Serializable]
    public abstract class PluginHubModuleBase
    {
        public int tabIndex { get; private set; } //该模块位于插件中心窗口中的Tab索引

        //若不覆写moduleName属性，则默认使用类名作为模块名称
        public virtual string moduleName => this.GetType().Name;

        // 模块类型,默认未分类
        public virtual ModuleType moduleType => ModuleType.None;

        //模块功能描述
        public virtual string moduleDescription => "无模块描述";

        //模块唯一标识前缀
        public string moduleIdentifyPrefix => $"{PluginHubFunc.ProjectUniquePrefix}_{GetType().Name}";

        public bool expand //展开状态
        {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_{GetType().Name}_ExpandState", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_{GetType().Name}_ExpandState", value); }
        }

        protected bool moduleDebug//模块debug模式
        {
            get { return EditorPrefs.GetBool($"{PluginHubFunc.ProjectUniquePrefix}_{GetType().Name}_moduleDebug", false); }
            set { EditorPrefs.SetBool($"{PluginHubFunc.ProjectUniquePrefix}_{GetType().Name}_moduleDebug", value); }
        }
            
            
        public bool isDataDirty { get; private set; }= true; //为真，该帧则会刷新数据
        // protected PluginHubWindow pluginHubWindow;
        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch(); //秒表对象 用于计算代码执行时间
        private float guiTimeLastFrame;
        //指示该模块是否正在绘制SceneGUI
        public bool isDrawingSceneGUI { private get; set; }

        private MonoScript _scriptObj;
        private MonoScript scriptObj
        {
            get
            {
                if (_scriptObj != null)
                    return _scriptObj;
                //获取脚本对象，便于快速进入模块的脚本文件
                string[] guids = AssetDatabase.FindAssets(GetType().Name); //找出这个脚本文件对象
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith($"{GetType().Name}.cs"))
                        return _scriptObj = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                }
                return null;
            }
        }

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

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    string prefixStrIcon = expand ? "▼" : "▶";
                    GUIContent guiContent = PluginHubFunc.GuiContent($"{prefixStrIcon} {moduleName}", $"{moduleDescription}");
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
                            PluginHubFunc.Icon("ParticleSystemForceField Gizmo", "", "该模块正在绘制场景GUI，您可以在场景视图状态栏中勾选 Always Refresh 来让场景GUI绘制更加即时"),
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

        //刷新数据，为避免每一个GUI绘制都进行数据更新，浪费性能，所以刷新数据统一在这个方法内执行.
        //会在每次模块获取焦点时调用,以及时刷新数据
        //RefreshData方法放在Update中调用,但会在OnFocus中通知其去调用一次,以在用户即将查看模块GUI时及时刷新数据
        public virtual void RefreshData()
        {
            if(moduleDebug)
                Debug.Log($"{moduleName} mudule : RefreshData");
            isDataDirty = false;
        }

        //绘制模块自己的debug内容
        protected virtual void DrawModuleDebug() { }

        //draw module gui content
        //子类实现 每个模块实现不同
        protected abstract void DrawGuiContent();

        #region Event Function

        //当模块展开时调用
        public virtual void OnFoldoutExpand()
        {
            if (moduleDebug) Debug.Log($"{moduleName} mudule : OnFoldoutExpand");
        }

        //当模块折叠时调用
        public virtual void OnFoldoutCollapse()
        {
            if (moduleDebug) Debug.Log($"{moduleName} mudule : OnFoldoutCollapse");
        }

        public bool m_OnSceneGUI(SceneView sceneView)
        {
            if (expand)
                return OnSceneGUI(sceneView);
            return false;
        }

        //子类实现这个方法以绘制属于模块的场景GUI
        //返回是否正在绘制GUI
        protected virtual bool OnSceneGUI(SceneView sceneView)
        {
            return false;
        }

        // 2024年4月7日 该模块生命周期方法暂时被禁用了,有bug,无法很好的使用
        //这个方法在Unity打开时就会执行，不需要打开PluginHubWindow
        // public virtual void OnInitOnload()
        // {
        //     Debug.Log($"{moduleName} mudule : OnInitOnload");
        // }

        #endregion

        #region EditorWindow Functions 这些方法让模块内部可以使用EditorWindow的生命周期方法

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

        //OnUpdate会一直调用，不论PluginHubWindow是否获得焦点，但在模块折叠时不调用
        public virtual void OnUpdate()
        {
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
        }

        #endregion

        #region 为子模块提供记录 Asset 功能，存储到EditorPrefs。用于保存模块的数据，例如场景模块的喜爱场景。

        public List<Object> RecordableAssets => _recordableAssets;

        //可记录对象,用于为模块保存每个模块可能用到的默认值。例如场景模块中，喜爱的场景作为可记录对象存储到了EditorPrefs中永久保存。
        private List<Object> _recordableAssets = new List<Object>();


        //用于存取EditorPrefs的key
        private string _recordableAssetsKey = "";
        private string RecordableAssetsKey {
            get
            {
                //初始化所有的记录对象
                if (string.IsNullOrWhiteSpace(_recordableAssetsKey))
                {
                    _recordableAssetsKey = $"{PluginHubFunc.ProjectUniquePrefix}_{GetType().Name}_RecordableObjectsKey";

                    _recordableAssets.Clear();
                    string savedDataStr = EditorPrefs.GetString(RecordableAssetsKey, "");
                    //存储格式 guid;guid;...
                    string[] guids = savedDataStr.Split(';');
                    //load
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string guid = guids[i];
                        if (!string.IsNullOrWhiteSpace(guid))
                        {
                            // 原理是存储的是资产文件
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            Object objAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
                            _recordableAssets.Add(objAsset);
                        }
                    }
                }
                return _recordableAssetsKey;
            }
        }

        // 手动同步记录对象到EditorPrefs,除了添加和删除的其他需要同步的时候调用
        // 例如在排序的时候需要调用这个方法
        public void SyncRecordableObjectsToEditorPrefs()
        {
            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < _recordableAssets.Count; j++)
            {
                string assetPath = AssetDatabase.GetAssetPath(_recordableAssets[j]);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    sb.Append(guid);
                    sb.Append(";");
                }
            }
            EditorPrefs.SetString(RecordableAssetsKey, sb.ToString());
        }

        protected bool RecordableObjectsContain(Object obj)
        {
            return _recordableAssets.Contains(obj);
        }

        protected void AddRecordableObject(Object obj)
        {
            _recordableAssets.Add(obj);
            SyncRecordableObjectsToEditorPrefs();
        }

        protected void RemoveRecordableObject(Object obj)
        {
            _recordableAssets.Remove(obj);
            SyncRecordableObjectsToEditorPrefs();
        }

        #endregion

        #region Draw GUI
        //绘制一个标签行，提供标题和内容，以及可选的复制内容按钮
        public void DrawRow(string title, string content, bool copyBtn = false, float titleWidth = 100)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.Width(titleWidth));

                //使用这个label，因为它是可以自动换行的
                GUILayout.Label(content, PluginHubFunc.PHGUISkinUse.label);

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


        private GUIStyle titleLabel => PluginHubFunc.GetCustomStyle("TitleLabel");
        //绘制一个含有标题的分隔线，用于分隔模块内的小功能
        public void DrawSplitLine(string title)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"--- {title} ---", titleLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        public void DrawIconBtnOpenFolder(string path, bool checkExist, string buttonTxt = null)
        {
            path = path.Replace("/", "\\");
            path = path.Replace("Assets\\..\\", ""); //EditorUtility.RevealInFinder(path);不支持（..）因此需要处理
            string checkPath = Path.GetDirectoryName(path);
            bool exist = checkExist ? Directory.Exists(checkPath) : true;
            GUI.enabled = exist;
            //open folder button
            if (GUILayout.Button(PluginHubFunc.Icon("FolderEmpty On Icon", buttonTxt, path),
                    (string.IsNullOrWhiteSpace(buttonTxt))
                        ? PluginHubFunc.IconBtnLayoutOptions[0]
                        : GUILayout.ExpandWidth(false),
                    PluginHubFunc.IconBtnLayoutOptions[1]))
            {
                Debug.Log($"打开文件夹:{path}");
                EditorUtility.RevealInFinder(path);
            }
            GUI.enabled = true;
        }

        //绘制一个拷贝文本的按钮，点击后会将文本拷贝到剪贴板
        public void DrawIconBtnCopy(string textToCopy)
        {
            //拷贝按钮
            if (GUILayout.Button(PluginHubFunc.Icon("d_TreeEditor.Duplicate", "", $"Duplicate\n{textToCopy}"),
                    PluginHubFunc.IconBtnLayoutOptions))
            {
                EditorGUIUtility.systemCopyBuffer = textToCopy;
            }
        }

        // 画一个星星icon按钮,这种按钮一般用于添加到收藏夹
        public bool DrawIconBtnStar(string tooltip = "Add to favorite")
        {
            return GUILayout.Button(PluginHubFunc.Icon("d_Favorite@2x", "", tooltip), PluginHubFunc.IconBtnLayoutOptions);
        }

        // 画一个删除icon按钮
        public bool DrawIconBtnDelete(string toolTip = "delete")
        {
            return GUILayout.Button(PluginHubFunc.Icon("P4_DeletedLocal@2x", "", toolTip),
                PluginHubFunc.IconBtnLayoutOptions);
        }


        #endregion
    }
}