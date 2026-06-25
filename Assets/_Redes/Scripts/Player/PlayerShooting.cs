using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Player
{
    /// <summary>
    /// Shooting (one of the "seen in class" mechanics).
    /// Spawns Projectile NetworkObjects on the server (Host architecture).
    /// Cooperates with AmmoSystem (extra mechanic): cannot fire with 0 ammo.
    /// Logic is implemented by another agent.
    /// </summary>
    public class PlayerShooting : NetworkBehaviour
    {
        [Header("Refs (assigned by the Prefab tool)")]
        [SerializeField] private Transform _muzzle;        // Where bullets spawn.
        [SerializeField] private NetworkObject _projectilePrefab; // Bullet prefab.
        [SerializeField] private AmmoSystem _ammo;         // Sibling system.
        [SerializeField] private GameEventBus _eventBus;   // Global event bus.

        [Header("Tuning")]
        [SerializeField] private int _damage = GameConstants.DEFAULT_BULLET_DAMAGE;

        [Networked] public int ShootCount { get; set; }
        [Networked] public TickTimer ShootTimer { get; set; }

        public bool IsShooting => !ShootTimer.ExpiredOrNotRunning(Runner);

        private PlayerEventBus _playerEventBus;

        private void Awake()
        {
            _playerEventBus = GetComponent<PlayerEventBus>();
        }

        /// <summary>Called from the player's input tick when the fire button is pressed.</summary>
        public void Fire()
        {
            if (Object.HasStateAuthority)
            {
                if (_ammo != null && !_ammo.TryConsume())
                {
                    return;
                }

                if (_projectilePrefab != null && _muzzle != null)
                {
                    PlayerRef ownerRef = Object.InputAuthority;
                    var bullet = Runner.Spawn(_projectilePrefab, _muzzle.position, _muzzle.rotation, PlayerRef.None, (runner, obj) => {
                        var proj = obj.GetComponent<Combat.Projectile>();
                        if (proj != null)
                        {
                            proj.Owner = ownerRef;
                        }
                    });
                }

                ShootCount++;
                ShootTimer = TickTimer.CreateFromSeconds(Runner, 0.2f);
                if (_eventBus != null) _eventBus.TriggerPlayerShooting(Object.InputAuthority);
                if (_playerEventBus != null) _playerEventBus.TriggerShoot();
                RedesLog.Info(RedesLog.COMBAT, $"[Shooting] Jugador {Object.InputAuthority} disparó. Balas: {_ammo?.CurrentAmmo}. Pos: {_muzzle.position}");
            }
        }
    }
}
