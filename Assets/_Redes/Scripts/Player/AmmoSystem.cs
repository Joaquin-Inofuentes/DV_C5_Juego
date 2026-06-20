using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Player
{
    /// <summary>
    /// EXTRA MECHANIC (distinct from Movement / Shoot / Jump): AMMO + RELOAD.
    ///
    /// Magazine is limited; firing consumes ammo; when empty the player must
    /// reload (takes RELOAD_TIME). Ammo + reload state are [Networked] so they
    /// stay in sync across host and clients (no desfase).
    /// Logic is implemented by another agent.
    /// </summary>
    public class AmmoSystem : NetworkBehaviour
    {
        [Header("Tuning")]
        [SerializeField] private int _magazineSize = GameConstants.DEFAULT_MAGAZINE_SIZE;
        [SerializeField] private float _reloadTime = GameConstants.DEFAULT_RELOAD_TIME;

        // Networked state.
        [Networked] public int CurrentAmmo { get; set; }
        [Networked] public NetworkBool IsReloading { get; set; }
        // Server-authoritative timer for the reload duration.
        [Networked] public TickTimer ReloadTimer { get; set; }

        public bool HasAmmo => CurrentAmmo > 0;

        public override void Spawned()
        {
            // TODO (other agent): if (Object.HasStateAuthority) CurrentAmmo = _magazineSize;
        }

        /// <summary>Consume one round. Returns false if empty.</summary>
        public bool TryConsume()
        {
            // TODO (other agent): decrement CurrentAmmo if > 0.
            return HasAmmo;
        }

        /// <summary>Begin reloading.</summary>
        public void StartReload()
        {
            // REQUIRED-STYLE FLAG LOG
            RedesLog.Info(RedesLog.AMMO, $"El jugador {Object.InputAuthority} esta recargando");
            // TODO (other agent): set IsReloading = true; ReloadTimer = TickTimer.CreateFromSeconds(Runner, _reloadTime);
        }

        public override void FixedUpdateNetwork()
        {
            // TODO (other agent): when ReloadTimer.Expired(Runner) -> refill magazine,
            // set IsReloading = false, log "Recarga completa".
        }
    }
}
