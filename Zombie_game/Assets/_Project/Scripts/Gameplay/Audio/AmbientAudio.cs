using UnityEngine;

namespace Game.Gameplay.Audio
{
    /// <summary>Plays a selected ambience variant as a seamless loop for the lifetime of this object.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientAudio : MonoBehaviour
    {
        [SerializeField] private SoundCue _cue;
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.loop = true;
        }

        private void Start()
        {
            if (_cue == null || !_cue.HasClips) return;
            _source.clip = _cue.PickClip();
            _source.volume = _cue.Volume;
            _source.Play();
        }
    }
}
