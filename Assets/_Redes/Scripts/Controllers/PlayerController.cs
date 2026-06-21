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
        }

        private void Update()
        {
            if (_localPlayer == null || _localPlayer.Object == null || !_localPlayer.Object.IsValid || _hudView == null) return;

            string state = "Quieto";
            if (_localPlayer.Ammo != null && _localPlayer.Ammo.Object != null && _localPlayer.Ammo.Object.IsValid && _localPlayer.Ammo.IsReloading)
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

            if (_localPlayer.Ammo != null)
            {
                _hudView.ShowReloadProgress(_localPlayer.Ammo.ReloadProgress);
            }
        }
    }
}
