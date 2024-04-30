using System.Collections.Generic;
using UnityEngine;

namespace PluginHub.Runtime
{
    //使用的时候传入声音文件相对与Resources目录的相对路径,不需要后缀名
    //AudioManager2D.Instance.PlaySound("Sounds/Atk");
    public class AudioManager2D : SingletonMonoBehaviour<AudioManager2D>
    {
        private AudioSource bgmAudioSource;
        private AudioSource sdAudioSource;

        private Dictionary<string, AudioClip> _audioClipsDic = new Dictionary<string, AudioClip>();

        protected void Awake()
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;

            sdAudioSource = gameObject.AddComponent<AudioSource>();
            sdAudioSource.playOnAwake = false;
            sdAudioSource.loop = false;
        }

        public void BGM(string bgmPath ,float volume = 1)
        {
            if (string.IsNullOrEmpty(bgmPath))
                bgmAudioSource.Stop();
            else
            {
                bgmAudioSource.clip = GetClip(bgmPath);
                bgmAudioSource.volume = volume;
                bgmAudioSource.Play();
            }
        }

        public float Play2DSound(string soundPath)
        {
            AudioClip cilp = GetClip(soundPath);
            sdAudioSource.PlayOneShot(cilp);
            return cilp.length;
        }

        public void Stop2DSound()
        {
            sdAudioSource.Stop();
        }

        private AudioClip GetClip(string soundPath)
        {
            if (_audioClipsDic.ContainsKey(soundPath))
            {
                return _audioClipsDic[soundPath];
            }

            AudioClip audioClip = Resources.Load<AudioClip>(soundPath);
            _audioClipsDic.Add(soundPath, audioClip);
            return audioClip;
        }
    }
}