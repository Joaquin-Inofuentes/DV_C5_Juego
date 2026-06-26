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

        // [Header("Tuning")]
        // [SerializeField] private int _damage = GameConstants.DEFAULT_BULLET_DAMAGE; // Removed unused field

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
            try
            {
                if (!Object.HasStateAuthority) 
                {
                    // Only State Authority spawns objects
                    return;
                }

                RedesLog.Info(RedesLog.COMBAT, $"[PlayerShooting] Se recibio el input de disparar en State Authority. Jugador: {Object.InputAuthority}");

                if (_ammo != null && !_ammo.TryConsume())
                {
                    if (!_ammo.IsReloading)
                    {
                        RedesLog.Info(RedesLog.AMMO, $"[Auto-Reload] Jugador {Object.InputAuthority} intentó disparar sin munición → recarga automática iniciada");
                        _ammo.StartReload();
                    }
                    return;
                }

                if (_projectilePrefab == null)
                {
                    RedesLog.Error(RedesLog.COMBAT, $"[PlayerShooting] ERROR: _projectilePrefab es nulo en el jugador {Object.InputAuthority}. ¡Falta asignar el prefab en el inspector!");
                }

                if (_muzzle == null)
                {
                    RedesLog.Error(RedesLog.COMBAT, $"[PlayerShooting] ERROR: _muzzle es nulo en el jugador {Object.InputAuthority}. ¡Falta asignar el transform del origen de disparo!");
                }

                if (_projectilePrefab != null && _muzzle != null)
                {
                    RedesLog.Info(RedesLog.COMBAT, $"[PlayerShooting] Intentando instanciar bala. Prefab: {_projectilePrefab.name}, Muzzle Pos: {_muzzle.position}");
                    
                    PlayerRef ownerRef = Object.InputAuthority;
                    var bullet = Runner.Spawn(_projectilePrefab, _muzzle.position, _muzzle.rotation, PlayerRef.None, (runner, obj) => {
                        var proj = obj.GetComponent<Combat.Projectile>();
                        if (proj != null)
                        {
                            proj.Owner = ownerRef;
                        }
                    });

                    if (bullet != null)
                    {
                        RedesLog.Info(RedesLog.COMBAT, $"[PlayerShooting] EXITO: Se creo la bala {bullet.Id}. Se disparo con owner {ownerRef}");
                    }
                    else
                    {
                        RedesLog.Error(RedesLog.COMBAT, $"[PlayerShooting] FALLO: Runner.Spawn devolvió nulo. No se pudo crear la bala.");
                    }
                }

                ShootCount++;
                ShootTimer = TickTimer.CreateFromSeconds(Runner, 0.2f);

                if (_eventBus != null) _eventBus.TriggerPlayerShooting(Object.InputAuthority);
                if (_playerEventBus != null) _playerEventBus.TriggerShoot();
                
                string muzzlePosStr = _muzzle != null ? _muzzle.position.ToString() : "NULO";
                RedesLog.Info(RedesLog.COMBAT, $"[PlayerShooting] Proceso de disparo completado exitosamente. Jugador {Object.InputAuthority} disparó. Balas: {_ammo?.CurrentAmmo}. Pos: {muzzlePosStr}");
            }
            catch (System.Exception e)
            {
                RedesLog.Error(RedesLog.COMBAT, $"[PlayerShooting] ERROR FATAL en Fire(): {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
