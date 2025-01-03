using UnityEngine;
using System.Collections.Generic;

namespace PluginHub.Runtime
{
    // 使用一个脚本解决在移动设备上简单漫游场景的摄像机控制问题
    // - 在屏幕左侧：通过触摸控制相机前后左右移动
    // - 在屏幕右侧：通过触摸控制相机视角的上下左右旋转
    // - 在屏幕顶部区域：通过左右滑动控制相机的升降
    // 同时提供了在Unity编辑器中使用鼠标模拟触摸的测试功能
    public class MobileCameraMovement : MonoBehaviour
    {
        public float MoveSpeed = 1f;          // 移动速度系数
        public float LookAroundSpeed = 2.5f;    // 视角旋转速度系数
        public float HeightControlSpeed = .8f;  // 升降速度系数
        
        [SerializeField]
        private bool enableMouseTest = true;    // 是否启用编辑器中的鼠标测试模式

        private Touch simulatedTouch;           // 用于模拟的触摸数据
        private bool touchActive;               // 模拟触摸的激活状态
        private Vector2 previousMousePosition;  // 上一帧的鼠标位置
        private Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();

        void Update()
        {
            if (Application.isEditor && enableMouseTest)
            {
                SimulateTouch();
            }

            // 更新触摸位置字典
            foreach (Touch touch in GetTouches())
            {
                // 如果是触摸开始，记录初始位置
                if (touch.phase == TouchPhase.Began)
                {
                    lastTouchPositions[touch.fingerId] = touch.position;
                    continue;
                }
                
                // 确保我们有上一帧的位置
                if (!lastTouchPositions.ContainsKey(touch.fingerId))
                {
                    lastTouchPositions[touch.fingerId] = touch.position;
                    continue;
                }

                // 计算触摸增量
                Vector2 deltaPosition = touch.position - lastTouchPositions[touch.fingerId];

                // 处理触摸逻辑
                float heightRatio = touch.position.y / Screen.height;
                if (heightRatio > 0.8f)
                {
                    HandleHeightControl(deltaPosition);
                }
                else if (touch.position.x <= Screen.width * 0.5f)
                {
                    HandleNormalMove(deltaPosition);
                }
                else
                {
                    HandleNormalLookAround(deltaPosition);
                }

                // 更新上一帧位置
                lastTouchPositions[touch.fingerId] = touch.position;

                // 如果触摸结束，清除记录
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    lastTouchPositions.Remove(touch.fingerId);
                }
            }
        }

        private void SimulateTouch()
        {
            if (Input.GetMouseButton(0))
            {
                if (!touchActive)
                {
                    // 触摸开始
                    touchActive = true;
                    previousMousePosition = Input.mousePosition;
                    simulatedTouch = new Touch
                    {
                        phase = TouchPhase.Began,
                        position = Input.mousePosition,
                        deltaPosition = Vector2.zero
                    };
                }
                else
                {
                    // 触摸移动
                    Vector2 deltaPosition = (Vector2)Input.mousePosition - previousMousePosition;
                    simulatedTouch = new Touch
                    {
                        phase = TouchPhase.Moved,
                        position = Input.mousePosition,
                        deltaPosition = deltaPosition
                    };
                    previousMousePosition = Input.mousePosition;
                }
            }
            else if (touchActive)
            {
                // 触摸结束
                touchActive = false;
                simulatedTouch = new Touch
                {
                    phase = TouchPhase.Ended,
                    position = Input.mousePosition,
                    deltaPosition = Vector2.zero
                };
            }
        }

        private IEnumerable<Touch> GetTouches()
        {
            if (Application.isEditor && enableMouseTest && touchActive)
            {
                yield return simulatedTouch;
            }
            else
            {
                foreach (Touch touch in Input.touches)
                {
                    yield return touch;
                }
            }
        }

        private int GetTouchCount()
        {
            if (Application.isEditor && enableMouseTest)
            {
                return touchActive ? 1 : 0;
            }
            return Input.touchCount;
        }

        // 处理高度控制
        private void HandleHeightControl(Vector2 deltaPosition)
        {
            float normalizedDelta = deltaPosition.x / Screen.width;
            
            Vector3 newPosition = transform.position;
            newPosition.y += normalizedDelta * HeightControlSpeed * 1000f * Time.deltaTime;
            
            transform.position = newPosition;
        }

        // 处理正常移动
        private void HandleNormalMove(Vector2 deltaPosition)
        {
            Vector2 normalizedDelta = new Vector2(
                deltaPosition.x / Screen.width,
                deltaPosition.y / Screen.height
            );
            
            Vector3 moveDirection = new Vector3(
                normalizedDelta.x,
                0f,
                normalizedDelta.y
            ) * 1000f;
            
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection.y = 0f;
            moveDirection.Normalize();
            
            transform.position += moveDirection * MoveSpeed * Time.deltaTime;
        }

        // 处理视角旋转
        private void HandleNormalLookAround(Vector2 deltaPosition)
        {
            float rotationX = (deltaPosition.x / Screen.width) * LookAroundSpeed * 100f;
            float rotationY = (deltaPosition.y / Screen.height) * LookAroundSpeed * 100f;
            
            transform.Rotate(Vector3.up * rotationX, Space.World);
            transform.Rotate(Vector3.right * -rotationY, Space.Self);
        }
    }
}
