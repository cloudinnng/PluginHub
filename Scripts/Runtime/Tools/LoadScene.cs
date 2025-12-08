using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginHub.Runtime
{
    public class LoadScene : MonoBehaviour
    {
        public int sceneIndex = 1;
        public LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
        void Start()
        {
            SceneManager.LoadScene(sceneIndex, loadSceneMode);
        }

    }
}
