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
        public float SfxVolume = 0.6f;
        public float MusicVolume = 0.5f;

        // Per-clip relative volume scaler [0..1]. Multiplied with SfxVolume
        // when the clip plays. Lets us turn down the LOUDEST hits (towers
        // shooting, melee swings, imposter shhh) without sacrificing the
        // quieter UX-feedback sounds (button clicks, deploy stings).
        // Anything not listed here uses 1.0f.
        static readonly Dictionary<string, float> ClipVolume = new Dictionary<string, float>
        {
            // Combat: per-shot SFX fire CONSTANTLY during a battle, so
            // even small overrides matter. Halve them.
            { "tower_shoot",    0.45f },
            { "attack_swing",   0.55f },
            { "tower_destroyed", 0.7f },
            { "explosion",      0.7f },
            // Voice lines: trim the loudest ones requested by the user
            // (the Among Us / imposter "sus" was peaking near clipping).
            { "sus",            0.55f },
            { "amongus_spawn",  0.55f },
            { "imposter_spawn", 0.55f },
            { "imposter_hut_spawn", 0.55f },
            // Pig: replace the original "groan" oink with a sharper
            // squeal clip; keep volume modest so swarm pigs aren't a
            // wall of noise.
            { "pig_oink",       0.7f },
            { "pig_squeal",     0.8f },
            // IShowSpeed scream is loud — trim it down a little so it
            // sits in the same range as other voice lines.
            { "speed_suuuui",   0.7f },
        };

        /// <summary>
        /// Resolves the playback volume for a clip id given an optional
        /// per-card override (CardData.sfxVolume). Order:
        ///   global SfxVolume × ClipVolume[id] × cardOverride (if &gt; 0).
        /// </summary>
        static float ResolveVolume(string id, float cardOverride)
        {
            float v = I.SfxVolume;
            if (!string.IsNullOrEmpty(id) && ClipVolume.TryGetValue(id, out var clipMul)) v *= clipMul;
            if (cardOverride > 0f) v *= Mathf.Clamp01(cardOverride);
            return Mathf.Clamp01(v);
        }

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
            PlayMusic(id, -1f);
        }

        public static void PlayMusic(string id, float volume)
        {
            if (I == null) Boot();
            if (I._clips.TryGetValue(id, out var clip))
            {
                if (I._musicSource.clip != clip)
                {
                    I._musicSource.clip = clip;
                    I._musicSource.volume = volume >= 0f ? volume : I.MusicVolume;
                    I._musicSource.Play();
                }
                else
                {
                    I._musicSource.volume = volume >= 0f ? volume : I.MusicVolume;
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
            PlayOneShot(id, pos, 0f);
        }

        /// <summary>
        /// Plays a one-shot SFX with an optional per-card volume override
        /// (e.g. CardData.sfxVolume). When 0 or negative, only the
        /// per-clip + global volumes are applied.
        /// </summary>
        public static void PlayOneShot(string id, Vector3 pos, float cardVolumeOverride)
        {
            if (I == null) Boot();
            if (string.IsNullOrEmpty(id)) return;
            if (!I._clips.TryGetValue(id, out var clip)) return;
            float v = ResolveVolume(id, cardVolumeOverride);
            if (v <= 0f) return;
            I._sfxSource.PlayOneShot(clip, v);
        }

        public static void PlaySfx(string id)
        {
            PlayOneShot(id, Vector3.zero, 0f);
        }
    }
}
