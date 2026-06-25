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
            RedesLog.Info(RedesLog.COMBAT, $"[Bullet Spawned] ObjetoId: {Object.Id}, HasStateAuthority: {Object.HasStateAuthority}, Owner: {Owner}, Posición: {transform.position}");
            
            if (Object.HasStateAuthority)
            {
                Life = TickTimer.CreateFromSeconds(Runner, _lifeTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // Only the server/Host (State Authority) handles movement, collision logic and expiration despawning
            if (!Object.HasStateAuthority) return;

            // Move forward on State Authority (Host)
            transform.position += transform.forward * _speed * Runner.DeltaTime;

            if (Life.Expired(Runner))
            {
                RedesLog.Info(RedesLog.COMBAT, $"[Bullet Expired] ObjetoId: {Object.Id} expiró y se despawnea en tick {Runner.Tick}.");
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

                    RedesLog.Info(RedesLog.COMBAT, $">> Projectile.Hit: owner={Owner} target={hit.name} damage={_damage}");
                    try
                    {
                        RedesLog.Info(RedesLog.COMBAT, $">> Projectile: [IN] Calling damageable.TakeDamage(amount={_damage}, Owner={Owner})");
                        damageable.TakeDamage(_damage, Owner);
                        RedesLog.Info(RedesLog.COMBAT, $">> Projectile: [OUT] Call to damageable.TakeDamage completed successfully.");
                    }
                    catch (System.Exception ex)
                    {
                        RedesLog.Error(RedesLog.COMBAT, $">> Projectile: [ERROR] Exception during TakeDamage call: {ex.Message}\n{ex.StackTrace}");
                    }
                    
                    try
                    {
                        RedesLog.Info(RedesLog.COMBAT, $">> Projectile: [IN] Calling Runner.Despawn for bullet {Object.Id}");
                        Runner.Despawn(Object);
                        RedesLog.Info(RedesLog.COMBAT, $">> Projectile: [OUT] Despawn completed successfully.");
                    }
                    catch (System.Exception ex)
                    {
                        RedesLog.Error(RedesLog.COMBAT, $">> Projectile: [ERROR] Exception during Despawn call: {ex.Message}\n{ex.StackTrace}");
                    }
                    return;
                }
                else if (hit.gameObject.layer == 6 || hit.CompareTag("Obstacle"))
                {
                    // Hit obstacle
                    var matchNet = FindFirstObjectByType<MatchNetworkController>();
                    if (matchNet != null)
                    {
                        // Calculate approximate contact normal or direction
                        Vector3 normal = (transform.position - hit.bounds.center).normalized;
                        matchNet.RpcPlaySparkVfx(transform.position, normal);
                    }

                    Runner.Despawn(Object);
                    return;
                }
            }
        }
    }
}
