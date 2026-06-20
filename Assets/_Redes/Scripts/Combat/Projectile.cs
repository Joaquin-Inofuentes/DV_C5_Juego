using Fusion;
using UnityEngine;
using Redes.Core;
using NetworkPlayer = Redes.Player.NetworkPlayer;

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

        public int Damage { get => _damage; set => _damage = value; }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                Life = TickTimer.CreateFromSeconds(Runner, _lifeTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            transform.position += transform.forward * _speed * Runner.DeltaTime;

            if (Life.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            Collider[] hits = new Collider[5];
            int hitCount = Runner.GetPhysicsScene().OverlapSphere(transform.position, 0.3f, hits, -1, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (hit == null || hit.isTrigger) continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    var netPlayer = hit.GetComponentInParent<NetworkPlayer>();
                    if (netPlayer != null && netPlayer.Object.InputAuthority == Owner)
                    {
                        continue;
                    }

                    damageable.TakeDamage(_damage, Owner);
                    Runner.Despawn(Object);
                    return;
                }
            }
        }
    }
}
