using UnityEngine;
using Redes.Models;
using Redes.Player;
using Redes.Views;
using Redes.Core;
using NetworkPlayer = Redes.Player.NetworkPlayer;

namespace Redes.Controllers
{
    /// <summary>
    /// MVC - CONTROLLER that binds the LOCAL player's networked state to the HUD.
    ///
    /// Lives in the scene. When the local NetworkPlayer spawns it calls Bind(),
    /// then this controller keeps the GameHudView in sync with the PlayerModel.
    /// Keeps the View free of any game logic (SRP).
    ///
    /// Logic is implemented by another agent.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("View (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private GameHudView _hudView;
        [SerializeField] private TeleportCooldownView _teleportCooldownView;

        // Local view-model the HUD binds to.
        private PlayerModel _model;
        private NetworkPlayer _localPlayer;

        /// <summary>Called by the local NetworkPlayer when it spawns.</summary>
        public void Bind(NetworkPlayer player)
        {
            _localPlayer = player;
            _model = new PlayerModel();
            
            _model.OnHealthChanged += h => { if (_hudView != null) _hudView.ShowHealth(h); };
            _model.OnAmmoChanged += a => { if (_hudView != null) _hudView.ShowAmmo(a, GameConstants.DEFAULT_MAGAZINE_SIZE); };

            if (_localPlayer.Health != null)
            {
                _localPlayer.Health.OnHealthChanged += _model.SetHealth;
                _model.SetHealth(_localPlayer.Health.CurrentHealth);
            }

            if (_localPlayer.Ammo != null)
            {
                _localPlayer.Ammo.OnAmmoChanged += _model.SetAmmo;
                _model.SetAmmo(_localPlayer.Ammo.CurrentAmmo);
            }

            // Bindear TeleportCooldownView al jugador local (CONTROLLER conecta Modelo ↔ Vista)
            if (_teleportCooldownView != null && _localPlayer.Teleport != null)
            {
                _teleportCooldownView.Bind(
                    _localPlayer.Teleport,
                    _localPlayer.EventBus,
                    _localPlayer.transform);
            }
        }

        private void Update()
        {
            if (_localPlayer == null || _localPlayer.Object == null || !_localPlayer.Object.IsValid || _hudView == null) return;

            // ── Estado textual ──────────────────────────────────────────────
            string state = "Quieto";

            if (_localPlayer.Crouch != null && _localPlayer.Crouch.Object != null
                && _localPlayer.Crouch.Object.IsValid && _localPlayer.Crouch.IsCrouching)
            {
                state = "Agachado";
            }
            else if (_localPlayer.Ammo != null && _localPlayer.Ammo.Object != null
                && _localPlayer.Ammo.Object.IsValid && _localPlayer.Ammo.IsReloading)
            {
                state = "Recargando";
            }
            else if (_localPlayer.Shooting != null && _localPlayer.Shooting.IsShooting)
            {
                state = "Disparo";
            }
            else if (_localPlayer.Movement != null && _localPlayer.Movement.NetworkVelocity.sqrMagnitude > 0.01f)
            {
                state = "Moviendose";
            }

            _hudView.ShowState(state);

            // ── Reload progress ─────────────────────────────────────────────
            if (_localPlayer.Ammo != null)
            {
                _hudView.ShowReloadProgress(_localPlayer.Ammo.ReloadProgress);
            }

            // ── Teleport cooldown radial ─────────────────────────────────────
            // TeleportCooldownView se actualiza a si misma en Update().
            // GameHudView.ShowTeleportCooldown() ya no se usa (reemplazado por TeleportCooldownView).

            // ── Crouch indicator ────────────────────────────────────────────
            if (_localPlayer.Crouch != null && _localPlayer.Crouch.Object != null
                && _localPlayer.Crouch.Object.IsValid)
            {
                _hudView.ShowCrouch(_localPlayer.Crouch.IsCrouching);
            }
        }
    }
}
