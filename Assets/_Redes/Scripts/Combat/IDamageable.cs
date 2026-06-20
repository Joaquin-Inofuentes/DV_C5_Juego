using Fusion;

namespace Redes.Combat
{
    /// <summary>
    /// SOLID (Interface Segregation + Dependency Inversion):
    /// Anything that can be hit implements this. Projectile depends on this
    /// abstraction, NOT on PlayerHealth directly, so new damageable things
    /// (crates, turrets) can be added without touching combat code.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage. 'attacker' lets the match system know who scored the hit
        /// (needed for the win/lose notification).
        /// Logic is implemented by another agent.
        /// </summary>
        void TakeDamage(int amount, PlayerRef attacker);

        bool IsAlive { get; }
    }
}
