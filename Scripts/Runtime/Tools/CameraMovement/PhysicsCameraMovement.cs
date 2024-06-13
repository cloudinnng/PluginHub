using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    // 一个简单的自由相机控制，控制方式类似Unity编辑器,移动会被带碰撞器的墙体阻挡
    public class PhysicsCameraMovement : MonoBehaviour
    {
        public float moveSpeed = 10;
        private Rigidbody _rigidbody;
        private SphereCollider _collider;

        public float mouseSensitivityFactor = 1;
        private float yaw;
        private float pitch;

        private void Awake()
        {
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            if (_rigidbody == null)
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ;// 禁止Z轴旋转
            // 旋转阻力
            _rigidbody.angularDrag = 999;
            // 移动阻力
            // _rigidbody.drag = 999;
            // _rigidbody.isKinematic = true;

            _collider = gameObject.GetComponent<SphereCollider>();
            if (_collider == null)
                _collider = gameObject.AddComponent<SphereCollider>();
            _collider.radius = 0.5f;
        }

        void Update()
        {
            // WSAD QE 移动
            float inputX = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
            float inputZ = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
            float inputY = Input.GetKey(KeyCode.E) ? 1 : Input.GetKey(KeyCode.Q) ? -1 : 0;
            Vector3 move = transform.right * inputX + transform.forward * inputZ + transform.up * inputY;
            _rigidbody.velocity = move * moveSpeed;

            // 鼠标右键控制视角
            if (Input.GetMouseButtonDown(1))
            {
                yaw = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Input.GetMouseButtonUp(1))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (Input.GetMouseButton(1))
            {
                Vector2 mouseMovement = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
                yaw += mouseMovement.x * mouseSensitivityFactor;
                pitch += mouseMovement.y * mouseSensitivityFactor;
                transform.eulerAngles = new Vector3(pitch, yaw, 0);
            }


            // 纠正方位
            if (transform.eulerAngles.z != 0)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
            }
        }
    }
}
