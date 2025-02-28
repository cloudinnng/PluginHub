using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginHub.Runtime
{
//这个类扩展了GameObject的静态Find方法的功能
    public static class GameObjectEx
    {

        // 这个方法可以找到隐藏的对象
        public static GameObject Find(string path)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                Transform transform = allObjects[i].transform;
                string findPath = GetGameObjectFindPath(transform);
                if (findPath == path)
                    return allObjects[i].gameObject;
            }
            return null;
        }

        private static string GetGameObjectFindPath(Transform transform)
        {
            StringBuilder sb = new StringBuilder();
            while (transform != null)
            {
                sb.Insert(0, transform.name);
                if (transform.parent != null)
                    sb.Insert(0, "/");
                transform = transform.parent;
            }
            return sb.ToString();
        }


        #region ByName

        /// <summary>
        /// 在场景中查找给定name的对象
        /// 该方法可以找到隐藏的对象
        /// </summary>
        public static Transform FindByName(string name, bool partialMatch = false)
        {
            Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>() as Transform[];
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (partialMatch)
                    {
                        if (objs[i].name.Contains(name))
                            return objs[i];
                    }
                    else
                    {
                        if (objs[i].name == name)
                            return objs[i];
                    }
                }
            }

            return null;
        }

        //partialMatch:是否对name采用部分匹配
        public static Transform[] FindAllByName(string name, bool partialMatch = false)
        {
            Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>() as Transform[];
            List<Transform> returnList = new List<Transform>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
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

            return returnList.ToArray();
        }

        #endregion

        #region ByType

        /// <summary>
        /// 在场景中查找给定类型的所有对象
        /// 返回找到的第一个该类型对象
        /// 该方法可以找到隐藏的对象
        /// </summary>
        public static T FindByType<T>() where T : Component
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    return objs[i];
                }
            }

            return null;
        }

        public static T[] FindAllByType<T>() where T : Component
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            List<T> returnList = new List<T>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    returnList.Add(objs[i]);
                }
            }

            return returnList.ToArray();
        }

        #endregion

        #region ByTypeAndName

        /// <summary>
        /// 在场景中查找给定类型的所有对象
        /// 返回找到与给定名字相同的第一个该类型对象
        /// 该方法可以找到隐藏的对象
        /// </summary>
        public static T FindByTypeAndName<T>(string name, bool partialMatch = false) where T : Component
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (partialMatch)
                    {
                        if (objs[i].name.Contains(name))
                            return objs[i];
                    }
                    else
                    {
                        if (objs[i].name == name)
                            return objs[i];
                    }
                }
            }

            return null;
        }


        public static T[] FindAllByTypeAndName<T>(string name, bool partialMatch = false) where T : Component
        {
            T[] objs = Resources.FindObjectsOfTypeAll<T>() as T[];
            List<T> returnList = new List<T>();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    if (partialMatch)
                    {
                        if (objs[i].name.Contains(name))
                            returnList.Add(objs[i]);
                    }
                    else
                    {
                        if (objs[i].name == name)
                            returnList.Add(objs[i]);
                    }
                }
            }

            return returnList.ToArray();
        }

        #endregion


        // 在给定对象父亲中查找给定类型的对象
        //(该方法可以找到隐藏的对象)
        public static T GetComponentInParentEx<T>(this GameObject gameObject)
        {
            T component = default;
            Transform transform = gameObject.transform.parent;

            while (transform != null)
            {
                component = transform.GetComponent<T>();
                if (component != null) //找到了
                    break;
                transform = transform.parent;
            }

            return component;
        }
    }
}