using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 2024年1月9日
    /// FindSingleton正式更名为SceneSingleton
    /// 更贴切，因为它存在于场景中。
    ///
    /// 该单例模式（懒汉式单例）是在第一次调用的时候使用FindObjectsOfType<T>()来查找场景中的对象实现的。
    /// 所以该单例模式基于 MonoBehaviour 和 GameObject。
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                GetInstance();
                return _instance;
            }
        }

        private static void GetInstance()
        {
            if (_instance == null)
            {
                //这里用Resources.FindObjectsOfTypeAll<T>();会找到资产中的对象，而我只需要场景中的对象
                T[] tObjInScene = Object.FindObjectsOfType<T>();
                if (tObjInScene.Length > 0)
                {
                    _instance = tObjInScene[0];
                    if (tObjInScene.Length > 1)
                    {
                        Debug.Log($"{_instance.GetType()} 存在多于一个实例，请检查你的场景中是否挂有多个该组件。");
                        for (int i = 0; i < tObjInScene.Length; i++)
                            Debug.Log(tObjInScene[i].gameObject.name, tObjInScene[i].gameObject);
                    }
                }
                else
                {
                    _instance = null;
                }
            }
        }
    }
}