using UnityEngine;

//类似unity编辑器一样的简易相机控制功能，挂在camera上。右键按下使用asdqwe移动相机
[RequireComponent(typeof(Camera))]
public class EditorStyleCameraMovement : MonoBehaviour
{
    class CameraState
    {
        public float yaw;
        public float pitch;
        public float roll;
        public float x;
        public float y;
        public float z;
        public bool planeMoveMode = false;
        
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
            Vector3 rotatedTranslation;
            if(planeMoveMode)
                rotatedTranslation = Quaternion.Euler(0, yaw, roll) * translation;
            else
                rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;
            
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

        public void UpdateTransform(Transform t)
        {
            t.eulerAngles = new Vector3(pitch, yaw, roll);
            t.position = new Vector3(x, y, z);
        }
    }

    CameraState m_TargetCameraState = new CameraState();
    CameraState m_InterpolatingCameraState = new CameraState();

    [Header("Movement Settings")] [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.相机运动速度")]
    public float boost = 3.5f;

    [Tooltip("Time it takes to interpolate camera position 99% of the way to the target.该值越大，相机运动到最后会滑动一下"), Range(0.001f, 1f)]
    public float positionLerpTime = 0.2f;

    [Header("Rotation Settings")]
    [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
    public AnimationCurve mouseSensitivityCurve =
        new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

    [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
    public float rotationLerpTime = 0.01f;

    //是否启用基于水平面的移动模式,计算移动的时候会忽略pitch角度
    public bool planeMoveMode = false;

    //2023年11月20日 新增 Alt + 左键 旋转相机功能
    public bool enableRotateAround = false;
    public float rotateRroundSpeed = 5;
    private Vector3 rotateAroundOriginPos;

    void OnEnable()
    {
        m_TargetCameraState.SetFromTransform(transform);
        m_TargetCameraState.planeMoveMode = planeMoveMode;
        m_InterpolatingCameraState.SetFromTransform(transform);
        m_InterpolatingCameraState.planeMoveMode = planeMoveMode;
    }

    Vector3 GetInputTranslationDirection()
    {
        Vector3 direction = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector3.back;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector3.left;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector3.right;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            direction += Vector3.down;
        }

        if (Input.GetKey(KeyCode.E))
        {
            direction += Vector3.up;
        }

        return direction;
    }

    void Update()
    {

        #region 鼠标左键

        if (enableRotateAround)
        {
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
                    {
                        rotateAroundOriginPos = hit.point;
                    }else
                    {
                        rotateAroundOriginPos = Vector3.zero;
                    }
                    print($"rotateAroundOriginPos: {rotateAroundOriginPos}");
                }

                if (Input.GetMouseButton(0))
                {
                    Vector2 mouseMovement = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y")) * rotateRroundSpeed;

                    transform.RotateAround(rotateAroundOriginPos, Vector3.up, mouseMovement.x);
                    transform.RotateAround(rotateAroundOriginPos, transform.right, mouseMovement.y);
                }

                if (Input.GetMouseButtonUp(0))
                {
                    rotateAroundOriginPos = Vector3.zero;
                }
            }
        }

        #endregion


        #region 鼠标右键

        // Hide and lock cursor when right mouse button pressed
        if (Input.GetMouseButtonDown(1))
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Unlock and show cursor when right mouse button released
        if (Input.GetMouseButtonUp(1))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetMouseButton(1))
        {
            // Rotation
            var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

            m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
            m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
        }

        if (Input.GetMouseButton(1))
        {
            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            float mouseScrollY = Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(mouseScrollY, 0))
            {
                boost += mouseScrollY * 0.2f;
                if(ToastManager.Instance)
                    ToastManager.Instance.Show($"{boost:F1}",0.1f);
            }
        }

        //2024年3月12日 新增不按住右键也可以通过左Ctrl键来移动相机
        if(Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftControl))
        {
            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (Input.GetKey(KeyCode.LeftShift))
            {
                translation *= 10.0f;
            }

            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
        }

        #endregion

    }
}