using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor
{
    public static class ViewTweenInitializeOnLoad
    {
        private static List<ViewTween> activeTweens = new List<ViewTween>();
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            // Process any active tweens.
            if (activeTweens != null)
            {
                for (int i = 0; i < activeTweens.Count; i++)
                {
                    activeTweens[i].Update();
                    if (activeTweens[i].Complete)
                    {
                        activeTweens.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        
        internal static void GotoCameraBookmark(CameraBookmark bookmark, SceneView sceneView)
        {
            activeTweens.Add(new ViewTween(bookmark, sceneView));
        }

        internal static void GotoCamera(Camera camera, SceneView sceneView)
        {
            activeTweens.Add(new ViewTween(camera, sceneView));
        }
    }
}