using UnityEngine;

namespace PluginHub.Runtime
{
    public class DisableOnLoad : MonoBehaviour
    {
        void Start()
        {
            gameObject.SetActive(false);
        }
    }
}
