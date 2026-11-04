using System;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Pure health logic, no Unity dependencies: easy to unit-test and reuse for player and enemies.
    ///
    /// Segmented regeneration (player): after RegenDelay seconds without damage, health regenerates
    /// at RegenPerSecond, but only up to the top of the current segment. With max 100 and
    /// segmentFraction 0.25: 60 regenerates to 75, 50 stays 50. Medkits (Heal) restore beyond that.
    /// RegenPerSecond 0 disables regeneration (enemies). segmentFraction 0 means regen up to max.
    /// </summary>
    public sealed class HealthModel
    {
        private const float Epsilon = 0.0001f;

        public float Max { get; private set; }
        public float Current { get; private set; }
        public float RegenPerSecond { get; }
        public float RegenDelay { get; }
        public float SegmentFraction { get; }

        public bool IsAlive => Current > 0f;
        public float Normalized => Max > 0f ? Current / Max : 0f;

        /// <summary>(current, max) after any change.</summary>
        public event Action<float, float> Changed;

        /// <summary>Fired once when health reaches 0.</summary>
        public event Action Died;

        private float _timeSinceDamage;

        public HealthModel(float max, float regenPerSecond = 0f, float regenDelay = 0f, float segmentFraction = 0f)
        {
            if (max <= 0f) throw new ArgumentOutOfRangeException(nameof(max), "Max health must be positive.");
            if (regenPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(regenPerSecond));
            if (regenDelay < 0f) throw new ArgumentOutOfRangeException(nameof(regenDelay));
            if (segmentFraction < 0f || segmentFraction > 1f)
                throw new ArgumentOutOfRangeException(nameof(segmentFraction), "Must be in [0, 1].");

            Max = max;
            Current = max;
            RegenPerSecond = regenPerSecond;
            RegenDelay = regenDelay;
            SegmentFraction = segmentFraction;
            _timeSinceDamage = regenDelay;
        }

        /// <summary>Health the player regenerates up to without a medkit.</summary>
        public float RegenCeiling
        {
            get
            {
                if (SegmentFraction <= 0f || SegmentFraction >= 1f) return Max;
                float segment = Max * SegmentFraction;
                // Epsilon keeps 50.00001 from counting as "inside" the next segment.
                int index = (int)Math.Ceiling((Current - Epsilon) / segment);
                if (index < 1) index = 1;
                return Math.Min(Max, index * segment);
            }
        }

        /// <returns>Damage actually applied.</returns>
        public float ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;

            float applied = Math.Min(amount, Current);
            Current -= applied;
            _timeSinceDamage = 0f;
            Changed?.Invoke(Current, Max);

            if (Current <= 0f)
            {
                Current = 0f;
                Died?.Invoke();
            }
            return applied;
        }

        /// <returns>Health actually restored. Dead entities cannot be healed (use Revive).</returns>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;

            float restored = Math.Min(amount, Max - Current);
            if (restored <= 0f) return 0f;
            Current += restored;
            Changed?.Invoke(Current, Max);
            return restored;
        }

        public void Kill()
        {
            if (!IsAlive) return;
            ApplyDamage(Current);
        }

        public void Revive(float normalizedHealth = 1f)
        {
            float target = Math.Max(Epsilon, Math.Min(1f, normalizedHealth)) * Max;
            Current = target;
            _timeSinceDamage = RegenDelay;
            Changed?.Invoke(Current, Max);
        }

        /// <summary>Changes max health (upgrades). Keeps the same percentage of health.</summary>
        public void SetMax(float newMax, bool keepRatio = true)
        {
            if (newMax <= 0f) throw new ArgumentOutOfRangeException(nameof(newMax));
            float ratio = Normalized;
            Max = newMax;
            Current = keepRatio ? ratio * newMax : Math.Min(Current, newMax);
            Changed?.Invoke(Current, Max);
        }

        public void Tick(float deltaTime)
        {
            if (!IsAlive || RegenPerSecond <= 0f || deltaTime <= 0f) return;

            _timeSinceDamage += deltaTime;
            if (_timeSinceDamage < RegenDelay) return;

            float ceiling = RegenCeiling;
            if (Current >= ceiling) return;

            Current = Math.Min(ceiling, Current + RegenPerSecond * deltaTime);
            Changed?.Invoke(Current, Max);
        }
    }
}
