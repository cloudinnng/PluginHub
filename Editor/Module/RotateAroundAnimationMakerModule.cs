using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace PluginHub.Module
{

//用脚本方式添加关键帧，制作围绕旋转的动画片段
    public class RotateAroundAnimationMakerModule : PluginHubModuleBase
    {
        //围绕中心
        private Vector3 _centerPosition;
        private float _radius = 1;
        private float animationDuration = 10;

        //旋转动画开始时所处的角度，x轴正方向为0度
        private float startAngle = 0;

        private float endAngle = 360;

        //用于计算观测俯仰角时的中心坐标y偏移量
        private float centerPosOffsetY = 0;

        private AnimationClip _animationClip; //所要制作的动画片段

        protected override void DrawGuiContent()
        {
            _centerPosition = EditorGUILayout.Vector3Field("Center Position", _centerPosition);
            _radius = EditorGUILayout.FloatField("Radius", _radius);
            animationDuration = EditorGUILayout.FloatField("Cycle Time", animationDuration);
            startAngle = EditorGUILayout.FloatField("Start Angle", startAngle);
            endAngle = EditorGUILayout.FloatField("End Angle", endAngle);
            centerPosOffsetY = EditorGUILayout.FloatField("Center Pos Offset Y", centerPosOffsetY);

            _animationClip =
                EditorGUILayout.ObjectField("Animation", _animationClip, typeof(AnimationClip), true) as AnimationClip;


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


                _animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", curvePosX);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", curvePosY);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", curvePosZ);

                _animationClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.x", curveRotX);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.y", curveRotY);
                _animationClip.SetCurve("", typeof(Transform), "m_LocalEulerAngles.z", curveRotZ);
            }
        }

        public override bool OnSceneGUI(SceneView sceneView)
        {
            //中心坐标移动手柄
            _centerPosition = Handles.PositionHandle(_centerPosition, Quaternion.identity);


            Handles.DrawWireDisc(_centerPosition, Vector3.up, _radius);

            Handles.SphereHandleCap(0, GetCirclePosition(startAngle), Quaternion.identity, 0.2f, EventType.Repaint);
            Handles.color = Color.red;
            Handles.SphereHandleCap(0, GetCirclePosition(endAngle), Quaternion.identity, 0.2f, EventType.Repaint);
            Handles.color = Color.white;

            Handles.ArrowHandleCap(0, GetCirclePosition(startAngle),
                Quaternion.Euler(GetToCenterEulerAngles(GetCirclePosition(startAngle), centerPosOffsetY)), 0.2f,
                EventType.Repaint);

            return true;
        }

        private Vector3 GetToCenterEulerAngles(Vector3 position, float centerPosOffsetY = 0)
        {
            Vector3 useCenter = _centerPosition + new Vector3(0, centerPosOffsetY, 0);
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