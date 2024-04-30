
namespace PluginHub.Runtime
{
    //新一代单例模式：不继承自MonoBehaviour的单例模式
    public class Singleton<T> where T : new()
    {
        protected Singleton()
        {
        }

        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }

                return _instance;
            }
        }
    }
}