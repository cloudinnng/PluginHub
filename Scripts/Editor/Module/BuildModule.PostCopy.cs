using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    // 该功能开启后会在构建完成后复制【指定文件夹】到构建目录下
    // 源目录和目标目录可以是相对路径或绝对路径
    //     源目录：相对路径相对于工程根目录
    //     目标目录：相对路径相对于构建输出根目录（与exe同级）
    // 复制时保留【源文件夹】，例如源 D:\\testfolder → D:\\[BuildDir]\\testfolder
    public partial class BuildModule : PluginHubModuleBase
    {
        // 是否启用构建后复制文件夹到构建目录
        private static bool enablePostCopy
        {
            get => EditorPrefs.GetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_enablePostCopy", false);
            set => EditorPrefs.SetBool($"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_enablePostCopy", value);
        }

        private static string PostCopyFolderPathsEditorPrefsKey =>
            $"{PluginHubEditor.ProjectUniquePrefix}_BuildModule_postCopyFolderPaths";

        /// <summary>
        /// 构建后要复制的文件夹映射：源目录 → 目标目录（相对构建输出根目录或绝对路径）。
        /// </summary>
        private static List<Tuple<string, string>> postCopyFolderPaths
        {
            get
            {
                string json = EditorPrefs.GetString(PostCopyFolderPathsEditorPrefsKey, "");
                if (string.IsNullOrWhiteSpace(json))
                    return new List<Tuple<string, string>>();

                try
                {
                    PostCopyFolderPathsStorage storage = JsonUtility.FromJson<PostCopyFolderPathsStorage>(json);
                    if (storage?.entries == null || storage.entries.Length == 0)
                        return new List<Tuple<string, string>>();

                    return storage.entries
                        .Select(e => Tuple.Create(e.source ?? "", e.destination ?? ""))
                        .ToList();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BuildModule] 读取 postCopyFolderPaths 失败，将使用空列表。{e.Message}");
                    return new List<Tuple<string, string>>();
                }
            }
            set
            {
                PostCopyFolderPathsStorage storage = new PostCopyFolderPathsStorage
                {
                    entries = (value ?? new List<Tuple<string, string>>())
                        .Select(t => new PostCopyFolderPathEntry
                        {
                            source = t.Item1 ?? "",
                            destination = t.Item2 ?? ""
                        })
                        .ToArray()
                };
                string json = JsonUtility.ToJson(storage);
                EditorPrefs.SetString(PostCopyFolderPathsEditorPrefsKey, json);
            }
        }

        [Serializable]
        private class PostCopyFolderPathEntry
        {
            public string source = "";
            public string destination = "";
        }

        [Serializable]
        private class PostCopyFolderPathsStorage
        {
            public PostCopyFolderPathEntry[] entries = Array.Empty<PostCopyFolderPathEntry>();
        }

        /// <summary>
        /// 构建完成后，按配置将源文件夹复制到构建目录下的目标路径。
        /// </summary>
        private static void ExecutePostCopyFolders(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (!enablePostCopy)
                return;

            List<Tuple<string, string>> copyList = postCopyFolderPaths;
            if (copyList == null || copyList.Count == 0)
            {
                Debug.Log("[BuildModule] 已启用构建后复制，但未配置任何路径，跳过。");
                return;
            }

            string buildRoot = GetBuildOutputRootDirectory(pathToBuiltProject);
            if (string.IsNullOrWhiteSpace(buildRoot))
            {
                Debug.LogWarning($"[BuildModule] 无法解析构建输出根目录，跳过构建后复制。pathToBuiltProject={pathToBuiltProject}");
                return;
            }

            Debug.Log($"[BuildModule] 开始构建后复制，平台={buildTarget}，构建根目录={buildRoot}，共 {copyList.Count} 条。");

            foreach (Tuple<string, string> pair in copyList)
            {
                string sourcePath = ResolveProjectPath(pair.Item1);
                if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
                {
                    Debug.LogWarning($"[BuildModule] 构建后复制跳过：源目录无效 → {pair.Item1}");
                    continue;
                }

                string destPath = ResolveDestinationPath(pair.Item2, buildRoot, pathToBuiltProject);
                if (string.IsNullOrWhiteSpace(destPath))
                {
                    Debug.LogWarning($"[BuildModule] 构建后复制跳过：目标目录为空，源={sourcePath}");
                    continue;
                }

                // 保留源文件夹名，例如 testfolder → .../StreamingAssets/testfolder/
                string finalDestPath = ResolveFinalCopyDestination(sourcePath, destPath);

                try
                {
                    CopyDirectoryRecursive(sourcePath, finalDestPath);
                    Debug.Log($"[BuildModule] 构建后复制完成：{sourcePath} → {finalDestPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BuildModule] 构建后复制失败：{sourcePath} → {finalDestPath}，{e.Message}");
                }
            }
        }

        /// <summary>
        /// 构建后复制路径列表：每条含源目录、目标目录，支持增删与选择文件夹。
        /// </summary>
        private void DrawPostCopyFolderPathsUI()
        {
            List<Tuple<string, string>> paths = postCopyFolderPaths;
            bool changed = false;


            for (int i = 0; i < paths.Count; i++)
            {
                string source = paths[i].Item1;
                string destination = paths[i].Item2;

                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"#{i + 1}", GUILayout.Width(24));
                        if (DrawIconBtnDelete("删除此复制项"))
                        {
                            paths.RemoveAt(i);
                            changed = true;
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                            i--;
                            continue;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        string newSource = EditorGUILayout.TextField("源目录", source);
                        if (GUILayout.Button("…", GUILayout.Width(22)) &&
                            TryPickFolder("选择源文件夹", newSource, out string pickedSource))
                        {
                            newSource = pickedSource;
                        }

                        if (newSource != source)
                        {
                            source = newSource;
                            changed = true;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        string newDestination = EditorGUILayout.TextField("目标目录", destination);
                        if (GUILayout.Button("…", GUILayout.Width(22)) &&
                            TryPickFolder("选择目标文件夹（可选，默认可手填相对路径）", destination, out string pickedDest))
                        {
                            newDestination = pickedDest;
                        }

                        if (newDestination != destination)
                        {
                            destination = newDestination;
                            changed = true;
                        }
                    }
                    GUILayout.EndHorizontal();

                    paths[i] = Tuple.Create(source, destination);
                }
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("添加复制项"))
            {
                string defaultDestination = GetDefaultPostCopyDestination();
                paths.Add(Tuple.Create("", defaultDestination));
                changed = true;
                Debug.Log($"[BuildModule] 已添加一条空的构建后复制配置，默认目标={defaultDestination}");
            }

            if (changed)
                postCopyFolderPaths = paths;
        }

        private static bool TryPickFolder(string title, string defaultPath, out string pickedPath)
        {
            pickedPath = null;
            string startFolder = ResolveProjectPath(defaultPath);
            if (!string.IsNullOrWhiteSpace(startFolder) && Directory.Exists(startFolder))
            {
                // startFolder = startFolder;
            }
            else
            {
                startFolder = Path.GetDirectoryName(Application.dataPath);
            }

            pickedPath = EditorUtility.OpenFolderPanel(title, startFolder, "");
            return !string.IsNullOrWhiteSpace(pickedPath);
        }



        private static string GetBuildOutputRootDirectory(string pathToBuiltProject)
        {
            if (string.IsNullOrWhiteSpace(pathToBuiltProject))
                return null;

            if (File.Exists(pathToBuiltProject))
                return Path.GetDirectoryName(pathToBuiltProject);

            if (Directory.Exists(pathToBuiltProject))
                return pathToBuiltProject;

            return Path.GetDirectoryName(pathToBuiltProject);
        }

        private static string ResolveProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        /// <summary>
        /// 新增复制项时的默认目标（相对 exe 同级目录）：{项目名}_Data/StreamingAssets。
        /// </summary>
        private static string GetDefaultPostCopyDestination()
        {
            string productName = PlayerSettings.productName;
            if (string.IsNullOrWhiteSpace(productName))
            {
                productName = "Game";
                Debug.LogWarning("[BuildModule] PlayerSettings.productName 为空，默认目标将使用 Game_Data/StreamingAssets。");
            }

            string defaultDestination = $"{productName}_Data/StreamingAssets";
            return defaultDestination;
        }

        /// <summary>
        /// 解析构建后复制的目标路径。目标为 /StreamingAssets 时映射到包体 StreamingAssets 目录。
        /// </summary>
        private static string ResolveDestinationPath(string destination, string buildRoot, string pathToBuiltProject)
        {
            if (string.IsNullOrWhiteSpace(destination))
                return null;

            if (TryParseStreamingAssetsDestination(destination, out string subFolder))
            {
                string playerStreamingAssets = GetPlayerStreamingAssetsDirectory(pathToBuiltProject, buildRoot);
                if (string.IsNullOrWhiteSpace(playerStreamingAssets))
                {
                    Debug.LogWarning($"[BuildModule] 无法解析包体 StreamingAssets 目录，目标={destination}");
                    return null;
                }

                string dest = string.IsNullOrWhiteSpace(subFolder)
                    ? playerStreamingAssets
                    : Path.Combine(playerStreamingAssets, subFolder);
                Debug.Log($"[BuildModule] 目标 /StreamingAssets 已映射到包体路径：{dest}");
                return Path.GetFullPath(dest);
            }

            if (Path.IsPathRooted(destination))
                return Path.GetFullPath(destination);

            return Path.GetFullPath(Path.Combine(buildRoot, destination));
        }

        /// <summary>
        /// 目标是否为包体 StreamingAssets（如 /StreamingAssets 或 StreamingAssets/子目录）。
        /// </summary>
        private static bool TryParseStreamingAssetsDestination(string destination, out string subFolder)
        {
            subFolder = null;
            if (string.IsNullOrWhiteSpace(destination))
                return false;

            string normalized = destination.Replace('\\', '/').Trim();
            while (normalized.StartsWith("/"))
                normalized = normalized.Substring(1);

            if (normalized.Equals("StreamingAssets", StringComparison.OrdinalIgnoreCase))
            {
                subFolder = "";
                return true;
            }

            const string prefix = "StreamingAssets/";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                subFolder = normalized.Substring(prefix.Length);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取 Player 包体内的 StreamingAssets 目录（{exe名}_Data/StreamingAssets）。
        /// </summary>
        private static string GetPlayerStreamingAssetsDirectory(string pathToBuiltProject, string buildRoot)
        {
            string buildDirectory = null;
            string productName = null;

            if (!string.IsNullOrWhiteSpace(pathToBuiltProject) && File.Exists(pathToBuiltProject))
            {
                buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
                productName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            }
            else if (!string.IsNullOrWhiteSpace(buildRoot) && Directory.Exists(buildRoot))
            {
                buildDirectory = buildRoot;
                // 构建根目录名通常与 exe / _Data 文件夹前缀一致
                productName = Path.GetFileName(buildDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }

            if (string.IsNullOrWhiteSpace(buildDirectory) || string.IsNullOrWhiteSpace(productName))
                return null;

            string dataStreamingAssets = Path.Combine(buildDirectory, $"{productName}_Data", "StreamingAssets");
            if (Directory.Exists(dataStreamingAssets))
                return dataStreamingAssets;

            // _Data 可能尚未创建时仍返回预期路径，供复制时创建
            Debug.Log($"[BuildModule] 包体 StreamingAssets 目录尚不存在，将创建：{dataStreamingAssets}");
            return dataStreamingAssets;
        }

        /// <summary>
        /// 确定最终复制目标：在目标父目录下保留源文件夹名；
        /// 若目标路径末尾已与源文件夹同名，则直接写入该目录（避免 testfolder/testfolder）。
        /// </summary>
        private static string ResolveFinalCopyDestination(string sourcePath, string resolvedDestPath)
        {
            string sourceFolderName = Path.GetFileName(
                sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string destFolderName = Path.GetFileName(
                resolvedDestPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (string.Equals(sourceFolderName, destFolderName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[BuildModule] 目标已包含源文件夹名，直接复制到：{resolvedDestPath}");
                return resolvedDestPath;
            }

            string finalPath = Path.Combine(resolvedDestPath, sourceFolderName);
            Debug.Log($"[BuildModule] 保留源文件夹名：{sourceFolderName} → {finalPath}");
            return finalPath;
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }

    }
}