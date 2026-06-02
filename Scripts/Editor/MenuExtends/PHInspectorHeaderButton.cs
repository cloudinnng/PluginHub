#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginHub.Editor
{
    /// <summary>
    /// 在组件 Inspector 标题栏右上角注入按钮：用组件类型名重命名 GameObject。
    /// 原理：向 EditorGUIUtility.s_EditorHeaderItemsMethods 注入 HeaderItemDelegate。
    /// </summary>
    [InitializeOnLoad]
    public static class PHInspectorHeaderButton
    {
        const BindingFlags BF = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>64×64 RGBA PNG，灰线稿 + 透明底（组件块 → 箭头 → 立方体）。</summary>
        const string RenameIconPngHex =
            "89-50-4E-47-0D-0A-1A-0A-00-00-00-0D-49-48-44-52-00-00-00-40-00-00-00-40-08-06-00-00-00-AA-69-71-DE-00-00-00-D5-49-44-41-54-78-DA-ED-D9-41-0A-84-30-0C-40-D1-DC-FF-64-5D-77-DD-BB-28-2E-0A-0A-22-6A-93-98-D8-FF-A1-9B-41-66-EC-43-3A-5A-A5-B5-B6-08-11-11-11-11-11-11-11-11-1D-DB-36-4D-B4-07-00-00-24-05-88-F0-3D-00-00-00-00-00-00-00-F0-21-C0-DB-FF-7D-00-4E-00-4A-29-CB-F4-00-E1-11-2C-D7-80-0E-10-1A-C1-02-A0-8F-3D-C0-36-EE-FC-8E-FB-33-88-27-40-47-B0-7A-30-73-5F-03-34-AF-80-CF-9E-42-2D-00-9E-AE-01-4F-27-93-0E-C0-63-F2-A1-D6-80-FE-59-AD-D5-6D-F2-21-01-AC-77-A2-54-CE-3F-2B-80-DA-F9-67-04-50-3D-FF-08-00-23-C7-03-00-00-00-F3-02-A8-DF-07-78-BE-19-1A-3D-DE-E4-46-28-0B-80-EA-EB-B8-3F-00-48-C6-B4-00-24-6B-D3-BD-85-0E-B1-FB-F3-17-00-21-22-22-22-22-22-BA-68-05-A7-6A-35-59-8B-E0-87-59-00-00-00-00-49-45-4E-44-AE-42-60-82";

        static Delegate s_HeaderButton;
        static Texture2D s_Icon;
        static GUIContent s_Content;

        static PHInspectorHeaderButton()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += RegisterOnce;
        }

        static void RegisterOnce(UnityEditor.Editor editor)
        {
            var list = GetHeaderButtonList();
            if (list == null) return;

            var delegateType = typeof(EditorGUIUtility).GetNestedType("HeaderItemDelegate", BF);
            if (delegateType == null) return;

            s_HeaderButton ??= Delegate.CreateDelegate(
                delegateType,
                typeof(PHInspectorHeaderButton).GetMethod(nameof(DrawRenameButton), BF));

            foreach (var item in list.Cast<object>().ToList())
                if (ReferenceEquals(item, s_HeaderButton)) return;

            list.Insert(0, s_HeaderButton);
            Debug.Log("[PHInspectorHeaderButton] 已注册「组件名命名」按钮");

            UnityEditor.Editor.finishedDefaultHeaderGUI -= RegisterOnce;
        }

        static IList GetHeaderButtonList()
        {
            var field = typeof(EditorGUIUtility).GetField("s_EditorHeaderItemsMethods", BF);
            return field?.GetValue(null) as IList;
        }

        static Texture2D GetIcon()
        {
            if (s_Icon != null) return s_Icon;

            var pngBytes = RenameIconPngHex.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();
            s_Icon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            s_Icon.LoadImage(pngBytes);
            s_Icon.hideFlags = HideFlags.HideAndDontSave;
            return s_Icon;
        }

        static GUIContent GetContent()
        {
            s_Content ??= new GUIContent(GetIcon(), "使用组件名命名 GameObject");
            return s_Content;
        }

        // 签名须与 HeaderItemDelegate 一致：bool(Rect, Object[])
        static bool DrawRenameButton(Rect rect, Object[] targets)
        {
            if (targets == null || targets.Length != 1 || targets[0] is not Component component)
                return false;

            var iconSize = Mathf.Min(rect.width, rect.height, 16f);
            var iconRect = new Rect(
                rect.x + (rect.width - iconSize) * 0.5f,
                rect.y + (rect.height - iconSize) * 0.5f,
                iconSize,
                iconSize);

            // 悬停提示
            GUI.Label(rect, new GUIContent("", "使用组件名命名 GameObject"));

            var tint = EditorGUIUtility.isProSkin
                ? new Color(0.78f, 0.78f, 0.78f)
                : new Color(0.49f, 0.49f, 0.49f);
            if (rect.Contains(Event.current.mousePosition))
                tint = EditorGUIUtility.isProSkin ? Color.white : new Color(0.2f, 0.2f, 0.2f);

            var prevColor = GUI.color;
            GUI.color = tint;
            if (GUI.Button(iconRect, GetContent(), GUIStyle.none))
            {
                var oldName = component.gameObject.name;
                component.gameObject.name = component.GetType().Name;
                EditorUtility.SetDirty(component.gameObject);
                Debug.Log($"[PHInspectorHeaderButton] 重命名: \"{oldName}\" → \"{component.gameObject.name}\"");
            }

            GUI.color = prevColor;
            return true;
        }
    }
}
#endif
