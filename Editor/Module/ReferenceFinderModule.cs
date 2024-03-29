using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace PluginHub.Module
{
    //资产的引用查找与替换
    public class ReferenceFinderModule : PluginHubModuleBase
    {
        private string guidToFind = string.Empty;
        private string replacementGuid = string.Empty;
        private Object searchedObject;
        private Dictionary<Object, int> referenceObjects = new Dictionary<Object, int>();
        private Vector2 scrollPosition;
        private Stopwatch searchTimer = new Stopwatch();
        public override string moduleDescription => "";
        private Object guidObject;
        protected override void DrawGuiContent()
        {
            //序列化模式必须为ForceText
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                GUILayout.Label(
                    "The Reference Finder relies on readable meta files (visible text serialization).\nPlease change your serialization mode in \"Edit/Project Settings/Editor/Version Control\"\n to \"Visisble Meta Files\" and \"Asset Serialization\" to \"Force Text\".");
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            searchedObject = EditorGUILayout.ObjectField(
                searchedObject != null ? searchedObject.name : "Drag & Drop Asset",
                searchedObject, typeof(Object), false);

            if (GUILayout.Button("Use Selection Object", GUILayout.ExpandWidth(false)))
            {
                searchedObject = Selection.activeObject;
            }
            if (GUILayout.Button("Search"))
            {
                if (searchedObject != null)
                {
                    var pathToAsset = AssetDatabase.GetAssetPath(searchedObject);
                    guidToFind = AssetDatabase.AssetPathToGUID(pathToAsset);
                    Search();
                }
                else
                {
                    guidToFind = "";
                }
            }

            EditorGUILayout.EndHorizontal();

            //显示GUID
            var newGuidToFind = EditorGUILayout.TextField("GUID", guidToFind);
            if (!guidToFind.Equals(newGuidToFind))
                guidToFind = newGuidToFind;

            DisplayReferenceObjectList(referenceObjects);

            GUILayout.Label("输入GUID或者拖入对象来替换");
            GUILayout.BeginHorizontal();
            {
                guidObject = EditorGUILayout.ObjectField(guidObject, typeof(Object), false);
                if(guidObject != null)
                {
                    replacementGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(guidObject));
                }
                
                //replacementGuid输入框
                var newReplacementGuid = EditorGUILayout.TextField(replacementGuid);
                if (!replacementGuid.Equals(newReplacementGuid))
                    replacementGuid = newReplacementGuid;
                //Replace 按钮
                GUI.enabled = referenceObjects != null && referenceObjects.Count > 0;
                if (GUILayout.Button("Replace",GUILayout.ExpandWidth(false)))
                {
                    //show confirm dialog
                    if (EditorUtility.DisplayDialog("Replace GUID",
                        "Are you sure you want to replace all references to " + guidToFind + " with " + replacementGuid + "?",
                        "Yes", "No"))
                    {
                        ReplaceGuids(referenceObjects, guidToFind, replacementGuid);
                    }
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private void Search()
        {
            searchTimer.Reset();
            searchTimer.Start();

            referenceObjects.Clear();
            var pathToAsset = AssetDatabase.GUIDToAssetPath(guidToFind);
            if (!string.IsNullOrEmpty(pathToAsset))
            {
                searchedObject = AssetDatabase.LoadAssetAtPath<Object>(pathToAsset);

                var allPathToAssetsList = new List<string>();
                var allPrefabs = Directory.GetFiles(Application.dataPath, "*.prefab",
                    SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allPrefabs);
                var allMaterials =
                    Directory.GetFiles(Application.dataPath, "*.mat", SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allMaterials);
                var allScenes =
                    Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allScenes);
                var allControllers =
                    Directory.GetFiles(Application.dataPath, "*.controller", SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allControllers);
                var allVfxGraphs =
                    Directory.GetFiles(Application.dataPath, "*.vfx", SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allVfxGraphs);
                var allShaderGraphs = Directory.GetFiles(Application.dataPath, "*.shadergraph",
                    SearchOption.AllDirectories);
                allPathToAssetsList.AddRange(allShaderGraphs);

                string assetPath;
                for (int i = 0; i < allPathToAssetsList.Count; i++)
                {
                    assetPath = allPathToAssetsList[i];
                    var text = File.ReadAllText(assetPath);
                    var lines = text.Split('\n');
                    for (int j = 0; j < lines.Length; j++)
                    {
                        var line = lines[j];
                        if (line.Contains("guid:"))
                        {
                            if (line.Contains(guidToFind))
                            {
                                var pathToReferenceAsset =
                                    assetPath.Replace(Application.dataPath, string.Empty);
                                pathToReferenceAsset = pathToReferenceAsset.Replace(".meta", string.Empty);
                                var path = "Assets" + pathToReferenceAsset;
                                path = path.Replace(@"\", "/"); // fix OSX/Windows path
                                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                                if (asset != null)
                                {
                                    if (!referenceObjects.ContainsKey(asset))
                                    {
                                        referenceObjects.Add(asset, 0);
                                    }

                                    referenceObjects[asset]++;
                                }
                                else
                                {
                                    Debug.LogError(path + " could not be loaded");
                                }
                            }
                        }
                    }
                }

                searchTimer.Stop();
                //Debug.Log("Search took " + searchTimer.Elapsed);
            }
            else
            {
                Debug.LogError("no asset found for GUID: " + guidToFind);
            }
        }


        private void ReplaceGuids(Dictionary<Object, int> referenceObjects, string guidToFind, string replacementGuid)
        {
            foreach (var referenceObject in referenceObjects.Keys)
            {
                var assetPath = AssetDatabase.GetAssetPath(referenceObject);
                var text = File.ReadAllText(assetPath);
                var newText = text.Replace(guidToFind, replacementGuid);
                Debug.Log(
                    "Overwriting file data of: " + referenceObject.name + "\n\nOld:\n" + text + "\n\nNew:\n" + newText);
                File.WriteAllText(assetPath, newText);
            }

            AssetDatabase.Refresh(ImportAssetOptions.Default);
        }

        //显示找到的引用列表
        private void DisplayReferenceObjectList(Dictionary<Object, int> referenceObjectsDictionary)
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label($"Reference by: {referenceObjectsDictionary.Count} assets. (Last search duration: {searchTimer.Elapsed})");
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                foreach (var referenceObject in referenceObjectsDictionary)
                {
                    var referencingObject = referenceObject.Key;
                    var referenceCount = referenceObject.Value;
                    EditorGUILayout.ObjectField(referencingObject.name + " (" + referenceCount + ")", referencingObject,
                        typeof(Object), false);
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }
    }
}