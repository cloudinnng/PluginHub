using System;
using System.Collections;
using System.Collections.Generic;
using PluginHub.Runtime.Extends;
using UnityEngine;
using UnityEngine.Animations;

//地图风格的相机移动方式
//左键拖动，中键缩放/旋转，右键旋转
[RequireComponent(typeof(Camera))]
public class MapStyleCameraMovement : MonoBehaviour
{
    private class CameraState
    {
        public float yaw;
        public float pitch;
        public float roll;
        public float x;
        public float y;
        public float z;
        
        //用transform设置相机状态
        public void SetFromTransform(Transform t)
        {
            pitch = t.eulerAngles.x;
            yaw = t.eulerAngles.y;
            roll = t.eulerAngles.z;
            x = t.position.x;
            y = t.position.y;
            z = t.position.z;
        }

        public void Translate(Vector3 translation)
        {
            Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;
            x += rotatedTranslation.x;
            y += rotatedTranslation.y;
            z += rotatedTranslation.z;
        }

        //朝向目标去插值
        public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
        {
            yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
            pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
            roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

            x = Mathf.Lerp(x, target.x, positionLerpPct);
            y = Mathf.Lerp(y, target.y, positionLerpPct);
            z = Mathf.Lerp(z, target.z, positionLerpPct);
        }

        //将相机的位置和朝向设置到transform
        public void UpdateTransform(Transform t)
        {
            t.eulerAngles = new Vector3(pitch, yaw, roll);
            t.position = new Vector3(x, y, z);
        }
    }

    //parameters set in the inspector
    public KeyCode temporarilyDisabledKey = KeyCode.None;//临时禁用键
    public float Sensitivity = 1;//公用的灵敏度
    public float DragSensitivity = 0.03f;
    public float ZoomSensitivity = 1.5f;
    public float RotateSensitivity = 3;
    public float RotateAroundSensitivity = 7;
    
    //runtime data
    private CameraState cameraState = new CameraState();
    private float zoomRemaingDelta;
    private GameObject cameraRotateAxisTmp=null;
    private Transform oldParent;
    private float distanceSave;
    private Plane dragPlane;//TODO  未实现功能
    private Vector2 mousePosition;
    private Vector2 lastMousePosition;
    
    private void OnEnable()
    {
        cameraState.SetFromTransform(transform);
    }

    //应用程序这一帧是否刚获得焦点，避免拖拽bug
    private bool justFocusThisFrame = false;
    private IEnumerator OnApplicationFocus(bool hasFocus)
    {
        justFocusThisFrame = true;
        yield return null;
        justFocusThisFrame = false;
    }

    private void Update()
    {
        if(temporarilyDisabledKey!= KeyCode.None)
            if(Input.GetKey(temporarilyDisabledKey))
                return;
        
        //应用控制键的灵敏度调整
        float controlKeyMultiplier = 1;
        if (Input.GetKey(KeyCode.LeftControl))
        {
            controlKeyMultiplier = 0.2f;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            controlKeyMultiplier = 5f;
        }
        //使用的公共灵敏度
        float commonSensitivityUse = Sensitivity * controlKeyMultiplier;

        //提供公共参数给操作使用
        mousePosition = Input.mousePosition;
        // print(mousePosition + " " + lastMousePosition);
        Vector2 mouseScreenPositionDelta = this.mousePosition - lastMousePosition;
        if(justFocusThisFrame)
            mouseScreenPositionDelta = Vector2.zero;
        // Vector2 mouseWorldDelta;
        
        
        //鼠标左键平移相机------------------------------------------------------------------------------
        if (Input.GetMouseButtonDown(0))//
        {
            Vector3 mouseHitPoint = MouseHitPoint();
            if (mouseHitPoint != Vector3.zero)
            {
                distanceSave = Vector3.Distance(mouseHitPoint, transform.position);
                dragPlane = new Plane(transform.forward.normalized, mouseHitPoint);
            }
        }
        if (Input.GetMouseButton(0))
        {
            //鼠标移动的像素距离
            // Vector2 mouseMovement = new Vector2(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            Vector2 dragDelta = DragSensitivity * commonSensitivityUse * distanceSave * -mouseScreenPositionDelta;
            cameraState.Translate(dragDelta);
        }
        
        //鼠标滚轮前进后退------------------------------------------------------------------------------
        Vector2 mouseScrollDelta = Input.mouseScrollDelta;
        if (!Mathf.Approximately(mouseScrollDelta.y, 0) && new Rect(0,0,Screen.width,Screen.height).Contains(mousePosition))//有滚动鼠标滚轮
        {
            //缩放速度
            float scaleSpeedUse = ZoomSensitivity * commonSensitivityUse;

            zoomRemaingDelta += mouseScrollDelta.y * scaleSpeedUse;
        }

        if (Mathf.Abs(zoomRemaingDelta) > 0)
        {
            Vector3 scalePos = MouseHitPoint();
            if (scalePos == Vector3.zero)
                scalePos = transform.position + transform.forward * 10;
            
            Vector3 scaleVec = (scalePos - transform.position).normalized;//缩放的方向向量

            float newRemaingDelta = Mathf.Lerp(zoomRemaingDelta, 0, 20f* Time.deltaTime);

            Vector3 positionDelta =(zoomRemaingDelta - newRemaingDelta) * scaleVec;
            cameraState.x+= positionDelta.x;
            cameraState.y+= positionDelta.y;
            cameraState.z+= positionDelta.z;
            
            zoomRemaingDelta = newRemaingDelta;
        }
        
        
        //鼠标中键按下围绕旋转------------------------------------------------------------------------------
        if (Input.GetMouseButtonDown(2))//鼠标按下
        {
            Cursor.lockState = CursorLockMode.Locked;//锁定鼠标
            
            Vector3 mouseHitPoint = MouseHitPoint();
            if(mouseHitPoint != Vector3.zero)//鼠标有击中
            {
                //没有父亲或者父亲不是轴
                if (transform.parent == null || !transform.parent.name.Equals("CameraAxis"))
                {
                    oldParent = transform.parent;
                    cameraRotateAxisTmp = new GameObject("CameraAxis");
                    cameraRotateAxisTmp.transform.position = mouseHitPoint;
                    cameraRotateAxisTmp.transform.rotation = transform.rotation;
                    //把相机放到旋转轴里面
                    transform.SetParent(cameraRotateAxisTmp.transform,true);
                    transform.localEulerAngles = Vector3.zero;
                    //添加一个小球球用于显示旋转轴位置
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //给小球球起个名字
                    sphere.name = "CameraAxisSphere";
                    sphere.transform.localScale = Vector3.one * 0.1f;
                    //把小球球放到旋转轴上
                    sphere.transform.parent = cameraRotateAxisTmp.transform;
                    sphere.transform.localPosition = Vector3.zero;
                }
            }
            
        }
        if (Input.GetMouseButton(2) && cameraRotateAxisTmp != null)
        {
            Vector2 mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            Vector2 mouseDeltaAround = RotateAroundSensitivity * commonSensitivityUse * mouseMovement;
            //旋转相机轴
            cameraRotateAxisTmp.transform.Rotate(0,mouseDeltaAround.x,0,Space.World);
            Vector3 tmp = cameraRotateAxisTmp.transform.eulerAngles;
            cameraRotateAxisTmp.transform.Rotate(-mouseDeltaAround.y, 0, 0, Space.Self);
            cameraState.SetFromTransform(transform);//更新相机状态
        }
        if (Input.GetMouseButtonUp(2))//鼠标抬起
        {
            if (cameraRotateAxisTmp != null)
            {
                //把小球球游戏对象删除
                GameObject sphere = cameraRotateAxisTmp.transform.Find("CameraAxisSphere").gameObject;
                Destroy(sphere);
                
                cameraRotateAxisTmp.transform.DetachChildren();
                //删除旋转轴
                Destroy(cameraRotateAxisTmp);
                //把相机放回原来的位置
                transform.SetParent(oldParent,true);
            }
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        //鼠标右键环顾相机------------------------------------------------------------------------------
        // Hide and lock cursor when right mouse button pressed
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        // right mouse button hold to rotate camera
        if (Input.GetMouseButton(1))
        {
            // Rotation
            var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            cameraState.yaw += mouseMovement.x * commonSensitivityUse * RotateSensitivity;
            cameraState.pitch += mouseMovement.y * commonSensitivityUse * RotateSensitivity;
        }
        // Unlock and show cursor when right mouse button released
        if (Input.GetMouseButtonUp(1))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        //更新相机状态
        cameraState.UpdateTransform(transform);
        lastMousePosition = mousePosition;
    }
    
    //鼠标处的射线击中的位置
    public static Vector3 MouseHitPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray,out hit,9999,-5,QueryTriggerInteraction.Ignore))//排除触发器
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        // if (cameraRotateAxisTmp != null)
        // {
        //     GizmosEx.DrawArrow(cameraRotateAxisTmp.transform.position,cameraRotateAxisTmp.transform.forward);
        //     GizmosEx.DrawArrow(cameraRotateAxisTmp.transform.position,cameraRotateAxisTmp.transform.up);
        // }
        
        if(dragPlane.normal != Vector3.zero)
        {
            GizmosEx.DrawPlane(-dragPlane.normal.normalized * dragPlane.distance, dragPlane.normal, Color.red,500);
        }
    }
}
