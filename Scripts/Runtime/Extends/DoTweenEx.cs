using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 非DoTween的扩展，而是自己写的DoTween的简单实现，暂且叫DoTweenEx类
    /// 使用StartCoroutine启动
    /// </summary>
    public static class DoTweenEx
    {
        public static AnimationCurve EaseInOut = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public static AnimationCurve Linear = AnimationCurve.Linear(0, 0, 1, 1);

        public static AnimationCurve EaseIn = new AnimationCurve(new Keyframe(0, 0) { inTangent = 0, outTangent = 0 },
            new Keyframe(1, 1) { inTangent = 2, outTangent = 0 });

        public static AnimationCurve EaseOut = new AnimationCurve(new Keyframe(0, 0) { inTangent = 0, outTangent = 2 },
            new Keyframe(1, 1) { inTangent = 0, outTangent = 0 });

        public static AnimationCurve AnimCurve = EaseInOut;


        //想要并行执行
        // yield return DoTweenEx.StartCoroutinesInParallel(new IEnumerator[]
        // {
        //     DoTweenEx.DoSizeDelta(image.rectTransform, imageSize, 1),
        //     DoTweenEx.DoAnchorMove(image.rectTransform, targetPos, 1)
        // }, this);
        //并行执行协程的关键是使用StartCoroutine方法让其同时启动。
        //关键在于StartCoroutine方法的返回值是Coroutine，而不是IEnumerator
        //我们可以yield return Coroutine,也可以yield return IEnumerator。效果都是等待协程完成。
        public static IEnumerator StartCoroutinesInParallel(IEnumerator[] routines, MonoBehaviour onwer)
        {
            Coroutine[] coroutines = new Coroutine[routines.Length];
            for (int i = 0; i < routines.Length; i++)
            {
                coroutines[i] = onwer.StartCoroutine(routines[i]);
            }

            // 等待所有协程完成
            for (int i = 0; i < coroutines.Length; i++)
            {
                yield return coroutines[i];
            }
        }


        public static IEnumerator DoCustomAction(float duration, System.Action<float> action)
        {
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                action(t);
                yield return null;
            }
        }

        #region Move

        public static IEnumerator DoMove(Transform transform, Vector3 targetPos, float duration)
        {
            Vector3 startPos = transform.position;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
                transform.position = pos;
                yield return null;
            }
        }

        public static IEnumerator DoLocalMove(Transform transform, Vector3 targetLocalPos, float duration)
        {
            Vector3 startPos = transform.localPosition;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                Vector3 pos = Vector3.Lerp(startPos, targetLocalPos, t);
                transform.localPosition = pos;
                yield return null;
            }
        }

        public static IEnumerator DoMoveX(Transform transform, float targetPosX, float duration)
        {
            float startX = transform.position.x;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float x = Mathf.Lerp(startX, targetPosX, t);
                transform.position = new Vector3(x, transform.position.y, transform.position.z);
                yield return null;
            }
        }

        public static IEnumerator DoLocalMoveX(Transform transform, float targetLocalPosX, float duration)
        {
            float startX = transform.localPosition.x;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float x = Mathf.Lerp(startX, targetLocalPosX, t);
                transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
                yield return null;
            }
        }

        public static IEnumerator DoMoveY(Transform transform, float targetPosY, float duration)
        {
            float startY = transform.position.y;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float y = Mathf.Lerp(startY, targetPosY, t);
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
                yield return null;
            }
        }

        public static IEnumerator DoLocalMoveY(Transform transform, float targetLocalPosY, float duration)
        {
            float startY = transform.localPosition.y;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float y = Mathf.Lerp(startY, targetLocalPosY, t);
                transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
                yield return null;
            }
        }

        public static IEnumerator DoMoveZ(Transform transform, float targetPosZ, float duration)
        {
            float startZ = transform.position.z;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float z = Mathf.Lerp(startZ, targetPosZ, t);
                transform.position = new Vector3(transform.position.x, transform.position.y, z);
                yield return null;
            }
        }

        public static IEnumerator DoLocalMoveZ(Transform transform, float targetLocalPosZ, float duration)
        {
            float startZ = transform.localPosition.z;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float z = Mathf.Lerp(startZ, targetLocalPosZ, t);
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
                yield return null;
            }
        }

        public static IEnumerator DoAnchorMove(RectTransform rectTransform, Vector2 targetPos, float duration)
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                Vector2 pos = Vector2.Lerp(startPos, targetPos, t);
                rectTransform.anchoredPosition = pos;
                yield return null;
            }
        }

        public static IEnumerator DoAnchorMoveX(RectTransform rectTransform, float targetPosX, float duration)
        {
            float startX = rectTransform.anchoredPosition.x;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float x = Mathf.Lerp(startX, targetPosX, t);
                rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
                yield return null;
            }
        }

        public static IEnumerator DoAnchorMoveY(RectTransform rectTransform, float targetPosY, float duration)
        {
            float startY = rectTransform.anchoredPosition.y;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float y = Mathf.Lerp(startY, targetPosY, t);
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
                yield return null;
            }
        }

        public static IEnumerator DoAnchorMoveZ(RectTransform rectTransform, float targetPosZ, float duration)
        {
            float startZ = rectTransform.anchoredPosition3D.z;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float z = Mathf.Lerp(startZ, targetPosZ, t);
                rectTransform.anchoredPosition3D =
                    new Vector3(rectTransform.anchoredPosition3D.x, rectTransform.anchoredPosition3D.y, z);
                yield return null;
            }
        }

        #endregion

        #region SizeDelta

        public static IEnumerator DoSizeDelta(RectTransform rectTransform, Vector2 targetSizeDelta, float duration)
        {
            Vector2 startSizeDelta = rectTransform.sizeDelta;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                Vector2 sizeDelta = Vector2.Lerp(startSizeDelta, targetSizeDelta, t);
                rectTransform.sizeDelta = sizeDelta;
                yield return null;
            }
        }

        #endregion

        #region Rotate

        public static IEnumerator DoRotate(Transform transform, Vector3 targetAngle, float duration)
        {
            Vector3 startAngle = transform.eulerAngles;
            //lerp from nearlest angle
            if (Mathf.Abs(startAngle.y - targetAngle.y) > 180)
                if (startAngle.y > targetAngle.y) startAngle.y -= 360;
                else startAngle.y += 360;

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                Vector3 angle = Vector3.Lerp(startAngle, targetAngle, t);
                transform.eulerAngles = angle;
                yield return null;
            }
        }

        public static IEnumerator DoLocalRotate(Transform transform, Vector3 targetLocalAngle, float duration)
        {
            Vector3 startAngle = transform.localEulerAngles;
            //lerp from nearlest angle
            if (Mathf.Abs(startAngle.y - targetLocalAngle.y) > 180)
                if (startAngle.y > targetLocalAngle.y) startAngle.y -= 360;
                else startAngle.y += 360;

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                Vector3 angle = Vector3.Lerp(startAngle, targetLocalAngle, t);
                transform.localEulerAngles = angle;
                yield return null;
            }
        }

        public static IEnumerator DoLocalRotateX(Transform transform, float targetLocalAngleX, float duration)
        {
            float startAngleX = transform.localEulerAngles.x;
            //lerp from nearlest angle
            if (Mathf.Abs(startAngleX - targetLocalAngleX) > 180)
                if (startAngleX > targetLocalAngleX) startAngleX -= 360;
                else startAngleX += 360;

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float x = Mathf.Lerp(startAngleX, targetLocalAngleX, t);
                transform.localEulerAngles = new Vector3(x, transform.localEulerAngles.y, transform.localEulerAngles.z);
                yield return null;
            }
        }

        public static IEnumerator DoLocalRotateY(Transform transform, float targetLocalAngleY, float duration)
        {
            float startAngleY = transform.localEulerAngles.y;
            //lerp from nearlest angle
            if (Mathf.Abs(startAngleY - targetLocalAngleY) > 180)
                if (startAngleY > targetLocalAngleY) startAngleY -= 360;
                else startAngleY += 360;

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float y = Mathf.Lerp(startAngleY, targetLocalAngleY, t);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, y, transform.localEulerAngles.z);
                yield return null;
            }
        }

        public static IEnumerator DoLocalRotateZ(Transform transform, float targetLocalAngleZ, float duration)
        {
            float startAngleZ = transform.localEulerAngles.z;
            //lerp from nearlest angle
            if (Mathf.Abs(startAngleZ - targetLocalAngleZ) > 180)
                if (startAngleZ > targetLocalAngleZ) startAngleZ -= 360;
                else startAngleZ += 360;

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float z = Mathf.Lerp(startAngleZ, targetLocalAngleZ, t);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, z);
                yield return null;
            }
        }

        #endregion

        #region Scale

        public static IEnumerator DoLocalScale(Transform transform, Vector3 targetLocalScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);

                Vector3 scale = Vector3.Lerp(startScale, targetLocalScale, t);
                transform.localScale = scale;
                yield return null;
            }
        }

        public static IEnumerator DoLocalScaleX(Transform transform, float targetLocalScaleX, float duration)
        {
            float startScaleX = transform.localScale.x;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float x = Mathf.Lerp(startScaleX, targetLocalScaleX, t);
                transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
                yield return null;
            }
        }

        public static IEnumerator DoLocalScaleY(Transform transform, float targetLocalScaleY, float duration)
        {
            float startScaleY = transform.localScale.y;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float y = Mathf.Lerp(startScaleY, targetLocalScaleY, t);
                transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
                yield return null;
            }
        }

        public static IEnumerator DoLocalScaleZ(Transform transform, float targetLocalScaleZ, float duration)
        {
            float startScaleZ = transform.localScale.z;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = AnimCurve.Evaluate(time / duration);
                float z = Mathf.Lerp(startScaleZ, targetLocalScaleZ, t);
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
                yield return null;
            }
        }

        #endregion
    }
}