namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Anything that can be hit: Health, HitZone (forwards to Health), destructible props.
    /// Weapons find it with collider.GetComponent&lt;IDamageable&gt;().
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <returns>Damage actually applied (0 if dead, invulnerable, or amount was not positive).</returns>
        float TakeDamage(in DamageInfo damage);
    }
}
