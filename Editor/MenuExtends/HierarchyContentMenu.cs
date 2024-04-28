using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginHub.MenuExtend
{
    public static class HierarchyContentMenu
    {
        [MenuItem("GameObject/PH_SelectSameNameSilbing", false, 0)]
        private static void SelectSameNameSilbing()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("No GameObject Selected");
                return;
            }
            //
            Transform parent = Selection.activeGameObject.transform.parent;
            string name = Selection.activeGameObject.name;
            if (parent == null)
            {
                Debug.LogWarning("No Parent");
                return;
            }
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    gameObjects.Add(child.gameObject);
            }
            // Select
            Selection.objects = gameObjects.ToArray();
        }

    }
}