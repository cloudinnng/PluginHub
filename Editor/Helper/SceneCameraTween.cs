using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Editor.Helper
{
    // 定义一个场景相机的补间动画,让场景相机以动画形式移动到指定的位置
    // SceneView.pivot是Alt+左键拖拽的旋转中心
    // SceneView.size 这个API直接控制场景视图相机到pivot的距离,
    // 当场景相机fov是60时, 若size=10, 则相机到pivot的距离是20,也就是两倍的关系
    public class SceneCameraTween
    {
        private const double TWEEN_DURATION = 0.2;
        private Vector3 targetPosition;
        private float targetSize;
        private Quaternion targetRotation;

        private Vector3 initPosition;
        private float initSize;
        private double startTime;
        private SceneView sceneView;

        private bool Complete;


        // 可以直接调用这个方法,让场景相机移动到指定位置
        public static void GoTo(Vector3 targetPosition,float targetSize,Quaternion targetRotation)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            SceneCameraTween tween = new SceneCameraTween(targetPosition, targetSize, targetRotation);
            tween.PlayTween(sceneView);
        }

        public SceneCameraTween(Vector3 targetPosition,float targetSize,Quaternion targetRotation)
        {
            this.targetPosition = targetPosition;
            this.targetSize = targetSize;
            this.targetRotation = targetRotation;
        }

        public void PlayTween(SceneView sceneView)
        {
            // 初始化
            startTime = EditorApplication.timeSinceStartup;
            this.sceneView = sceneView;
            initPosition = sceneView.pivot;
            initSize = sceneView.size;
            Complete = false;

            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private void Update()
        {
            if (!Complete)
            {
                float progress = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - startTime) / TWEEN_DURATION));

                sceneView.pivot = Vector3.Lerp(initPosition, targetPosition, progress);
                sceneView.size = Mathf.Lerp(initSize, targetSize, progress);
                sceneView.rotation = Quaternion.Slerp(sceneView.rotation, targetRotation, progress);

                if (progress >= 1)
                {
                    Complete = true;
                    EditorApplication.update -= Update;
                }
            }
        }
    }
}