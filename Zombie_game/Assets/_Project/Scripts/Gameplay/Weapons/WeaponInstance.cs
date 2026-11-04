using System;

namespace Game.Gameplay.Weapons
{
    /// <summary>
    /// Runtime state of one carried weapon: magazine, reserve, fire cooldown, reload.
    /// Pure C# (no MonoBehaviour), driven by PlayerWeaponController. Never modifies the definition.
    ///
    /// Fire rate is quantized to frames (at most one shot per frame per weapon); fine for the MVP.
    /// </summary>
    public sealed class WeaponInstance
    {
        public WeaponDefinition Definition { get; }
        public int Magazine { get; private set; }
        public int Reserve { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadRemaining { get; private set; }
        public float CooldownRemaining { get; private set; }

        public bool IsMagazineEmpty => Magazine <= 0;
        public bool HasAnyAmmo => Magazine > 0 || Reserve > 0;
        public bool CanFire => !IsReloading && Magazine > 0 && CooldownRemaining <= 0f;
        public bool CanReload => !IsReloading && Magazine < Definition.MagazineSize && Reserve > 0;
        public float ReloadProgress => IsReloading && Definition.ReloadTime > 0f
            ? 1f - ReloadRemaining / Definition.ReloadTime
            : 0f;

        public event Action AmmoChanged;
        public event Action ReloadStarted;
        public event Action ReloadFinished;

        public WeaponInstance(WeaponDefinition definition)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            Magazine = definition.MagazineSize;
            Reserve = definition.StartingReserve;
        }

        /// <summary>Consumes one round and starts the cooldown. Returns false if it cannot fire now.</summary>
        public bool TryFire()
        {
            if (!CanFire) return false;
            Magazine--;
            CooldownRemaining = Definition.SecondsPerShot;
            AmmoChanged?.Invoke();
            return true;
        }

        public bool TryStartReload()
        {
            if (!CanReload) return false;
            IsReloading = true;
            ReloadRemaining = Definition.ReloadTime;
            ReloadStarted?.Invoke();
            if (ReloadRemaining <= 0f) FinishReload();
            return true;
        }

        /// <summary>Weapon switch, sprint-cancel etc. Ammo is not lost.</summary>
        public void CancelReload()
        {
            IsReloading = false;
            ReloadRemaining = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (CooldownRemaining > 0f) CooldownRemaining = Math.Max(0f, CooldownRemaining - deltaTime);

            if (IsReloading)
            {
                ReloadRemaining -= deltaTime;
                if (ReloadRemaining <= 0f) FinishReload();
            }
        }

        /// <returns>Rounds actually added to reserve (capped by MaxReserve).</returns>
        public int AddReserve(int amount)
        {
            if (amount <= 0) return 0;
            int added = Math.Min(amount, Definition.MaxReserve - Reserve);
            if (added <= 0) return 0;
            Reserve += added;
            AmmoChanged?.Invoke();
            return added;
        }

        /// <summary>Full magazine and full reserve (ammo station, max-ammo pickup).</summary>
        public void RefillAll()
        {
            CancelReload();
            Magazine = Definition.MagazineSize;
            Reserve = Definition.MaxReserve;
            AmmoChanged?.Invoke();
        }

        private void FinishReload()
        {
            int needed = Definition.MagazineSize - Magazine;
            int taken = Math.Min(needed, Reserve);
            Magazine += taken;
            Reserve -= taken;
            IsReloading = false;
            ReloadRemaining = 0f;
            AmmoChanged?.Invoke();
            ReloadFinished?.Invoke();
        }
    }
}
