using System.Collections.Generic;
using UnityEngine;

namespace Cloudinnng.CFramework
{
    //使用的时候传入声音文件相对与Resources目录的相对路径,不需要后缀名
    //AudioManager.Instance.PlaySound("Sound/Atk");
    public class AudioManager3D : SingletonMonoBehaviour<AudioManager3D>
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

        public void PlayBGM(string bgmPath)
        {
            bgmAudioSource.clip = GetClip(bgmPath);
            bgmAudioSource.Play();
        }

        public void StopBGM()
        {
            bgmAudioSource.Stop();
            bgmAudioSource.clip = null;
        }

        public void Play3DSound(string soundPath, Vector3 pos)
        {
            AudioClip cilp = GetClip(soundPath);
            AudioSource.PlayClipAtPoint(cilp, pos);
        }

        public void Play3DStopableSound(string soundPath, GameObject gameObject)
        {
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = GetClip(soundPath);
            audioSource.spatialBlend = 1; //3D
            audioSource.loop = true;
            audioSource.Play();
        }

        public void StopSound(GameObject gameObject)
        {
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource)
            {
                audioSource.Stop();
            }
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