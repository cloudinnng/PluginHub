using System.Collections;
using System.Collections.Generic;
using PluginHub.Editor;
using PluginHub.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

namespace PluginHub.Editor
{
    public class TestModule : PluginHubModuleBase
    {
        public override string moduleDescription { get; } = "测试";
        protected override void DrawGuiContent()
        {
            DrawSplitLine("RayIntersectsTriangle");
            {
                v0.x = EditorGUILayout.FloatField("v0.x", v0.x);
                v0.y = EditorGUILayout.FloatField("v0.y", v0.y);
                v0.z = EditorGUILayout.FloatField("v0.z", v0.z);
                v1.x = EditorGUILayout.FloatField("v1.x", v1.x);
                v1.y = EditorGUILayout.FloatField("v1.y", v1.y);
                v1.z = EditorGUILayout.FloatField("v1.z", v1.z);
                v2.x = EditorGUILayout.FloatField("v2.x", v2.x);
                v2.y = EditorGUILayout.FloatField("v2.y", v2.y);
                v2.z = EditorGUILayout.FloatField("v2.z", v2.z);

                if (GUILayout.Button("Test"))
                {
                    v0 = Random.insideUnitSphere * 9999;
                    v1 = Random.insideUnitSphere * 9999;
                    v2 = Random.insideUnitSphere * 9999;
                    // Ray ray = SceneViewMouseRay();
                    Ray ray = new Ray(new Vector3(0,0,-5),Vector3.forward);
                    DebugEx.DebugRay(ray,9999,Color.red,30f);
                    PerformanceTest.Start();
                    // bool r = RaycastWithoutCollider.RaycastTriangle(ray.origin, ray.direction, v0, v1, v2,
                    //     out Vector3 hitPoint, out Vector3 normal, true);
                    PerformanceTest.End("Ray Intersects Triangle");
                    // Debug.Log(r);
                }
            }

            DrawSplitLine("SceneView");
            {
                SceneView.lastActiveSceneView.pivot = EditorGUILayout.Vector3Field("SceneView.lastActiveSceneView.pivot", SceneView.lastActiveSceneView.pivot);
                SceneView.lastActiveSceneView.size = EditorGUILayout.FloatField("SceneView.lastActiveSceneView.size", SceneView.lastActiveSceneView.size);
                SceneView.lastActiveSceneView.camera.transform.position = EditorGUILayout.Vector3Field("SceneView.lastActiveSceneView.camera.transform.position", SceneView.lastActiveSceneView.camera.transform.position);
                GUILayout.Label("distance form camera to pivot: " + Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, SceneView.lastActiveSceneView.pivot));
                GUILayout.Label($"SceneView.lastActiveSceneView.position: {SceneView.lastActiveSceneView.position}");
                GUILayout.Label($"SceneView.lastActiveSceneView.rotation: {SceneView.lastActiveSceneView.rotation}");
                GUILayout.Label($"SceneView.lastActiveSceneView.eulerAngles: {SceneView.lastActiveSceneView.rotation.eulerAngles}");
            }

            DrawSplitLine("SceneViewContextMenu");
            {
                GUILayout.Label($"SceneViewContextMenu.mouseCurrPosition: {PHSceneContextMenu.mouseCurrPosition}");
                GUILayout.Label($"{PHSceneContextMenu.mouseCurrPosition.x / SceneView.lastActiveSceneView.position.width},{1 - PHSceneContextMenu.mouseCurrPosition.y / SceneView.lastActiveSceneView.position.height}");

            }
            // DrawRow("Triangle in scene", "");
        }

        protected override bool OnSceneGUI(SceneView sceneView)
        {
            Handles.color = Color.red;
            Handles.DrawLine(v0, v1);
            Handles.DrawLine(v1, v2);
            Handles.DrawLine(v2, v0);

            return true;
        }

        private Vector3 v0 = new Vector3(1, 1, 0);
        private Vector3 v1 = new Vector3(1, -1, 0);
        private Vector3 v2 = new Vector3(-1, 0, 0);


    }
}