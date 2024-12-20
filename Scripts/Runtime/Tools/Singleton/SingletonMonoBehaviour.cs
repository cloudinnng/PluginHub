using UnityEngine;

namespace PluginHub.Runtime
{
	/// <summary>
	/// Be aware this will not prevent a non singleton constructor
	/// 请注意，这不会阻止非单例构造函数
	///   such as `T myT = new T();`
	/// 例如`T myT = new T（）;`
	/// To prevent that, add `protected T () {}` to your singleton class.
	/// 为了防止这种情况，请将`protected T（）`添加到您的单例类中。
	///
	/// As a note, this is made as MonoBehaviour because we need Coroutines.
	/// 作为一个注释，这是作为MonoBehaviour，因为我们需要Coroutines。
	///
	/// wiki: http://wiki.unity3d.com/index.php/Singleton
	///
	/// 原则上，单例类仅在应用程序退出时销毁，不要手动销毁一个单例类
	///
	/// 我的备注：--------------------------------------------------
	/// 这种单例在第一次调用时会自动创建，并且会被标记为DontDestroyOnLoad，
	/// 所以这种单例可以不放在Scene中，直接在场景中的单例，使用FindSingleton
	///
	/// </summary>
	public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance;

		private static object _lock = new object();

		public static T Instance
		{
			get
			{
				if (applicationIsQuitting)
				{
					//这个实例已经在应用退出时销毁，并且不会再次创建 - 返回null
					Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
					                 "' already destroyed on application quit." +
					                 " Won't create again - returning null.");
					return null;
				}

				lock (_lock)
				{
					if (_instance == null)
					{
						_instance = FindObjectsByType<T>(FindObjectsSortMode.None)[0];

						if (FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
						{
							//出了点问题，这里有超过1个单例实例，重新打开场景可能会解决它。
							Debug.LogError("[Singleton] Something went really wrong " +
							               " - there should never be more than 1 singleton!" +
							               " Reopening the scene might fix it.");
							return _instance;
						}

						if (_instance == null)
						{
							GameObject singleton = new GameObject();
							_instance = singleton.AddComponent<T>();
							singleton.name = "(singleton) " + typeof(T).ToString();

							DontDestroyOnLoad(singleton);
							//一个这个类的实例在场景中被需要，所以创建一个并标记为DontDestroyOnLoad
							Debug.Log("[Singleton] An instance of " + typeof(T) +
							          " is needed in the scene, so '" + singleton +
							          "' was created with DontDestroyOnLoad.");
						}
						else
						{
							//使用已经创建的实例
							Debug.Log("[Singleton] Using instance already created: " + _instance.gameObject.name);

						}
					}

					return _instance;
				}
			}
		}

		private static bool applicationIsQuitting = false;

		/// <summary>
		/// When Unity quits, it destroys objects in a random order.
		/// 当Unity退出时，它会以随机顺序销毁对象。
		/// In principle, a Singleton is only destroyed when application quits.
		/// 原则上，Singleton仅在应用程序退出时销毁。
		/// If any script calls Instance after it have been destroyed,
		/// 如果任何脚本在销毁后调用Instance
		///   it will create a buggy ghost object that will stay on the Editor scene
		/// 它将创建一个将停留在编辑器场景中的错误鬼对象
		///   even after stopping playing the Application. Really bad!
		/// 即使在停止播放应用程序之后 特别糟糕！
		/// So, this was made to be sure we're not creating that buggy ghost object.
		/// 所以，这是为了确保我们不会创建那个错误的鬼对象。
		/// </summary>
		public virtual void OnDestroy()
		{
			applicationIsQuitting = true;
		}
	}
}