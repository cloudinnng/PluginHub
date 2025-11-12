using UnityEngine;
using UnityEditor;

namespace PluginHub.Editor
{
    /// <summary>
    /// Utility class for tweening from current camera within a scene view to a given bookmark.
    /// This allows the tool to provide a smooth tween to bookmarks instead of a snap (if tweens enabled).
    /// 用于从场景视图内的当前摄像机渐变到给定书签的实用程序类。
    /// 这允许该工具为书签提供平滑的补间，而不是快照(如果启用了补间)。
    ///
    /// 处理跳转到相机书签时的补间动画的工具类
    /// </summary>
    internal class ViewTween
    {
        private const double TWEEN_DURATION = 0.15;

        private Vector3 initPosition;
        private Quaternion initRotation;
        private float initSize;
        private CameraBookmark target;
        private double startTime;
        private SceneView view;

        //指示该补间是否已完成。
        public bool Complete { get; private set; }

        /// <summary>
        /// Create a new tween object using a given sceneview and a target bookmark.
        /// </summary>
        /// <param name="targetBookmark">Bookmark you wish to assume.</param>
        /// <param name="sceneView">Sceneview that you wish to apply theis bookmark to.</param>
        public ViewTween(CameraBookmark targetBookmark, SceneView sceneView)
        {
            if (targetBookmark == null || sceneView == null)
                return;

            Complete = false;

            view = sceneView;
            target = targetBookmark;

            initPosition = sceneView.pivot;
            initRotation = sceneView.rotation;
            initSize = sceneView.size;

            view.in2DMode = target.is2DMode;
            view.orthographic = target.Orthographic;

            startTime = EditorApplication.timeSinceStartup;
        }

        // 使用此方法让场景相机移动到指定Camera位置
        public ViewTween(Camera camera, SceneView sceneView)
        {
            if (camera == null || sceneView == null)
                return;

            Complete = false;

            view = sceneView;
            target = new CameraBookmark()
            {
                is2DMode = camera.orthographic,
                Orthographic = camera.orthographic,
                pivot = camera.transform.position,
                rotation = camera.transform.rotation,
                size = 0.01f
            };

            initPosition = sceneView.pivot;
            initRotation = sceneView.rotation;
            initSize = sceneView.size;

            view.in2DMode = target.is2DMode;
            view.orthographic = target.Orthographic;

            startTime = EditorApplication.timeSinceStartup;

        }
        
        // 使用此方法让场景相机移动到指定Transform位置
        public ViewTween(Transform targetTransform, SceneView sceneView)
        {
            if (targetTransform == null || sceneView == null)
                return;

            Complete = false;

            view = sceneView;
            target = new CameraBookmark()
            {
                is2DMode = false,
                Orthographic = sceneView.orthographic,
                pivot = targetTransform.position,
                rotation = targetTransform.rotation,
                size = 0.01f
            };

            initPosition = sceneView.pivot;
            initRotation = sceneView.rotation;
            initSize = sceneView.size;

            view.in2DMode = target.is2DMode;
            view.orthographic = target.Orthographic;

            startTime = EditorApplication.timeSinceStartup;

        }

        /// <summary>
        /// Updates the tween, lerping between the previous camera state, and the desired bookmark.
        /// </summary>
        public void Update()
        {
            if (!Complete) // Just in case...for sanity.
            {
                if (view != null)
                {
                    float progress =
                        Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - startTime) / TWEEN_DURATION));

                    view.pivot = Vector3.Lerp(initPosition, target.pivot, progress);
                    if(!target.is2DMode)
                        view.rotation = Quaternion.Lerp(initRotation, target.rotation, progress);
                    view.size = Mathf.Lerp(initSize, target.size, progress);

                    if (progress >= 1)
                    {
                        Complete = true;
                    }
                }
            }
        }
    }
}