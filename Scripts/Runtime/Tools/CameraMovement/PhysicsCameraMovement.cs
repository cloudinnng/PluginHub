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

        public float mouseSensitivityFactor = 50f;
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
            _rigidbody.angularDrag = 9999;
            // 移动阻力
            _rigidbody.drag = 1;
            // _rigidbody.isKinematic = true;

            // 碰撞器
            _collider = gameObject.GetComponent<SphereCollider>();
            if (_collider == null)
                _collider = gameObject.AddComponent<SphereCollider>();
            _collider.radius = 0.5f;
            PhysicMaterial physicMaterial = new PhysicMaterial("NoFriction");
            physicMaterial.bounciness = 0;
            physicMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            physicMaterial.bounceCombine = PhysicMaterialCombine.Minimum;
            physicMaterial.dynamicFriction = 0;
            physicMaterial.staticFriction = 0;
            _collider.sharedMaterial = physicMaterial;
        }

        void Update()
        {
            // WSAD QE 移动
            float inputX = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
            float inputZ = (Input.GetKey(KeyCode.W) || Input.GetAxis("Mouse ScrollWheel") > 0) ? 1 : (Input.GetKey(KeyCode.S) || Input.GetAxis("Mouse ScrollWheel") < 0) ? -1 : 0;
            float inputY = Input.GetKey(KeyCode.E) ? 1 : Input.GetKey(KeyCode.Q) ? -1 : 0;
            Vector3 moveVector = transform.right * inputX + transform.forward * inputZ + transform.up * inputY;
            if(Input.GetKey(KeyCode.LeftShift))// 加速
                moveVector *= 5;

            _rigidbody.velocity = moveVector * moveSpeed;
            // 速度快会穿墙
            // _rigidbody.MovePosition(_rigidbody.position + moveVector * moveSpeed * Time.deltaTime);
            // transform.Translate(moveVector * moveSpeed * Time.deltaTime, Space.World);



            // 鼠标右键控制视角旋转
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
                yaw += mouseMovement.x * mouseSensitivityFactor * Time.deltaTime;
                pitch += mouseMovement.y * mouseSensitivityFactor * Time.deltaTime;
                transform.eulerAngles = new Vector3(pitch, yaw, 0);
                // _rigidbody.MoveRotation(Quaternion.Euler(pitch, yaw, 0));
                // _rigidbody.rotation = Quaternion.Euler(pitch, yaw, 0);
            }


            // 纠正方位
            if (transform.eulerAngles.z != 0)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
            }
        }
    }
}
