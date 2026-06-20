using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Combat
{
    /// <summary>
    /// Bullet as a Network Object (assignment requirement: Network Object).
    /// Spawned by PlayerShooting on the server. Moves forward and, on hit,
    /// calls IDamageable.TakeDamage (depends on the abstraction, not on the
    /// player - SOLID/DIP).
    /// Logic is implemented by another agent.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class Projectile : NetworkBehaviour
    {
        [Header("Tuning")]
        [SerializeField] private float _speed = 12f;
        [SerializeField] private int _damage = GameConstants.DEFAULT_BULLET_DAMAGE;
        [SerializeField] private float _lifeTime = 3f;

        // Who fired this bullet (so the hit can credit the winner).
        [Networked] public PlayerRef Owner { get; set; }

        // Server-side despawn timer.
        [Networked] public TickTimer Life { get; set; }

        public override void Spawned()
        {
            // TODO (other agent): if (Object.HasStateAuthority) Life = TickTimer.CreateFromSeconds(Runner, _lifeTime);
        }

        public override void FixedUpdateNetwork()
        {
            // TODO (other agent): move forward; on overlap with IDamageable -> TakeDamage(_damage, Owner);
            // then Runner.Despawn(Object). Despawn when Life.Expired(Runner).
        }
    }
}
