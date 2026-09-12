using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Plays every sound the game produces. Mirrors how SpriteFactory hands out art:
    /// GameBootstrap assigns AudioLibrary to Sfx.Assets once at startup, then any script
    /// calls Sfx.Play(sounds.someClip) whenever its event happens. A null/unassigned
    /// clip slot just stays silent — nothing throws, nothing else about the game changes.
    /// </summary>
    public static class Sfx
    {
        /// <summary>Assigned once by GameBootstrap before anything else plays a sound.</summary>
        public static AudioLibrary Assets;

        static AudioSource oneShotSource;
        static AudioSource musicSource;

        static AudioSource EnsureOneShotSource()
        {
            if (oneShotSource != null) return oneShotSource;
            var go = new GameObject("Sfx_OneShot");
            Object.DontDestroyOnLoad(go);
            oneShotSource = go.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            return oneShotSource;
        }

        static AudioSource EnsureMusicSource()
        {
            if (musicSource != null) return musicSource;
            var go = new GameObject("Sfx_Music");
            Object.DontDestroyOnLoad(go);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            return musicSource;
        }

        /// <summary>Fire-and-forget one-shot. Empty slot in the Inspector = silence, same as
        /// leaving a sprite slot empty keeps the procedural shape instead of throwing.</summary>
        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            EnsureOneShotSource().PlayOneShot(clip, volume);
        }

        /// <summary>Starts (or keeps playing) the looping background track. Safe to call
        /// repeatedly - it won't restart a clip that's already playing.</summary>
        public static void PlayMusic(AudioClip clip, float volume)
        {
            var src = EnsureMusicSource();
            if (clip == null) { src.Stop(); return; }
            if (src.clip == clip && src.isPlaying)
            {
                src.volume = volume;
                return;
            }
            src.clip = clip;
            src.volume = volume;
            src.Play();
        }

        public static void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }
    }
}
