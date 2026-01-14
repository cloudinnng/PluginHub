using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    public partial class Debugger
    {
        [Header("Gesture")]
        //使用手势呼出控制台需要转的圈数
        public int numOfCircleToShow = 3;

        //是否使用toast显示圈数提示
        public bool showGestureTipOnToast = false;
        
        private List<Vector2> gestureDetector = new List<Vector2>();
        private Vector2 gestureSum = Vector2.zero;
        private float gestureLength = 0;
        private int gestureCount = 0;

        //检测到手势后，这个方法返回真
        private bool isGestureDone()
        {
            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (InputEx.touchCount != 1)
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                }
                else
                {
                    if (InputEx.GetTouch(0).phase == TouchPhase.Canceled || InputEx.GetTouch(0).phase == TouchPhase.Ended)
                        gestureDetector.Clear();
                    else if (InputEx.GetTouch(0).phase == TouchPhase.Moved)
                    {
                        Vector2 p = InputEx.GetTouch(0).position;
                        if (gestureDetector.Count == 0 ||
                            (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                            gestureDetector.Add(p);
                    }
                }
            }
            else
            {
                if (InputEx.GetMouseButtonUp(0))
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                }
                else
                {
                    if (InputEx.GetMouseButton(0))
                    {
                        Vector2 p = new Vector2(InputEx.mousePosition.x, InputEx.mousePosition.y);
                        if (gestureDetector.Count == 0 ||
                            (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                            gestureDetector.Add(p);
                    }
                }
            }

            if (gestureDetector.Count < 10)
                return false;

            gestureSum = Vector2.zero;
            gestureLength = 0;
            Vector2 prevDelta = Vector2.zero;
            for (int i = 0; i < gestureDetector.Count - 2; i++)
            {
                Vector2 delta = gestureDetector[i + 1] - gestureDetector[i];
                float deltaLength = delta.magnitude;
                gestureSum += delta;
                gestureLength += deltaLength;

                float dot = Vector2.Dot(delta, prevDelta);
                if (dot < 0f)
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                    return false;
                }

                prevDelta = delta;
            }

            int gestureBase = (Screen.width + Screen.height) / 4;

            if (gestureLength > gestureBase && gestureSum.magnitude < gestureBase / 2)
            {
                gestureDetector.Clear();
                gestureCount++;
                if(showGestureTipOnToast)
                    ToastManager.Instance.Show($"{gestureCount}/{numOfCircleToShow}", 1);
                if (gestureCount >= numOfCircleToShow)
                {
                    gestureCount = 0;
                    return true;
                }
            }

            return false;
        }
    }
}