using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PluginHub.Helper;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginHub.Module
{
    public class CommonComponentModule : PluginHubModuleBase
    {
        public override ModuleType moduleType => ModuleType.Shortcut;
        public override string moduleName
        {
            get { return "频繁使用"; }
        }
        // private float littleBarWidth;

        public override string moduleDescription => "快速选中常用游戏对象.便于美术师、程序员快速定位";

        public static Vector3 SceneViewCameraPosition()
        {
            if (SceneView.lastActiveSceneView == null)
                return Vector3.zero;
            return SceneView.lastActiveSceneView.camera.transform.position;
        }

        private void DrawLittleBar(string title, string icon, GameObject[] drawObj = null)
        {
            //GUILayout.BeginVertical(GUILayout.Width(littleBarWidth));//GUILayout.Width((this.position.width - 20) / 3f)
            GUILayout.BeginVertical();
            {
                // GUIStyle labelStyle = new GUIStyle("Label");
                // labelStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(title, GUILayout.Height(48), GUILayout.MaxWidth(105), GUILayout.MinWidth(30));
                    // GUILayout.Label(title, GUILayout.Height(48),GUILayout.Width(100));
                    GUILayout.FlexibleSpace();
                    GUI.enabled = drawObj != null && drawObj.Length > 0;
                    if (GUILayout.Button(EditorGUIUtility.IconContent(icon), GUILayout.Width(48), GUILayout.Height(48)))
                    {
                        //这个按钮是默认选中第一个
                        if (drawObj != null && drawObj.Length > 0)
                        {
                            SelectObjectAndShowInspector(drawObj[0]);
                        }
                    }

                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                DrawObjectArray(drawObj);
            }
            GUILayout.EndVertical();
        }

        public void DrawObjectArray(GameObject[] objects2Draw)
        {
            for (int i = 0; i < objects2Draw.Length; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(objects2Draw[i], typeof(GameObject), false, GUILayout.ExpandWidth(true),
                    GUILayout.MinWidth(30));
                if (GUILayout.Button("Select", GUILayout.Width(48)))
                {
                    SelectObjectAndShowInspector(objects2Draw[i]);
                }

                GUILayout.EndHorizontal();
            }

            //画一个假的 用于对齐
            if (objects2Draw.Length == 0)
            {
                GUI.enabled = false;
                GUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(null, typeof(GameObject), false, GUILayout.ExpandWidth(true));
                GUILayout.Button("Select", GUILayout.Width(48));
                GUILayout.EndHorizontal();
                GUI.enabled = true;
            }


        }

        //绘制常用快捷组件
        protected override void DrawGuiContent()
        {
            // littleBarWidth = (editorWindow.position.width - 38f) / 3f;
            if (moduleDebug)
            {
                // GUILayout.Label($"littleBarWidth:{littleBarWidth}");
                GUILayout.Label($"position rect:{PluginHubWindow.Window.position}");
            }

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUI.enabled = Selection.gameObjects != null && Selection.gameObjects.Length > 0;
                    if (GUILayout.Button("转到选中物体", GUILayout.ExpandWidth(false)))
                    {
                        PluginHubFunc.RotateSceneViewCameraToTarget(Selection.gameObjects[0].transform);
                    }

                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                //附近的东西
                GUILayout.Label(PluginHubFunc.GuiContent("Things Nearby：", "这里显示场景相机附近的常用游戏对象，用于快速选择"));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GameObject[] goToShow = TryFindLocalVolume();
                    DrawLittleBar("Local Volume", "LightProbeProxyVolume Gizmo", goToShow);

                    GUILayout.FlexibleSpace();

                    goToShow = TryFindReflectionProbe();
                    DrawLittleBar("Reflection Probe", "d_ReflectionProbeSelector@2x", goToShow);

                    GUILayout.FlexibleSpace();

                    goToShow = TryFindNearbyLight();
                    DrawLittleBar("Light", "d_Light Icon", goToShow);
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("Other GameObject：");
                GUILayout.BeginHorizontal();
                {
                    GameObject[] goToShow = TryFindGlobalVolume();
                    DrawLittleBar("Global Volume", "d_ToolHandleGlobal@2x", goToShow);

                    GUILayout.FlexibleSpace();

                    goToShow = TryFind<LightProbeGroup>();
                    //用距离场景相机的距离排序
                    goToShow = goToShow.OrderBy(x =>
                        (x.transform.position - SceneViewCameraPosition()).magnitude).ToArray();
                    DrawLittleBar("Light Probe Group", "d_LightProbeGroup Icon", goToShow);

                    GUILayout.FlexibleSpace();

                    goToShow = TryFindDirectionalLight();
                    DrawLittleBar("Directional Light", "DirectionalLight Gizmo", goToShow);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GameObject[] goToShow = TryFind<Canvas>();
                    DrawLittleBar("Canvas", "d_Canvas Icon", goToShow);

                    GUILayout.FlexibleSpace();

                    goToShow = TryFindMainScripts();
                    DrawLittleBar("Main Script", "cs Script Icon", goToShow);

                    GUILayout.FlexibleSpace();

                    goToShow = TryFind<Camera>();
                    DrawLittleBar("Camera", "Camera Gizmo", goToShow);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        //选择对象然后自动跳转到检视面板
        private static void SelectObjectAndShowInspector(Object obj)
        {
            Selection.objects = new[] { obj };
            //open unity Inspector editor window
            EditorWindow.GetWindow(Type.GetType("UnityEditor.InspectorWindow,UnityEditor")).Show();
            // if(!EditorApplication.ExecuteMenuItem("Window/Panels/6 Inspector"))
            //     EditorApplication.ExecuteMenuItem("Window/Panels/7 Inspector");
        }


        #region TryFind

        //通用查找方法，参数为可选的过滤条件
        public static GameObject[] TryFind<T>(Func<T, bool> predicate = null) where T : Component
        {
            Component[] components = GameObject.FindObjectsOfType<T>();
            if (predicate != null)
                components = components.Where((component) => predicate.Invoke(component as T)).ToArray();
            return components.Select((o) => o.gameObject).ToArray();
        }

        //尝试寻找场景视图相机当前处于那些HDRP local volume对象内
        public static GameObject[] TryFindLocalVolume()
        {
            BoxCollider[] boxColliders = GameObject.FindObjectsOfType<BoxCollider>();
            //过滤出Volume的boxcollider
            boxColliders = boxColliders.Where((box) =>
            {
                //这个BoxCollider游戏对象上的所有组件
                Component[] components = box.gameObject.GetComponents<Component>();
                bool isVolumeObj = false;
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    //脚本missing，component会为空
                    if (component != null && component.GetType().Name.Contains("Volume"))
                    {
                        isVolumeObj = true;
                        break;
                    }
                }

                return isVolumeObj;
            }).ToArray();
            //过滤出包含场景视图相机在内部的
            boxColliders = boxColliders.Where((box) => box.bounds.Contains(SceneViewCameraPosition()))
                .ToArray();
            //按大小排序,范围小的优先
            boxColliders = boxColliders.OrderBy((box) => box.bounds.size.x + box.bounds.size.y + box.bounds.size.z)
                .ToArray();
            return boxColliders.Select((o) => o.gameObject).ToArray();
        }

        //尝试寻找全局Volume
        //可以优化
        public static GameObject[] TryFindGlobalVolume()
        {
            Component[] components = GameObject.FindObjectsOfType<Component>();
            components = components.Where((c) =>
            {
                //过滤名字里面有Volume
                bool nameHasVolume = c.name.Contains("Volume"); //游戏对象名字有volume 直接就是了
                if (nameHasVolume) return true;
                return c.GetType().Name.Contains("Volume"); //再看脚本名字
            }).ToArray();
            components = components.Where((c) =>
            {
                Type componentType = c.GetType();
                //注意URP12版本以后isGlobal从字段变成了属性，所以这里要进行多次判断
                FieldInfo fieldInfo = componentType.GetField("isGlobal");
                PropertyInfo propertyInfo = componentType.GetProperty("isGlobal");
                if (fieldInfo == null && propertyInfo == null) //没有IsGlobal字段也没有属性
                    return false;
                bool value = false;
                if (fieldInfo != null)
                    value = Convert.ToBoolean(fieldInfo.GetValue(c));
                if (propertyInfo != null)
                    value = Convert.ToBoolean(propertyInfo.GetValue(c));
                if (!value) //IsGlobal 为假，不是全局的Volume
                    return false;
                return true;
            }).ToArray();
            return components.Select((o) => o.gameObject).ToArray();
        }

        public static GameObject[] TryFindDirectionalLight()
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            lights = lights.Where((light) => light.type == LightType.Directional).ToArray();
            return lights.Select((o) => o.gameObject).ToArray();
        }

        public static GameObject[] TryFindMainScripts()
        {
            Component[] components = GameObject.FindObjectsOfType<Component>();
            components = components.Where((c) =>
            {
                // string name = c.GetType().Name;//避免使用反射
                string name = c.name;
                return name.Contains("MainControl") || name.Contains("MainManager");
            }).ToArray();
            return components.Select((o) => o.gameObject).ToArray();
        }

        //尝试寻找场景视图相机当前处于那些反射探头内
        public static GameObject[] TryFindReflectionProbe()
        {
            ReflectionProbe[] reflectionProbes = GameObject.FindObjectsOfType<ReflectionProbe>();
            //在范围内
            reflectionProbes = reflectionProbes
                .Where((rp) => rp.bounds.Contains(SceneViewCameraPosition())).ToArray();
            return reflectionProbes.Select((o) => o.gameObject).ToArray();
        }

        //寻找附近的灯光组件
        public static GameObject[] TryFindNearbyLight()
        {
            Light[] lightObjs = GameObject.FindObjectsOfType<Light>();
            lightObjs = lightObjs.Where((lightObj) => !HasParentLight(lightObj.transform)).ToArray(); //它爸爸也是灯组件，忽略掉它
            //按距离排序
            lightObjs = lightObjs.OrderBy((lightObj) =>
                    Vector3.SqrMagnitude(lightObj.transform.position - SceneViewCameraPosition()))
                .ToArray();
            lightObjs = lightObjs.Take(5).ToArray();
            //排序：在视野内的优先
            lightObjs = lightObjs.OrderBy((lightObj) =>
            {
                Camera camera = SceneView.lastActiveSceneView.camera;
                Vector3 viewportPoint = camera.WorldToViewportPoint(lightObj.transform.position);
                bool inView = (viewportPoint.z > 0 && (new Rect(0, 0, 1, 1)).Contains(viewportPoint));

                return !inView;
            }).ToArray();
            lightObjs = lightObjs.Take(3).ToArray();

            return lightObjs.Select((o) => o.gameObject).ToArray();
        }

        static bool HasParentLight(Transform transform)
        {
            if (transform.parent == null)
                return false;
            return transform.parent.GetComponent<Light>() != null;
        }

        #endregion

    }
}