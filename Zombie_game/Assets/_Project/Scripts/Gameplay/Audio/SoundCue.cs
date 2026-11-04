using UnityEngine;

namespace Game.Gameplay.Audio
{
    /// <summary>One logical sound (e.g. "pistol shot"): random clip from variants, volume, pitch jitter, 2D/3D.</summary>
    [CreateAssetMenu(menuName = "Game/Audio/Sound Cue", fileName = "SC_New")]
    public sealed class SoundCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;
        [SerializeField, Range(0.5f, 2f)] private float _pitchMin = 0.95f;
        [SerializeField, Range(0.5f, 2f)] private float _pitchMax = 1.05f;
        [Tooltip("0 = 2D (player's own sounds), 1 = 3D (world)")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 1f;
        [SerializeField, Min(1f)] private float _maxDistance = 40f;

        public float Volume => _volume;
        public float SpatialBlend => _spatialBlend;
        public float MaxDistance => _maxDistance;
        public bool HasClips => _clips != null && _clips.Length > 0;

        public AudioClip PickClip() => HasClips ? _clips[Random.Range(0, _clips.Length)] : null;
        public float PickPitch() => Random.Range(Mathf.Min(_pitchMin, _pitchMax), Mathf.Max(_pitchMin, _pitchMax));

        internal void Initialize(AudioClip[] clips, float volume, float spatialBlend, float maxDistance = 40f)
        {
            _clips = clips;
            _volume = volume;
            _spatialBlend = spatialBlend;
            _maxDistance = maxDistance;
        }
    }
}
