namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Timing of one melee attack: windup (telegraph) -> hit moment -> recovery -> finished.
    /// Pure C#: the enemy decides what "hit" means (range check, damage).
    /// </summary>
    public sealed class MeleeAttackCycle
    {
        public enum Phase { Idle, Windup, Recovery }

        public enum Result { None, HitMoment, Finished }

        public float Windup { get; private set; }
        public float Recovery { get; private set; }
        public Phase Current { get; private set; } = Phase.Idle;
        public bool IsBusy => Current != Phase.Idle;

        private float _timer;

        public MeleeAttackCycle(float windup, float recovery)
        {
            Configure(windup, recovery);
        }

        public void Configure(float windup, float recovery)
        {
            Windup = windup < 0f ? 0f : windup;
            Recovery = recovery < 0f ? 0f : recovery;
        }

        /// <returns>False if an attack is already in progress.</returns>
        public bool Begin()
        {
            if (IsBusy) return false;
            Current = Phase.Windup;
            _timer = Windup;
            return true;
        }

        public void Cancel()
        {
            Current = Phase.Idle;
            _timer = 0f;
        }

        /// <summary>Advance time. Reports HitMoment once per attack and Finished at the end of recovery.</summary>
        public Result Tick(float deltaTime)
        {
            if (Current == Phase.Idle) return Result.None;

            _timer -= deltaTime;
            if (_timer > 0f) return Result.None;

            if (Current == Phase.Windup)
            {
                Current = Phase.Recovery;
                _timer += Recovery; // carry over the remainder so long frames don't stretch attacks
                return Result.HitMoment;
            }

            Current = Phase.Idle;
            _timer = 0f;
            return Result.Finished;
        }
    }
}
