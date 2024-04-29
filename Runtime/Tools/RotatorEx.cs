using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实现基本的持续旋转功能
/// 此类可以在编辑模式持续执行基本旋转功能，关键代码在OnDrawGizmos里
/// </summary>
[ExecuteInEditMode]
public class RotatorEx : MonoBehaviour
{
    public bool isRunInEditMode = false;
    public Space rotateSpace = Space.World;
    public Vector3 rotateAxis = new Vector3(0,1,0);
    public float rotateSpeed = 30;
    
    // Update is called once per frame
    void Update()
    {
        if ((!Application.isPlaying && isRunInEditMode) || Application.isPlaying)
        {
            //transform.Rotate(0, rotateSpeed * Time.deltaTime, 0,rotateSpace);
            transform.Rotate(rotateAxis,rotateSpeed * Time.deltaTime,rotateSpace);
        }
    }
    void OnDrawGizmos()
    {
        // Your gizmo drawing thing goes here if required...
#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying && isRunInEditMode)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }
}
