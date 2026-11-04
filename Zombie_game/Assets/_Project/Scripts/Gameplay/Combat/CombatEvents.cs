using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>Raised on the global EventBus when any Health takes damage.</summary>
    public readonly struct DamageTakenEvent : IGameEvent
    {
        public readonly Health Target;
        public readonly DamageInfo Damage;
        public readonly float Applied;

        public DamageTakenEvent(Health target, in DamageInfo damage, float applied)
        {
            Target = target;
            Damage = damage;
            Applied = applied;
        }
    }

    /// <summary>Raised on the global EventBus when any Health dies. Economy, waves and stats listen to this.</summary>
    public readonly struct EntityDiedEvent : IGameEvent
    {
        public readonly Health Target;
        public readonly DamageInfo KillingBlow;

        public EntityDiedEvent(Health target, in DamageInfo killingBlow)
        {
            Target = target;
            KillingBlow = killingBlow;
        }

        public GameObject Instigator => KillingBlow.Instigator;
    }
}
