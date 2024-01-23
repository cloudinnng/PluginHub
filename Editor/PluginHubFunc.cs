using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

# if PH_WINFORMS
using System;
using System.Windows.Forms;
#endif

namespace PluginHub
{
    //this file contain common function in PluginHub
    public static class PluginHubFunc
    {
        // static PluginHubFunc() { }

        #region Const

        //可以使用EditorGUIUtility.whiteTexture获取纹理
        // public static readonly Texture2D WhiteTexture = EditorGUIUtility.whiteTexture;

        //选中的颜色
        public static readonly Color SelectedColor = new Color(0.572549f, 0.7960784f, 1f, 1f);

        //定义一个仅能容纳一个icon的小型按钮的尺寸，icon按钮专用
        private static readonly Vector2 iconBtnSize = new Vector2(28f, 19f);

        //ICON按钮的LayoutOption
        //用法示例： GUILayout.Button("X", PluginHubFunc.IconBtnLayoutOptions);
        public static readonly GUILayoutOption[] IconBtnLayoutOptions = new[]
            { GUILayout.Width(iconBtnSize.x), GUILayout.Height(iconBtnSize.y) };

        //项目唯一前缀(每个项目不一样，这样可以为每个项目存储不同的偏好)，用于存储EditorPrefs
        public static readonly string ProjectUniquePrefix = $"PH_{Application.companyName}_{Application.productName}";

        #endregion


        #region GUI Skin / Style

        private static GUISkin _skinUse;//do not call this, use PHGUISkin instead

        //Resources文件夹中的那个GUISkin
        public static GUISkin PHGUISkinUse
        {
            get
            {
                if (_skinUse == null)
                {
                    GUISkin originSkin = Resources.Load<GUISkin>("PluginHubGUISkin");
                    //复制一份，避免修改原文件;
                    _skinUse = Object.Instantiate(originSkin);
                    Resources.UnloadAsset(originSkin);
                }
                return _skinUse;
            }
        }

        public static GUIStyle GetCustomStyle(string styleName)
        {
            GUIStyle style = PHGUISkinUse.customStyles.Where(s => s.name.Equals(styleName)).First();
            if (style == null)
                Debug.LogError($"找不到样式：{styleName}");
            return style;
        }

        #endregion

        #region Draw

        public static Material DrawMaterialRow(string text, Material material)
        {
            Material returnMat;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(text);

                returnMat = (Material)EditorGUILayout.ObjectField(material, typeof(Material), true);

                DrawMaterialTypeLabel(material);
            }
            GUILayout.EndHorizontal();
            return returnMat;
        }

        //画材质类型标签(嵌入式材质还是自由材质)
        public static void DrawMaterialTypeLabel(Material material)
        {
            string matType = "";
            if (material == null)
                matType = "";
            else if (PluginHubFunc.IsEmbeddedMaterial(material))
                matType = "Embedded"; //嵌入式材质
            else
                matType = "Free"; //自由材质
            GUILayout.Label(matType, GUILayout.MaxWidth(50));
        }

        //绘制一个显示标题和内容的Label行，由两个label左右布局组成。
        public static void DrawTitleContentLabelRow(string title, string content)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(title, GUILayout.MaxWidth(150));
                GUILayout.Label(content);
            }
            GUILayout.EndHorizontal();
        }

        //绘制一个打开文件夹的按钮
        public static void DrawOpenFolderIconButton(string path, bool checkExist, string buttonTxt = null)
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
        public static void DrawCopyIconButton(string textToCopy)
        {
            //拷贝按钮
            if (GUILayout.Button(PluginHubFunc.Icon("d_TreeEditor.Duplicate", "", $"Duplicate\n{textToCopy}"),
                    PluginHubFunc.IconBtnLayoutOptions))
            {
                EditorGUIUtility.systemCopyBuffer = textToCopy;
            }
        }

        //绘制一个拷贝文件按钮，点击后会将文件拷贝到剪贴板,便于粘贴到支持Ctrl+v的应用程序中，如微信或文件管理器。
        //需要注意的是，这个功能只能在Windows平台下使用，因为使用了Windows.Forms的API
        //TODO
        public static void DrawCopyFileButton(string filePath)
        {
# if PH_WINFORMS
            if (GUILayout.Button(PluginHubFunc.Icon("d_TreeEditor.Duplicate", "", $"Duplicate file\n {filePath} to clipboard"),
                    PluginHubFunc.IconBtnLayoutOptions))
            {
                // 创建一个包含文件路径的StringCollection
                System.Collections.Specialized.StringCollection paths = new System.Collections.Specialized.StringCollection();
                paths.Add(filePath);

                // 将文件路径设置到剪切板
                Clipboard.SetFileDropList(paths);
            }
#else
            Debug.LogError("no PH_WINFORMS");
#endif
        }

        public static void TextBox(string text)
        {
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField(text, PHGUISkinUse.label);
            EditorGUILayout.EndHorizontal();
        }


        //一行两个文本
        public static void RowTwoText(string text0, string text1)
        {
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField(text0, PHGUISkinUse.label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(text1, PHGUISkinUse.label, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        private const float labelWidth = 130;
        private const bool expandWidth = false;

        public static T LableWithObjectFiled<T>(string lableText, Object obj) where T : Object
        {
            T objReturn;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth), GUILayout.ExpandWidth(expandWidth));
                objReturn = (T)EditorGUILayout.ObjectField(obj, typeof(T), true);
            }
            GUILayout.EndHorizontal();
            return objReturn;
        }

        public static bool LabelWithToggle(string lableText, bool toggleOldValue)
        {
            bool toggleReturn = false;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth), GUILayout.ExpandWidth(expandWidth));
                toggleReturn = GUILayout.Toggle(toggleOldValue, "");
            }
            GUILayout.EndHorizontal();
            return toggleReturn;
        }

        public static float LabelWithSlider(string lableText, float sliderOldValue, float leftValue, float rightValue)
        {
            float toggleReturn;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth));
                toggleReturn = GUILayout.HorizontalSlider(sliderOldValue, leftValue, rightValue);
                toggleReturn = EditorGUILayout.FloatField("", toggleReturn, GUILayout.Width(50));
            }
            GUILayout.EndHorizontal();
            return toggleReturn;
        }

        public static Vector3 LabelWithVector3Field(string lableText, Vector3 oldVector3)
        {
            Vector3 returnValue;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(lableText, GUILayout.Width(labelWidth));
                returnValue = EditorGUILayout.Vector3Field("", oldVector3);
            }
            GUILayout.EndHorizontal();
            return returnValue;
        }



        #endregion

        #region Layout Helper Functions

        /// <summary>
        /// 在for循环中进行水平布局，每行显示numberOfLines个对象，使用该方法判断是否应该开始水平布局
        /// </summary>
        /// <param name="i">循环索引</param>
        /// <param name="numberOfLines">一行按钮的个数，设置一行几个</param>
        /// <returns></returns>
        public static bool ShouldBeginHorizontal(int i, int numberOfLines)
        {
            return i % numberOfLines == 0;
        }

        public static bool ShouldEndHorizontal(int i, int numberOfLines)
        {
            return (i + 1) % numberOfLines == 0;
        }

        #endregion

        #region Operation

        //提取单个材质到同目录Materials文件夹内
        public static Material ExtractMaterial(Material embeddedMat)
        {
            //原理是复制该材质，存为材质资产，因此不会丢失原嵌入式材质
            //一般在检视面板都是提取整个fbx的材质，这个是提取单个材质，原理采用新建一个材质资产，复制其参数和纹理引用
            string savePath = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(embeddedMat)),
                $"Materials/{embeddedMat.name}.mat");
            string folderPath = Path.GetDirectoryName(savePath);

            if (Directory.Exists(folderPath) == false)
            {
                Debug.Log($"创建文件夹{folderPath}");
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                Material newMat = Object.Instantiate(embeddedMat);
                AssetDatabase.CreateAsset(newMat, savePath);
                Debug.Log($"已生产，点击定位。{savePath}。", newMat);
                // Selection.objects = new[] {newMat};//选中
                return newMat;
            }

            return null;
        }

        //选择对象然后自动跳转到检视面板
        public static void SelectObjectAndShowInspector(Object obj)
        {
            Selection.objects = new[] { obj };
            //open unity Inspector editor window
            EditorWindow.GetWindow(Type.GetType("UnityEditor.InspectorWindow,UnityEditor")).Show();
            // if(!EditorApplication.ExecuteMenuItem("Window/Panels/6 Inspector"))
            //     EditorApplication.ExecuteMenuItem("Window/Panels/7 Inspector");
        }

        #endregion

        #region Function

        //使用递归获取一个Transform的查找路径
        public static void GetFindPath(Transform transform, StringBuilder sb)
        {
            if (transform.parent == null)
            {
                sb.Insert(0, $"/{transform.name}");
                return;
            }

            sb.Insert(0, $"/{transform.name}");
            GetFindPath(transform.parent, sb);
        }

        public static void GC()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private static readonly GUIContent tempTextContent = new GUIContent();

        //获取一个带有tooltip的GuiContent
        public static GUIContent GuiContent(string text, string tooltip = "")
        {
            tempTextContent.text = text;
            tempTextContent.tooltip = tooltip;
            return tempTextContent;
        }

        //获取一个带Icon的GUIContent，也可以附加tooltip
        public static GUIContent Icon(string iconStr, string text = "", string tooltip = "")
        {
            //各种icon参见   https://unitylist.com/p/5c3/Unity-editor-icons
            GUIContent guiContent = EditorGUIUtility.IconContent(iconStr);
            guiContent.text = text;
            guiContent.tooltip = tooltip;
            return guiContent;
        }

        //返回一个材质是否是一个嵌入式材质
        public static bool IsEmbeddedMaterial(Material material)
        {
            return IsEmbeddedMaterial(AssetDatabase.GetAssetPath(material));
        }

        public static bool IsEmbeddedMaterial(string materialPath)
        {
            //不是.mat结尾，即为嵌入式材质
            return !materialPath.EndsWith(".mat");
        }


        //是否选中了新的材质
        public static bool IsSelectNewMaterial(Material oldMaterial)
        {
            if (Selection.objects == null)
                return false;
            if (Selection.objects.Length <= 0)
                return false;
            Material select = Selection.objects[0] as Material;
            if (select == null)
                return false;
            if (select == oldMaterial)
                return false;
            return true;
        }

        public static bool IsSelectMaterial()
        {
            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                return Selection.objects[0] is Material;
            }

            return false;
        }

        #endregion

        #region Alpha Function 测试函数，可能不稳定

        private static Quaternion startRotation;
        private static Quaternion rotateTarget;
        private static float t = 0;
        private static bool animWasRunning = false;

        //让场景视图相机看向目标物体，且有补间动画
        public static void RotateSceneViewCameraToTarget(Transform target)
        {
            if (SceneView.lastActiveSceneView == null) return; //编辑器不存在场景视图
            if (animWasRunning) return;

            Camera camera = SceneView.lastActiveSceneView.camera;
            Vector3 positionSave = camera.transform.position;
            startRotation = SceneView.lastActiveSceneView.rotation;
            rotateTarget = Quaternion.LookRotation(target.position - camera.transform.position);
            t = 0;
            SceneView.lastActiveSceneView.size = 0.01f;
            SceneView.lastActiveSceneView.pivot = positionSave;
            EditorApplication.update += RotateUpdate; //订阅更新函数  得以更新
            animWasRunning = true;
        }

        private static void RotateUpdate()
        {
            //Debug.Log("Anim Slerp update");
            t += 0.02f; //Anim speed
            if (t > 1) t = 1;
            Quaternion rotation = Quaternion.Slerp(startRotation, rotateTarget, t);
            rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0); //去除z旋转
            SceneView.lastActiveSceneView.rotation = rotation;
            if (t == 1)
            {
                EditorApplication.update -= RotateUpdate;
                animWasRunning = false;
            }
        }

        // private static Texture2D rightArrow;
        //
        // private static GUIContent ArrowRight(string text)
        // {
        //     GUIContent guiContent = Icon("UpArrow",text);
        //     if (rightArrow == null)
        //         rightArrow = rotateTexture(guiContent.image as Texture2D, true);
        //     guiContent.image = rightArrow;
        //     return guiContent;
        // }
        //
        // private static Texture2D rotateTexture(Texture2D originalTexture, bool clockwise)
        // {
        //     Texture2D newTexture= new Texture2D(originalTexture.width,originalTexture.height);
        //     Graphics.CopyTexture(originalTexture,0,0, newTexture,0,0);
        //
        //     Color32[] original = newTexture.GetPixels32();
        //     Color32[] rotated = new Color32[original.Length];
        //     int w = newTexture.width;
        //     int h = newTexture.height;
        //     int iRotated, iOriginal;
        //
        //     for (int j = 0; j < h; ++j)
        //     {
        //         for (int i = 0; i < w; ++i)
        //         {
        //             iRotated = (i + 1) * h - j - 1;
        //             iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
        //             rotated[iRotated] = original[iOriginal];
        //         }
        //     }
        //
        //     Texture2D rotatedTexture = new Texture2D(h, w);
        //     rotatedTexture.SetPixels32(rotated);
        //     rotatedTexture.Apply();
        //     return rotatedTexture;
        // }

        #endregion

        #region 快速折叠层级视图和项目文件夹功能

        //修改自 https://gist.github.com/yasirkula/0b541b0865eba11b55518ead45fba8fc

        private const BindingFlags INSTANCE_FLAGS =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const BindingFlags STATIC_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static void CollapseFolders()
        {
            Selection.objects = null;
            Selection.activeObject = null;
            EditorWindow projectWindow =
                typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser")
                    .GetField("s_LastInteractedProjectBrowser", STATIC_FLAGS).GetValue(null) as EditorWindow;
            if (projectWindow)
            {
                object assetTree = projectWindow.GetType().GetField("m_AssetTree", INSTANCE_FLAGS)
                    .GetValue(projectWindow);
                if (assetTree != null)
                    CollapseTreeViewController(projectWindow, assetTree,
                        (TreeViewState)projectWindow.GetType().GetField("m_AssetTreeState", INSTANCE_FLAGS)
                            .GetValue(projectWindow));

                object folderTree = projectWindow.GetType().GetField("m_FolderTree", INSTANCE_FLAGS)
                    .GetValue(projectWindow);
                if (folderTree != null)
                {
                    object treeViewDataSource = folderTree.GetType().GetProperty("data", INSTANCE_FLAGS)
                        .GetValue(folderTree, null);
                    int searchFiltersRootInstanceID = (int)typeof(EditorWindow).Assembly
                        .GetType("UnityEditor.SavedSearchFilters").GetMethod("GetRootInstanceID", STATIC_FLAGS)
                        .Invoke(null, null);
                    bool isSearchFilterRootExpanded = (bool)treeViewDataSource.GetType()
                        .GetMethod("IsExpanded", INSTANCE_FLAGS, null, new System.Type[] { typeof(int) }, null)
                        .Invoke(treeViewDataSource, new object[] { searchFiltersRootInstanceID });

                    CollapseTreeViewController(projectWindow, folderTree,
                        (TreeViewState)projectWindow.GetType().GetField("m_FolderTreeState", INSTANCE_FLAGS)
                            .GetValue(projectWindow),
                        isSearchFilterRootExpanded ? new int[1] { searchFiltersRootInstanceID } : null);

                    // Preserve Assets and Packages folders' expanded states because they aren't automatically preserved inside ProjectBrowserColumnOneTreeViewDataSource.SetExpandedIDs
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/e740821767d2290238ea7954457333f06e952bad/Editor/Mono/ProjectBrowserColumnOne.cs#L408-L420
                    InternalEditorUtility.expandedProjectWindowItems = (int[])treeViewDataSource.GetType()
                        .GetMethod("GetExpandedIDs", INSTANCE_FLAGS).Invoke(treeViewDataSource, null);

                    TreeViewItem rootItem = (TreeViewItem)treeViewDataSource.GetType()
                        .GetField("m_RootItem", INSTANCE_FLAGS).GetValue(treeViewDataSource);
                    if (rootItem.hasChildren)
                    {
                        foreach (TreeViewItem item in rootItem.children)
                            EditorPrefs.SetBool("ProjectBrowser" + item.displayName,
                                (bool)treeViewDataSource.GetType()
                                    .GetMethod("IsExpanded", INSTANCE_FLAGS, null, new System.Type[] { typeof(int) },
                                        null).Invoke(treeViewDataSource, new object[] { item.id }));
                    }
                }
            }
        }

        public static void CollapseGameObjects()
        {
            Selection.objects = null;
            EditorWindow hierarchyWindow =
                typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow")
                    .GetField("s_LastInteractedHierarchy", STATIC_FLAGS).GetValue(null) as EditorWindow;
            if (hierarchyWindow)
            {
#if UNITY_2018_3_OR_NEWER
                object hierarchyTreeOwner = hierarchyWindow.GetType().GetField("m_SceneHierarchy", INSTANCE_FLAGS)
                    .GetValue(hierarchyWindow);
#else
			object hierarchyTreeOwner = hierarchyWindow;
#endif
                object hierarchyTree = hierarchyTreeOwner.GetType().GetField("m_TreeView", INSTANCE_FLAGS)
                    .GetValue(hierarchyTreeOwner);
                if (hierarchyTree != null)
                {
                    List<int> expandedSceneIDs = new List<int>(4);
                    foreach (string expandedSceneName in (IEnumerable<string>)hierarchyTreeOwner.GetType()
                                 .GetMethod("GetExpandedSceneNames", INSTANCE_FLAGS).Invoke(hierarchyTreeOwner, null))
                    {
                        Scene scene = SceneManager.GetSceneByName(expandedSceneName);
                        if (scene.IsValid())
                            expandedSceneIDs.Add(scene
                                .GetHashCode()); // GetHashCode returns m_Handle which in turn is used as the Scene's instanceID by SceneHierarchyWindow
                    }

                    CollapseTreeViewController(hierarchyWindow, hierarchyTree,
                        (TreeViewState)hierarchyTreeOwner.GetType().GetField("m_TreeViewState", INSTANCE_FLAGS)
                            .GetValue(hierarchyTreeOwner), expandedSceneIDs);
                }
            }
        }


        private static void CollapseTreeViewController(EditorWindow editorWindow, object treeViewController,
            TreeViewState treeViewState, IList<int> additionalInstanceIDsToExpand = null)
        {
            object treeViewDataSource = treeViewController.GetType().GetProperty("data", INSTANCE_FLAGS)
                .GetValue(treeViewController, null);
            List<int> treeViewSelectedIDs = new List<int>(treeViewState.selectedIDs);
            int[] additionalInstanceIDsToExpandArray;
            if (additionalInstanceIDsToExpand != null && additionalInstanceIDsToExpand.Count > 0)
            {
                treeViewSelectedIDs.AddRange(additionalInstanceIDsToExpand);

                additionalInstanceIDsToExpandArray = new int[additionalInstanceIDsToExpand.Count];
                additionalInstanceIDsToExpand.CopyTo(additionalInstanceIDsToExpandArray, 0);
            }
            else
                additionalInstanceIDsToExpandArray = new int[0];

            treeViewDataSource.GetType().GetMethod("SetExpandedIDs", INSTANCE_FLAGS).Invoke(treeViewDataSource,
                new object[] { additionalInstanceIDsToExpandArray });
#if UNITY_2019_1_OR_NEWER
            treeViewDataSource.GetType().GetMethod("RevealItems", INSTANCE_FLAGS).Invoke(treeViewDataSource,
                new object[] { treeViewSelectedIDs.ToArray() });
#else
		foreach( int treeViewSelectedID in treeViewSelectedIDs )
			treeViewDataSource.GetType().GetMethod( "RevealItem", INSTANCE_FLAGS ).Invoke( treeViewDataSource, new object[] { treeViewSelectedID } );
#endif

            editorWindow.Repaint();
        }

        #endregion


        #region in development

        private static Rect _rect;

        //获取布局空间的可用宽度
        //实现原理：丢一个看不见的假label进去占满宽度，然后用GetlastRect获取其宽度
        //来自：https://forum.unity.com/threads/editorguilayout-get-width-of-inspector-window-area.82068/。最后一个留言
        public static float GetViewWidth()
        {
            GUILayout.Label("hack", GUILayout.MaxHeight(0));
            if (Event.current.type == EventType.Repaint)
            {
                // hack to get real view width
                _rect = GUILayoutUtility.GetLastRect();
            }

            return _rect.width;
        }

        #endregion
    }
}