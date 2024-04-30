using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PluginHub.Runtime
{
//绑定在相机上
//类似绝地求生的第三人称视角
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target; // 跟随的目标物体
        public Vector3 targetOffset = new Vector3(0, 0f, 0); // 目标物体的偏移量
        public Vector3 cameraOffset = new Vector3(0, 0f, 0); // 相机的偏移量
        public float mouseSensitivityX = 2000.0f; // 鼠标x轴灵敏度
        public float mouseSensitivityY = 300.0f; // 鼠标y轴灵敏度
        public float _cameraDistance = 3;

        private float _cameraHightOffset = 2;
        private float _cameraWorldAngle = 0;

        private void Start()
        {
            //隐藏且锁定鼠标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }


        void LateUpdate()
        {
            if (target == null) return;

            //mouse input
            float horizontalInput = Input.GetAxis("Mouse X") * (mouseSensitivityX * Time.deltaTime);
            float verticalInput = Input.GetAxis("Mouse Y") * (mouseSensitivityY * Time.deltaTime);

            //鼠标y值控制相机高度
            _cameraHightOffset -= verticalInput;
            _cameraHightOffset = Mathf.Clamp(_cameraHightOffset, 0.2f, 6);

            //相机跟随位置
            Vector3 targetPosition = target.position + targetOffset;

            Vector3 cameraTargetPosition = targetPosition + Vector3.up * _cameraHightOffset;
            _cameraWorldAngle += horizontalInput;
            cameraTargetPosition += Quaternion.Euler(0, _cameraWorldAngle, 0) * Vector3.back * _cameraDistance;
            cameraTargetPosition += transform.TransformVector(cameraOffset);

            //set transform
            transform.position = Vector3.Lerp(transform.position, cameraTargetPosition, 1f);

            //look at target
            Vector3 lookTarget = targetPosition + transform.TransformVector(cameraOffset);
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position);
        }
    }
}