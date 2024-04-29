using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 2022年5月19日，新版Unity打包出现以下错误：
/// RuntimeInitializeOnLoadMethodAttribute: Is not allowed on a Generic class FindSingleton`1.InitFindSingleton 
/// 所以注释掉RuntimeInitializeOnLoadMethod属性，如果想用，就在子类里写一遍
/// 
/// 2021年5月6日更新，现在FindSingleton支持初始状态为隐藏
/// 
/// 使用FindObjectOfType的单例，寻找场景中唯一的类实例，
/// 这种单例必须最开始就被放到场景中，不会自动创建，
/// 若想要自动创建，使用另一种单例SingletonMonoBehaviour。
/// 这种单例可以随着场景销毁，销毁后后期出现可以自动获取到
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
[Obsolete("使用SceneSingleton代替")]
public class FindSingleton<T> : MonoBehaviour where T : MonoBehaviour
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

    //注意，如果是AfterSceneLoad的话，运行后会报错
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    // public static void InitFindSingleton()
    // {
    //     GetInstance();
    // }

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
                    Debug.Log($"{_instance.GetType()} 存在多于一个实例，请检查你的代码。");
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