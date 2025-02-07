using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    public class BookmarkSettings
    {
        internal const string ASSET_DIR = "Assets/Plugins/PHSceneViewBookmarkGenerated";
        internal const string BOOKMARKS_ASSET_PATH = ASSET_DIR + "/BookmarkCollection.asset";
        internal const int BOOKMARK_COUNT = 6;

        internal static Color COLOR_BOOKMARK_BUTTON_NORMAL = new Color(0.588f, 0.686f, 1.0f , 1.0f);
        internal static Color COLOR_BOOKMARK_BUTTON_ACTIVE = new Color(1f, 0.6f, 0.80f, 1.0f);
        internal static Color COLOR_BOOKMARK_BUTTON_EMPTY = new Color(0.588f, 0.686f, 1.0f, 0.5f);

        internal static Vector2Int BUTTON_SIZE = new Vector2Int(30, 30);

    }
}