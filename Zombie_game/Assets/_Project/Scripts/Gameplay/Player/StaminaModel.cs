using System;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Pure stamina logic for sprinting. Drains while sprinting; after RegenDelay without sprinting it regenerates.
    /// Running dry sets Exhausted: sprint stays blocked until stamina reaches ResumeFraction of max.
    /// </summary>
    public sealed class StaminaModel
    {
        public float Max { get; }
        public float Current { get; private set; }
        public float DrainPerSecond { get; }
        public float RegenPerSecond { get; }
        public float RegenDelay { get; }
        public float ResumeFraction { get; }

        public bool Exhausted { get; private set; }
        public bool CanSprint => !Exhausted && Current > 0f;
        public float Normalized => Current / Max;

        private float _timeSinceUse;

        public StaminaModel(float max, float drainPerSecond, float regenPerSecond, float regenDelay, float resumeFraction)
        {
            if (max <= 0f) throw new ArgumentOutOfRangeException(nameof(max));
            Max = max;
            Current = max;
            DrainPerSecond = Math.Max(0f, drainPerSecond);
            RegenPerSecond = Math.Max(0f, regenPerSecond);
            RegenDelay = Math.Max(0f, regenDelay);
            ResumeFraction = Math.Max(0f, Math.Min(1f, resumeFraction));
            _timeSinceUse = RegenDelay;
        }

        /// <param name="sprinting">True if the player is trying to sprint this frame.</param>
        /// <returns>True if sprinting is allowed this frame.</returns>
        public bool Tick(float deltaTime, bool sprinting)
        {
            if (deltaTime <= 0f) return sprinting && CanSprint;

            if (sprinting && CanSprint)
            {
                Current = Math.Max(0f, Current - DrainPerSecond * deltaTime);
                _timeSinceUse = 0f;
                if (Current <= 0f) Exhausted = true;
                return true;
            }

            _timeSinceUse += deltaTime;
            if (_timeSinceUse >= RegenDelay && Current < Max)
            {
                Current = Math.Min(Max, Current + RegenPerSecond * deltaTime);
            }
            if (Exhausted && Current >= Max * ResumeFraction) Exhausted = false;
            return false;
        }

        public void Refill()
        {
            Current = Max;
            Exhausted = false;
        }
    }
}
