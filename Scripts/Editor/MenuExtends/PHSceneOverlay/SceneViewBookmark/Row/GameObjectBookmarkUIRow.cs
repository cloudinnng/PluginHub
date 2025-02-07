using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    public class GameObjectBookmarkUIRow : IBookmarkUIRow
    {
        private Texture postProcessVolumeIcon;

        //遇到这些字符串,注意是外部Resources文件夹下的icon图标
        private string[] externalIcon = new[] { "PostProcessing" };

        protected override void DrawHorizontalInnerGUI(SceneBookmarkGroup group)
        {
            // Debug.Log("GameObjectBookmarkUIRow DrawHorizontalInnerGUI");
            GUI.color = Selection.activeGameObject != null
                ? BookmarkSettings.COLOR_BOOKMARK_BUTTON_ACTIVE
                : Color.white;
            //画行首的图标
            GUILayout.Label(EditorGUIUtility.IconContent("GameObject On Icon").image, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y));

            GUI.color = Color.white;

            for (int i = 0; i < BookmarkSettings.BOOKMARK_COUNT; i++)
            {
                GameObjectBookmark gameObjectBookmark = group.gameObjectPaths[i];
                GUI.color = gameObjectBookmark.valid
                    ? (gameObjectBookmark.IsActivated()
                        ? BookmarkSettings.COLOR_BOOKMARK_BUTTON_ACTIVE
                        : BookmarkSettings.COLOR_BOOKMARK_BUTTON_NORMAL)
                    : BookmarkSettings.COLOR_BOOKMARK_BUTTON_EMPTY;

                string showName = (i + 1).ToString();

                if (gameObjectBookmark.valid)
                {
                    GUIContent icon;
                    //图标图片非Unity内置图片
                    if (externalIcon.Contains(gameObjectBookmark.componentName))
                    {
                        if (postProcessVolumeIcon == null)
                            postProcessVolumeIcon = Resources.Load<Texture>(gameObjectBookmark.componentName);
                        icon = new GUIContent(postProcessVolumeIcon);
                    }
                    else
                    {
                        //图标可以使用Unity内置图片
                        icon = EditorGUIUtility.IconContent($"d_{gameObjectBookmark.componentName} Icon");
                    }

                    //1，2，3，4，5 快捷键选择游戏对象
                    bool shortKeyPress = Event.current.keyCode == KeyCode.Alpha1 + i
                                         // && Event.current.type == EventType.KeyUp
                                         //没有按下ctrl alt shift
                                         && !Event.current.control && !Event.current.alt && !Event.current.shift;

                    if (GUILayout.Button(icon, BookmarkButtonStyle, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y)) || shortKeyPress)
                    {
                        HandleButton(gameObjectBookmark);
                    }
                }
                else
                {
                    if (GUILayout.Button(showName, BookmarkButtonStyle, GUILayout.Width(BookmarkSettings.BUTTON_SIZE.x), GUILayout.Height(BookmarkSettings.BUTTON_SIZE.y)))
                    {
                        HandleButton(gameObjectBookmark);
                    }
                }
                GUI.color = Color.white;


                Rect rect = GUILayoutUtility.GetLastRect();
                if (gameObjectBookmark.valid)
                {
                    //右下角画一个lable表示快捷键
                    Rect rect1 = rect;
                    rect1.x += 20;
                    rect1.y += 20;
                    rect1.width = 10;
                    rect1.height = 10;
                    GUI.Label(rect1, (i + 1).ToString());
                }
            }
        }



        private void HandleButton(GameObjectBookmark gameObjectBookmark)
        {
            if (Event.current.control) //按了ctrl
            {
                if (Event.current.button == 0) //左键保存
                {
                    GameObject gameObject = Selection.activeGameObject;
                    if (gameObject != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        GetFindPath(gameObject.transform, sb);
                        gameObjectBookmark.text = sb.ToString();
                        gameObjectBookmark.componentName = GetPrimaryComponentName(gameObject);
                        BookmarkAssetSO.Instance.Save();
                    }
                }
                else if (Event.current.button == 1) //右键清空
                {
                    gameObjectBookmark.text = "";
                    gameObjectBookmark.componentName = "";
                    BookmarkAssetSO.Instance.Save();
                }
            }
            else //没按ctrl 载入
            {
                if (gameObjectBookmark.valid)
                {
                    Selection.activeGameObject = GameObject.Find(gameObjectBookmark.text);
                }
            }
        }

        //使用递归获取一个Transform的查找路径
        protected void GetFindPath(Transform transform, StringBuilder sb)
        {
            if (transform.parent == null)
            {
                sb.Insert(0, $"/{transform.name}");
                return;
            }
            sb.Insert(0, $"/{transform.name}");
            GetFindPath(transform.parent, sb);
        }

        private string GetPrimaryComponentName(GameObject gameObject)
        {
            //先收集所有的组件
            Component[] components = gameObject.GetComponents<Component>();
            List<string> componentNames = new List<string>();
            foreach (var component in components)
            {
                string typeName = component.GetType().ToString();
                componentNames.Add(typeName);
                // Debug.Log(typeName); // 打印所有组件的名字,测试用
            }

            // 优先返回重要组件的名字
            if (componentNames.Contains("UnityEngine.Camera"))
                return "Camera";
            if (componentNames.Contains("UnityEngine.Rendering.PostProcessing.PostProcessVolume"))
                return "PostProcessing";
            if (componentNames.Contains("UnityEngine.Light"))
            {
                if (gameObject.GetComponent<Light>().type == LightType.Point)
                    return "Light";
                if (gameObject.GetComponent<Light>().type == LightType.Spot)
                    return "SpotLight";
                if (gameObject.GetComponent<Light>().type == LightType.Directional)
                    return "DirectionalLight";
                if (gameObject.GetComponent<Light>().type == LightType.Rectangle)
                    return "AreaLight";
            }

            if (componentNames.Contains("UnityEngine.Animator"))
                return "Animator";
            if (componentNames.Contains("UnityEngine.ParticleSystem"))
                return "ParticleSystem";
            if (componentNames.Contains("UnityEngine.MeshFilter"))
                return "MeshFilter";
            if (componentNames.Contains("UnityEngine.Playables.PlayableDirector"))
                return "PlayableDirector";
            if (componentNames.Contains("UnityEngine.Canvas"))
                return "Canvas";
            if (componentNames.Contains("Volume"))
                return "Volume";
            if (componentNames.Contains("UnityEngine.UI.Text") || componentNames.Contains("TMPro.TextMeshProUGUI"))
                return "Text";
            if (componentNames.Where(s => !s.StartsWith("UnityEngine.")).Count() > 0)
                return "cs Script";
            return "GameObject";
        }

        #region 载入快捷键
        // typeof(SceneView) 表示这个快捷键只在SceneView中有效

        [Shortcut("PH/SceneViewBookmark/LoadObject1",typeof(SceneView), KeyCode.Alpha1, ShortcutModifiers.None)]
        private static void LoadObject1()
        {
            if (!PHSceneOverlay.instance.isDisplayed) return;
            Selection.activeGameObject = GameObject.Find(BookmarkAssetSO.Instance.GetSceneBookmarkGroup(SceneManager.GetActiveScene().path).gameObjectPaths[0].text);
        }

        [Shortcut("PH/SceneViewBookmark/LoadObject2",typeof(SceneView), KeyCode.Alpha2, ShortcutModifiers.None)]
        private static void LoadObject2()
        {
            if (!PHSceneOverlay.instance.isDisplayed) return;
            Selection.activeGameObject = GameObject.Find(BookmarkAssetSO.Instance.GetSceneBookmarkGroup(SceneManager.GetActiveScene().path).gameObjectPaths[1].text);
        }

        [Shortcut("PH/SceneViewBookmark/LoadObject3",typeof(SceneView), KeyCode.Alpha3, ShortcutModifiers.None)]
        private static void LoadObject3()
        {
            if (!PHSceneOverlay.instance.isDisplayed) return;
            Selection.activeGameObject = GameObject.Find(BookmarkAssetSO.Instance.GetSceneBookmarkGroup(SceneManager.GetActiveScene().path).gameObjectPaths[2].text);
        }

        [Shortcut("PH/SceneViewBookmark/LoadObject4",typeof(SceneView), KeyCode.Alpha4, ShortcutModifiers.None)]
        private static void LoadObject4()
        {
            if (!PHSceneOverlay.instance.isDisplayed) return;
            Selection.activeGameObject = GameObject.Find(BookmarkAssetSO.Instance.GetSceneBookmarkGroup(SceneManager.GetActiveScene().path).gameObjectPaths[3].text);
        }

        [Shortcut("PH/SceneViewBookmark/LoadObject5",typeof(SceneView), KeyCode.Alpha5, ShortcutModifiers.None)]
        private static void LoadObject5()
        {
            if (!PHSceneOverlay.instance.isDisplayed) return;
            Selection.activeGameObject = GameObject.Find(BookmarkAssetSO.Instance.GetSceneBookmarkGroup(SceneManager.GetActiveScene().path).gameObjectPaths[4].text);
        }
        [Shortcut("PH/SceneViewBookmark/LoadObject6",typeof(SceneView), KeyCode.Alpha6, ShortcutModifiers.None)]
        private static void LoadObject6()
        {
            if (!PHSceneOverlay.instance.isDisplayed) return;
            Selection.activeGameObject = GameObject.Find(BookmarkAssetSO.Instance.GetSceneBookmarkGroup(SceneManager.GetActiveScene().path).gameObjectPaths[5].text);
        }
        #endregion
    }
}