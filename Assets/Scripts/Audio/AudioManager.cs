using System.Collections.Generic;
using UnityEngine;

namespace TrashRoyale.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        AudioSource _musicSource;
        AudioSource _sfxSource;
        public float SfxVolume = 0.85f;
        public float MusicVolume = 0.5f;

        public static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AudioManager>();
            I._musicSource = go.AddComponent<AudioSource>();
            I._musicSource.loop = true;
            I._musicSource.volume = I.MusicVolume;
            I._sfxSource = go.AddComponent<AudioSource>();
            I._sfxSource.spatialBlend = 0f;
            I._sfxSource.volume = I.SfxVolume;
            I.Preload();
        }

        void Preload()
        {
            var all = Resources.LoadAll<AudioClip>("Sounds");
            foreach (var c in all)
            {
                _clips[c.name] = c;
            }
            Debug.Log($"[Audio] Preloaded {_clips.Count} clips");
        }

        public static void PlayMusic(string id)
        {
            if (I == null) Boot();
            if (I._clips.TryGetValue(id, out var clip))
            {
                if (I._musicSource.clip != clip)
                {
                    I._musicSource.clip = clip;
                    I._musicSource.Play();
                }
            }
        }

        public static void StopMusic()
        {
            if (I == null) return;
            I._musicSource.Stop();
        }

        public static void PlayOneShot(string id, Vector3 pos)
        {
            if (I == null) Boot();
            if (string.IsNullOrEmpty(id)) return;
            if (!I._clips.TryGetValue(id, out var clip)) return;
            I._sfxSource.PlayOneShot(clip, I.SfxVolume);
        }

        public static void PlaySfx(string id)
        {
            PlayOneShot(id, Vector3.zero);
        }
    }
}
