#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 挂在camera上，能够将相机transform对齐到场景视图相机
/// 测试的时候，做效果的时候对比查看起来挺方便。
///
/// 旧GrabEditorCamMatrix类
/// 仅用于编辑模式
/// 
/// </summary>
[ExecuteAlways]
public class SyncCamTransform : MonoBehaviour
{
    [Tooltip("false: 游戏相机对齐到场景相机，true: 场景相机对齐到游戏相机，一般用于预览timeline。")]
    public bool invert = false;
    public bool enableInRuntime = false;
    
    
    private Camera renderingCamera;
    
    void Update ()
    {
        if ((Application.isPlaying && !enableInRuntime) || UnityEditor.SceneView.lastActiveSceneView == null) return;

#if UNITY_EDITOR
        Camera temp = UnityEditor.SceneView.lastActiveSceneView.camera;

        if (temp.name.Equals("SceneCamera"))
            renderingCamera = temp;
        else
            renderingCamera = null;

        if (renderingCamera != null)
        {
            if (invert)
            {
                SceneView.lastActiveSceneView.pivot = transform.position;
                SceneView.lastActiveSceneView.rotation = transform.rotation;
            }
            else
            {
                transform.position = renderingCamera.transform.position;
                transform.rotation = renderingCamera.transform.rotation;
            }
        }
#endif
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += OnEditorUpdate;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= OnEditorUpdate;
#endif
    }

#if UNITY_EDITOR
    protected virtual void OnEditorUpdate()
    {
        Update();
    }
#endif
}
#endif

