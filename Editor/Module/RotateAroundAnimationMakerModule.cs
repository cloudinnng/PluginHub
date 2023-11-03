using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace PluginHub.Module
{

    //用脚本方式添加关键帧，制作围绕目标旋转的动画片段
    public class RotateAroundAnimationMakerModule : PluginHubModuleBase
    {
        //围绕中心
        private Vector3 _centerPosition
        {
            get
            {
                Vector3 re = new Vector3();
                re.x = EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_centerPosition_x", 0);
                re.y = EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_centerPosition_y", 0);
                re.z = EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_centerPosition_z", 0);
                return re;
            }
            set
            {
                EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_centerPosition_x", value.x);
                EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_centerPosition_y", value.y);
                EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_centerPosition_z", value.z);
            }
        }

        private float _radius
        {
            get => EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_radius", 10);
            set => EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_radius", value);
        }

        private float animationDuration
        {
            get => EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_animationDuration", 10);
            set => EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_animationDuration", value);
        }

        //旋转动画开始时所处的角度，x轴正方向为0度
        private float startAngle
        {
            get => EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_startAngle", 0);
            set => EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_startAngle", value);
        }

        private float endAngle
        {
            get => EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_endAngle", 360);
            set => EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_endAngle", value);
        }

        //用于计算观测俯仰角时的中心坐标y偏移量
        private float centerPosOffsetY
        {
            get => EditorPrefs.GetFloat("RotateAroundAnimationMakerModule_centerPosOffsetY", 0);
            set => EditorPrefs.SetFloat("RotateAroundAnimationMakerModule_centerPosOffsetY", value);
        }

        private AnimationClip _animationClip; //所要制作的动画片段

        protected override void DrawGuiContent()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("跳转到中心",GUILayout.ExpandWidth(false)))
                {
                    SceneView.lastActiveSceneView.pivot = GetUseCenter();
                    SceneView.lastActiveSceneView.Repaint();
                }

                if (GUILayout.Button("重置值"))
                {
                    _centerPosition = Vector3.zero;
                    _radius = 10;
                    animationDuration = 10;
                    startAngle = 0;
                    endAngle = 360;
                    centerPosOffsetY = 0;
                }
            }
            GUILayout.EndHorizontal();


            _centerPosition = EditorGUILayout.Vector3Field("Center Position", _centerPosition);
            _radius = Mathf.Max(0.1f, EditorGUILayout.FloatField("Radius", _radius));
            centerPosOffsetY = EditorGUILayout.FloatField("Center Pos Offset Y", centerPosOffsetY);
            animationDuration = Mathf.Max(0.5f, EditorGUILayout.FloatField("Cycle Time", animationDuration));
            startAngle = EditorGUILayout.FloatField("Start Angle", startAngle);
            endAngle = EditorGUILayout.FloatField("End Angle", endAngle);

            _animationClip =
                EditorGUILayout.ObjectField("Animation", _animationClip, typeof(AnimationClip), true) as AnimationClip;


            EditorGUILayout.HelpBox("拖入AnimationClip即可生成(通常是关联相机的)，无需点击按钮。",MessageType.Info);

            if (_animationClip != null)
            {
                //清除所有关键帧。
                _animationClip.ClearCurves();

                int frameCount = (int)(_animationClip.frameRate * animationDuration);
                Debug.Log($"frameCount:{frameCount}");

                Keyframe[] keysPosX = new Keyframe[frameCount];
                Keyframe[] keysPosY = new Keyframe[frameCount];
                Keyframe[] keysPosZ = new Keyframe[frameCount];
                Keyframe[] keysRotX = new Keyframe[frameCount];
                Keyframe[] keysRotY = new Keyframe[frameCount];
                Keyframe[] keysRotZ = new Keyframe[frameCount];

                float timeStep = animationDuration / (frameCount);
                float angleStep = (endAngle - startAngle) / (frameCount);

                for (int i = 0; i < frameCount; i++)
                {
                    Vector3 circlePosition = GetCirclePosition(startAngle + angleStep * i);
                    keysPosX[i] = new Keyframe(i * timeStep, circlePosition.x);
                    keysPosY[i] = new Keyframe(i * timeStep, circlePosition.y);
                    keysPosZ[i] = new Keyframe(i * timeStep, circlePosition.z);

                    Vector3 eulerAngles = GetToCenterEulerAngles(circlePosition, centerPosOffsetY);
                    keysRotX[i] = new Keyframe(i * timeStep, eulerAngles.x);
                    keysRotY[i] = new Keyframe(i * timeStep, eulerAngles.y);
                    keysRotZ[i] = new Keyframe(i * timeStep, eulerAngles.z);
                }

                //last frame is the same as the first frame
                // keysPosX[keysPosX.Length - 1] = new Keyframe(animationDuration, keysPosX[0].value);
                // keysPosY[keysPosY.Length - 1] = new Keyframe(animationDuration, keysPosY[0].value);
                // keysPosZ[keysPosZ.Length - 1] = new Keyframe(animationDuration, keysPosZ[0].value);
                //make the curve
                AnimationCurve curvePosX = new AnimationCurve(keysPosX);
                AnimationCurve curvePosY = new AnimationCurve(keysPosY);
                AnimationCurve curvePosZ = new AnimationCurve(keysPosZ);
                AnimationCurve curveRotX = new AnimationCurve(keysRotX);
                AnimationCurve curveRotY = new AnimationCurve(keysRotY);
                AnimationCurve curveRotZ = new AnimationCurve(keysRotZ);


                //relaticePath 是指记录动画的对象，如果是空字符串，则表示记录动画的对象是Animator组件所在的对象
                _animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", curvePosX);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", curvePosY);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", curvePosZ);

                _animationClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.x", curveRotX);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.y", curveRotY);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.z", curveRotZ);
            }
        }

        protected override bool OnSceneGUI(SceneView sceneView)
        {
            //中心坐标移动手柄
            _centerPosition = Handles.PositionHandle(_centerPosition, Quaternion.identity);

            Handles.DrawWireDisc(_centerPosition, Vector3.up, _radius);

            float size = HandleUtility.GetHandleSize(GetCirclePosition(startAngle)) * 0.2f;
            Handles.SphereHandleCap(0, GetCirclePosition(startAngle), Quaternion.identity, size, EventType.Repaint);
            Handles.color = Color.red;

            size = HandleUtility.GetHandleSize(GetCirclePosition(endAngle)) * 0.2f;
            Handles.SphereHandleCap(0, GetCirclePosition(endAngle), Quaternion.identity, size, EventType.Repaint);
            Handles.color = Color.white;

            size = HandleUtility.GetHandleSize(GetUseCenter()) * 0.2f;
            Handles.SphereHandleCap(0, GetUseCenter(), Quaternion.identity, size, EventType.Repaint);

            //caculate size
            size = HandleUtility.GetHandleSize(GetCirclePosition(startAngle)) * 1f;

            Handles.ArrowHandleCap(0, GetCirclePosition(startAngle),
                Quaternion.Euler(GetToCenterEulerAngles(GetCirclePosition(startAngle), centerPosOffsetY)), size,
                EventType.Repaint);

            return true;
        }

        private Vector3 GetUseCenter()
        {
            return _centerPosition + new Vector3(0, centerPosOffsetY, 0);
        }

        private Vector3 GetToCenterEulerAngles(Vector3 position, float centerPosOffsetY = 0)
        {
            Vector3 useCenter = GetUseCenter();
            return Quaternion.LookRotation(useCenter - position).eulerAngles;
        }

        private Vector3 GetCirclePosition(float angle)
        {
            float x = _centerPosition.x + _radius * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = _centerPosition.y;
            float z = _centerPosition.z + _radius * Mathf.Sin(angle * Mathf.Deg2Rad);
            return new Vector3(x, y, z);
        }

    }
}