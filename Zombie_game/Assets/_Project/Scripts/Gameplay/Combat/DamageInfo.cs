using UnityEngine;

namespace Game.Gameplay.Combat
{
    public enum DamageType
    {
        Generic = 0,
        Bullet = 1,
        Melee = 2,
        Explosive = 3,
        Fire = 4,
        Acid = 5,
        Environment = 6,
    }

    public enum HitZoneType
    {
        Body = 0,
        Head = 1,
        Limb = 2,
    }

    /// <summary>
    /// One hit. Immutable value type, passed with `in` to avoid copies.
    /// Amount is the final damage after hit-zone multipliers (HitZone applies them).
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly HitZoneType Zone;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;

        /// <summary>Who caused the damage (player, enemy, trap). May be null.</summary>
        public readonly GameObject Instigator;

        public DamageInfo(float amount, DamageType type = DamageType.Generic, GameObject instigator = null,
            Vector3 point = default, Vector3 direction = default, HitZoneType zone = HitZoneType.Body)
        {
            Amount = amount;
            Type = type;
            Instigator = instigator;
            Point = point;
            Direction = direction;
            Zone = zone;
        }

        public bool IsHeadshot => Zone == HitZoneType.Head;

        public DamageInfo WithAmount(float amount) =>
            new DamageInfo(amount, Type, Instigator, Point, Direction, Zone);

        public DamageInfo WithZone(HitZoneType zone, float multiplier) =>
            new DamageInfo(Amount * multiplier, Type, Instigator, Point, Direction, zone);

        public override string ToString() => $"{Amount:0.##} {Type} ({Zone})";
    }
}
