using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Module
{
    public class SceneModule : PluginHubModuleBase
    {
        public override string moduleName
        {
            get { return "场景"; }
        }
        public override ModuleType moduleType => ModuleType.Shortcut;
        private List<SceneAsset> recentScene = new List<SceneAsset>(); //最近场景资产
        private bool showAllScene = false;

        private string filiterText
        {
            get { return EditorPrefs.GetString($"{PluginHubFunc.ProjectUniquePrefix}_SceneModule_FiliterText", ""); }
            set { EditorPrefs.SetString($"{PluginHubFunc.ProjectUniquePrefix}_SceneModule_FiliterText", value); }
        }


        string[] allScenePaths = null; //项目中所有场景文件的路径

        public override void OnEnable()
        {
            base.OnEnable();
            
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneLoaded -= OnSceneLoaded;
            EditorSceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.newSceneCreated -= OnSceneCreated;
            EditorSceneManager.newSceneCreated += OnSceneCreated;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;

            //当打开的时候，添加当前场景到最近场景中
            AddSceneToRecent(SceneManager.GetActiveScene());
        }

        
        
        protected override void DrawModuleDebug()
        {
            base.DrawModuleDebug();
            GUILayout.TextArea(recentScenePaths);
        }

        protected override void DrawGuiContent()
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Current Scene:");
                    Scene currScene = SceneManager.GetActiveScene();
                    SceneAsset currSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(currScene.path);
                    DrawSceneRow(-1, currScene.path, currSceneAsset, false);
                }
                GUILayout.EndHorizontal();

                Color oldColor = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label("Favorite Scene :");
                GUI.color = oldColor;

                //画出收藏的场景
                List<Object> foreachList = new List<Object>(RecordableAssets);
                for (int i = 0; i < foreachList.Count; i++)
                {
                    SceneAsset sceneAsset = foreachList[i] as SceneAsset;
                    if (sceneAsset == null) continue;
                    string path = AssetDatabase.GetAssetPath(sceneAsset);

                    DrawSceneRow(i, path, sceneAsset, true);
                }

                GUILayout.Label("Recent Scene : ");

                
                //画出最近场景
                LoadRecentScenes();
                for (int i = recentScene.Count - 1; i >= 0; i--)
                {
                    SceneAsset sceneAsset = recentScene[i];
                    string path = AssetDatabase.GetAssetPath(sceneAsset);
                    DrawSceneRow(i, path, sceneAsset, false);
                }


                //画出 项目中的所有场景
                string suffix = allScenePaths == null ? "" : allScenePaths.Length.ToString();

                if (GUILayout.Button($"Search all scene in project : {suffix}"))
                    showAllScene = !showAllScene;
                
                if (showAllScene)
                {
                    string[] scenePathsFilitered = allScenePaths;
                    if (scenePathsFilitered != null)
                    {
                        //过滤
                        scenePathsFilitered = scenePathsFilitered.Where(path =>
                        {
                            // string name = Path.GetFileName(p).ToLower();
                            string name = path.Replace("\\", "/");
                            return name.ToLower().Contains(filiterText.ToLower());
                        }).ToArray();
                    }

                    GUILayout.BeginHorizontal("Box");
                    GUILayout.Label("Input fillter : ", GUILayout.Width(100));
                    filiterText = EditorGUILayout.TextArea(filiterText);
                    if (GUILayout.Button("X", GUILayout.Width(28))) filiterText = "";
                    GUILayout.EndHorizontal();
                    //显示过滤后的数量
                    GUILayout.Label($"Result: {(scenePathsFilitered == null ? 0 : scenePathsFilitered.Length)}");
                    
                    if (scenePathsFilitered != null)
                    {
                        //显示场景
                        for (int i = 0; i < scenePathsFilitered.Length; i++)
                        {
                            string path = scenePathsFilitered[i];
                            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                            DrawSceneRow(i, path, sceneAsset, false);
                        }
                    }
                }
            }
            GUILayout.EndVertical();
        }

        public override void RefreshData()
        {
            base.RefreshData();
            if (showAllScene)
            {
                //寻找项目中所有场景文件的guid
                IEnumerable<string> iEnumerable = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets" });
                //获取到场景文件路径
                iEnumerable = iEnumerable.Select((s) => AssetDatabase.GUIDToAssetPath(s)).ToArray();
                //按场景名称字母顺序排序
                //iEnumerable = iEnumerable.OrderBy(s => Path.GetFileName(s)).ToArray();
                //按路径字母顺序排序
                iEnumerable = iEnumerable.OrderBy(s => (s)).ToArray();
                allScenePaths = iEnumerable.ToArray();
            }
        }
        private void AddSceneToRecent(Scene scene)
        {
            LoadRecentScenes();//先载入一下
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            if (sceneAsset != null)
            {
                if (recentScene.Contains(sceneAsset))//如果有 先删除
                    recentScene.Remove(sceneAsset);
                
                recentScene.Add(sceneAsset);
                if (recentScene.Count > 10)
                    recentScene.RemoveAt(0);
            }
            //保存最近场景到EditorPrefs
            SaveRecentScenes();
        }

        //画出一个展示一个场景文件的行GUI
        //inFavoriteList:  这个场景行GUI是否绘制在喜爱列表里
        private void DrawSceneRow(int id, string sceneAssetPath, SceneAsset sceneAsset, bool inFavoriteList)
        {
            GUILayout.BeginHorizontal();
            {
                if (id >= 0)
                    GUILayout.Label(id.ToString(), GUILayout.Width(25));
                
                //scene 
                EditorGUILayout.ObjectField(sceneAsset, typeof(SceneAsset), false);

                //draw curr scene icon
                if (SceneManager.GetActiveScene().path.Equals(sceneAssetPath))
                {
                    GUILayout.Label(PluginHubFunc.Icon("d_SceneAsset Icon", "", "this is current scene"), GUILayout.Height(19),
                        GUILayout.Width(20));
                }
                
                //open button
                if (GUILayout.Button(PluginHubFunc.GuiContent("Open", sceneAssetPath), GUILayout.Width(80)))
                {
                    //这段代码在切换场景前调用，若场景有未保存的更改，会弹出提示
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(sceneAssetPath);
                    }
                }

                //open folder button
                if (GUILayout.Button(PluginHubFunc.Icon("FolderEmpty On Icon", "", "open in explorer"), PluginHubFunc.IconBtnLayoutOptions))
                {
                    EditorUtility.RevealInFinder(sceneAssetPath);
                }

                if (!inFavoriteList)
                {
                    //star icon button
                    GUI.enabled = !RecordableObjectsContain(sceneAsset);
                    if (GUILayout.Button(PluginHubFunc.Icon("d_Favorite@2x", "", "Add this scene to favorite list"),
                            GUILayout.Height(19f), GUILayout.Width(28)))
                    {
                        if (!RecordableObjectsContain(sceneAsset))
                        {
                            AddRecordableObject(sceneAsset);
                        }
                        else
                        {
                            // Debug.Log("This scene is already in favorite list");
                        }
                    }
                    GUI.enabled = true;
                }

                if (inFavoriteList)
                {
                    GUI.enabled = id > 0;
                    if (GUILayout.Button("↑", PluginHubFunc.IconBtnLayoutOptions))
                    {
                        Object o = RecordableAssets[id];
                        RecordableAssets[id] = RecordableAssets[id - 1];
                        RecordableAssets[id - 1] = o;
                        SyncRecordableObjectsToEditorPrefs();
                    }

                    GUI.enabled = id < RecordableAssets.Count - 1;
                    if (GUILayout.Button("↓", PluginHubFunc.IconBtnLayoutOptions))
                    {
                        Object o = RecordableAssets[id];
                        RecordableAssets[id] = RecordableAssets[id + 1];
                        RecordableAssets[id + 1] = o;
                        SyncRecordableObjectsToEditorPrefs();
                    }

                    GUI.enabled = true;
                    //remove icon
                    if (GUILayout.Button(PluginHubFunc.Icon("winbtn_win_close@2x", "", "remove"), PluginHubFunc.IconBtnLayoutOptions))
                    {
                        RemoveRecordableObject(sceneAsset);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }


        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            //当打开场景，储存该场景
            AddSceneToRecent(scene);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AddSceneToRecent(scene);
        }

        private void OnSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            AddSceneToRecent(scene);
        }

        private void OnSceneClosed(Scene scene)
        {
            AddSceneToRecent(scene);
        }

        public override void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneLoaded -= OnSceneLoaded;
            EditorSceneManager.newSceneCreated -= OnSceneCreated;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
        }
        
        
        //用于存储最近的场景的列表，使用分号分隔
        private string recentScenePaths
        {
            get { return EditorPrefs.GetString($"{PluginHubFunc.ProjectUniquePrefix}_SceneModule_RecentScene", ""); }
            set { EditorPrefs.SetString($"{PluginHubFunc.ProjectUniquePrefix}_SceneModule_RecentScene", value); }
        }
        
        //将存储在EditorPrefs中的最近场景列表载入到recentScene中
        private void LoadRecentScenes()
        {
            recentScene.Clear();
            string recentSceneStr = recentScenePaths;
            if (!string.IsNullOrEmpty(recentSceneStr))
            {
                string[] scenePaths = recentSceneStr.Split(';');
                foreach (var scenePath in scenePaths)
                {
                    if (!string.IsNullOrEmpty(scenePath))
                    {
                        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                        recentScene.Add(sceneAsset);
                    }
                }
            }
        }
        //将recentScene中的场景储存到EditorPrefs中，使用分号分隔
        private void SaveRecentScenes()
        {
            recentScenePaths = string.Join(";", recentScene.Select(s => AssetDatabase.GetAssetPath(s)).ToArray());
        }
    }
}