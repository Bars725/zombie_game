using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Marks the player so enemies can find their target without FindObjectOfType.
    /// Single-player: one current anchor. Put it on the player root next to Health.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class PlayerAnchor : MonoBehaviour
    {
        public static PlayerAnchor Current { get; private set; }

        public Health Health { get; private set; }

        /// <summary>Point enemies aim for (feet position works with NavMesh).</summary>
        public Vector3 Position => transform.position;

        public bool IsAlive => Health != null && Health.IsAlive;

        private void Awake()
        {
            Health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (Current != null && Current != this)
            {
                Debug.LogWarning("Two PlayerAnchors are active; the newest one wins.", this);
            }
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;
    }
}
