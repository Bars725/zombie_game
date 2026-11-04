using UnityEngine;

namespace Game.Gameplay.Audio
{
    /// <summary>
    /// Fire-and-forget sound effects from a small round-robin pool of AudioSources.
    /// Usage: SfxPlayer.Play(cue) for 2D, SfxPlayer.Play(cue, position) for 3D. Null cues are ignored.
    /// </summary>
    public static class SfxPlayer
    {
        private const int PoolSize = 24;
        private static AudioSource[] s_Sources;
        private static int s_Next;

        public static void Play(SoundCue cue) => PlayInternal(cue, null);
        public static void Play(SoundCue cue, Vector3 position) => PlayInternal(cue, position);

        private static void PlayInternal(SoundCue cue, Vector3? position)
        {
            if (cue == null || !cue.HasClips || !Application.isPlaying) return;
            EnsurePool();

            AudioSource source = s_Sources[s_Next];
            s_Next = (s_Next + 1) % PoolSize;

            bool spatial = position.HasValue && cue.SpatialBlend > 0f;
            source.transform.position = position ?? Vector3.zero;
            source.spatialBlend = spatial ? cue.SpatialBlend : 0f;
            source.maxDistance = cue.MaxDistance;
            source.pitch = cue.PickPitch();
            source.volume = cue.Volume;
            source.clip = cue.PickClip();
            source.Play();
        }

        private static void EnsurePool()
        {
            if (s_Sources != null && s_Sources[0] != null) return;
            var root = new GameObject("[SfxPlayer]");
            Object.DontDestroyOnLoad(root);
            s_Sources = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Sfx_" + i);
                go.transform.SetParent(root.transform, false);
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 2f;
                s_Sources[i] = s;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Sources = null;
            s_Next = 0;
        }
    }
}
