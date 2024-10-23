using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PluginHub.Runtime
{
    //Transform 扩展方法
    public static class TransformEx
    {
        #region 判断父子关系

        // 若parent是child的父亲，返回真
        // 返回child是否是调用者的直系孩子，或非直系孩子。（只要是孩子就返回真）
        // 此方法与Unity内置方法 Transform.IsChildOf 互为相反功能
        public static bool IsMyChild(this Transform parent, Transform child)
        {
            if (child.parent == null)
                return false;
            Transform tmpParent = child.parent;
            while (tmpParent != null)
            {
                if (tmpParent == parent)
                    return true;
                tmpParent = tmpParent.parent;
            }

            return false;
        }

        //若A是B的父亲，返回真
        public static bool AIsBParent(Transform A, Transform B)
        {
            if (B.parent == null)
                return false;
            Transform tmpParent = B.parent;
            while (tmpParent != null)
            {
                if (tmpParent == A)
                    return true;
                tmpParent = tmpParent.parent;
            }

            return false;
        }

        #endregion


        #region Find方法均可以找到隐藏的对象

        //该方法可以找到隐藏的对象
        public static Transform FindByName(this Transform parent, string name, bool partialMatch = false)
        {
            Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>() as Transform[];
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (parent.IsMyChild(objs[i]))
                    {
                        if (partialMatch)
                        {
                            if (objs[i].name.Contains(name))
                            {
                                return objs[i];
                            }
                        }
                        else
                        {
                            if (objs[i].name == name)
                            {
                                return objs[i];
                            }
                        }
                    }
                }
            }

            return null;
        }

        //该方法可以找到隐藏的对象
        public static Transform[] FindAllByName(this Transform parent, string name, bool partialMatch = false)
        {
            Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>() as Transform[];
            List<Transform> returnList = new List<Transform>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (parent.IsMyChild(objs[i]))
                    {
                        if (partialMatch)
                        {
                            if (objs[i].name.Contains(name))
                            {
                                returnList.Add(objs[i]);
                            }
                        }
                        else
                        {
                            if (objs[i].name == name)
                            {
                                returnList.Add(objs[i]);
                            }
                        }
                    }
                }
            }

            return returnList.ToArray();
        }

        //该方法可以找到隐藏的对象
        public static T FindByType<T>(this Transform parent) where T : MonoBehaviour
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            List<T> returnList = new List<T>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (objs[i].GetType() == typeof(T) && parent.IsMyChild(objs[i].transform))
                    {
                        return objs[i];
                    }
                }
            }

            return null;
        }

        //该方法可以找到隐藏的对象
        public static T[] FindAllByType<T>(this Transform parent) where T : Component
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            List<T> returnList = new List<T>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (objs[i].GetType() == typeof(T) && parent.IsMyChild(objs[i].transform))
                    {
                        returnList.Add(objs[i]);
                    }
                }
            }

            return returnList.OrderBy((item) => item.transform.GetSiblingIndex()).ToArray();
        }

        //该方法可以找到隐藏的对象
        public static T FindByTypeName<T>(this Transform parent, string name, bool partialMatch = false)
            where T : MonoBehaviour
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            List<T> returnList = new List<T>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (objs[i].GetType() == typeof(T) && parent.IsMyChild(objs[i].transform))
                    {
                        if (partialMatch)
                        {
                            if (objs[i].name.Contains(name))
                            {
                                return objs[i];
                            }
                        }
                        else
                        {
                            if (objs[i].name == name)
                            {
                                return objs[i];
                            }
                        }
                    }
                }
            }

            return null;
        }

        //该方法可以找到隐藏的对象
        public static T[] FindAllByTypeName<T>(this Transform parent, string name, bool partialMatch = false)
            where T : Component
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            List<T> returnList = new List<T>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (objs[i].GetType() == typeof(T) && parent.IsMyChild(objs[i].transform))
                    {
                        if (partialMatch)
                        {
                            if (objs[i].name.Contains(name))
                            {
                                returnList.Add(objs[i]);
                            }
                        }
                        else
                        {
                            if (objs[i].name == name)
                            {
                                returnList.Add(objs[i]);
                            }
                        }
                    }
                }
            }

            return returnList.OrderBy((item) => item.transform.GetSiblingIndex()).ToArray();
        }

        #endregion


        //删除所有孩子对象
        public static void DestroyChildren(this Transform transform, bool includeInactive, bool immediate)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (includeInactive || child.gameObject.activeSelf)
                {
                    if (immediate)
                        GameObject.DestroyImmediate(child.gameObject);
                    else
                        GameObject.Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        /// 指定缩放中心的缩放
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="pivot"></param>
        /// <param name="newScale"></param>
        public static void ScaleAround(this Transform transform, Vector3 pivot, Vector3 newScale)
        {
            Vector3 A = transform.localPosition;
            Vector3 B = pivot;

            Vector3 C = A - B; // diff from object pivot to desired pivot/origin

            float RS = newScale.x / transform.localScale.x; // relataive scale factor

            // calc final position post-scale
            Vector3 finalPosition = B + C * RS;

            // finally, actually perform the scale/translation
            transform.localScale = newScale;
            transform.localPosition = finalPosition;
        }

        public static Transform[] DirectChildren(this Transform parent)
        {
            return Enumerable.Range(0, parent.childCount)
                .Select(i => parent.GetChild(i))
                .ToArray();
        }


        //使用递归获取一个Transform的查找路径
        //之后可以用GameObject.Find()找到对象
        public static void GetFindPath(this Transform transform, StringBuilder sb)
        {
            sb.Insert(0, $"/{transform.name}");
            if (transform.parent != null)
            {
                GetFindPath(transform.parent, sb);
            }
        }

        //返回一系列Transform是否具有相同的父亲
        public static bool SameParent(Transform[] transforms)
        {
            Transform parent = transforms[0].parent;
            return transforms.All(t => t.parent == parent);
        }
    }
}