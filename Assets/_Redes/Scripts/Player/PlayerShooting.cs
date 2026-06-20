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

        [Header("Tuning")]
        [SerializeField] private int _damage = GameConstants.DEFAULT_BULLET_DAMAGE;

        /// <summary>Called from the player's input tick when the fire button is pressed.</summary>
        public void Fire()
        {
            // TODO (other agent): check _ammo, spawn projectile on server, consume ammo.

            // REQUIRED LOG -> "El jugador B disparo"
            RedesLog.Info(RedesLog.COMBAT, $"El jugador {Object.InputAuthority} disparo");
        }
    }
}
