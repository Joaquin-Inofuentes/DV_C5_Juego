using UnityEngine;
using UnityEngine.UI; // Legacy Text.

namespace Redes.Views
{
    /// <summary>
    /// MVC - VIEW for the in-match HUD (local player health + ammo + teleport cooldown).
    /// ONLY draws UI. Binds to PlayerModel via PlayerController.
    /// Uses legacy UnityEngine.UI.Text.
    /// </summary>
    public class GameHudView : MonoBehaviour
    {
        [Header("Legacy Text refs (assigned by the Link tool)")]
        [SerializeField] private Text _healthText; // "Vida: 100"
        [SerializeField] private Text _ammoText;   // "Munición: 6/6"
        [SerializeField] private Text _stateText;  // "Estado: Quieto"
        [SerializeField] private Slider _reloadSlider; // Slider for reload progress

        [Header("Teleport Cooldown UI (asignado por el Link tool)")]
        [SerializeField] private Image  _teleportRadial; // Image.Type.Filled Radial360
        [SerializeField] private Text   _teleportText;   // "TELEPORT [SPACE]"

        [Header("Crouch UI")]
        [SerializeField] private Text _crouchText; // "AGACHADO" indicator

        public void ShowHealth(int health)
        {
            if (_healthText != null) _healthText.text = $"Vida: {health}";
        }

        public void ShowAmmo(int ammo, int magazineSize)
        {
            if (_ammoText != null) _ammoText.text = $"Munición: {ammo}/{magazineSize}";
        }

        public void ShowState(string state)
        {
            if (_stateText != null) _stateText.text = $"Estado: {state}";
        }

        public void ShowReloadProgress(float progress)
        {
            if (_reloadSlider != null)
            {
                _reloadSlider.value = Mathf.Clamp01(progress);
                
                // Hide reload slider when not reloading (i.e. progress >= 1.0)
                _reloadSlider.gameObject.SetActive(progress < 1.0f);
            }
        }

        /// <summary>
        /// Actualiza el radial de cooldown del teleport.
        /// progress = 0 → recargando, 1 → listo.
        /// </summary>
        public void ShowTeleportCooldown(float progress)
        {
            if (_teleportRadial != null)
            {
                _teleportRadial.fillAmount = Mathf.Clamp01(progress);
                // Color: rojo cuando enfriando → verde cuando listo
                _teleportRadial.color = Color.Lerp(
                    new Color(1f, 0.3f, 0.3f),   // rojo
                    new Color(0.3f, 1f, 0.5f),   // verde
                    progress);
            }

            if (_teleportText != null)
            {
                _teleportText.text = progress >= 1f
                    ? "TELEPORT [SPACE]"
                    : $"TELEPORT {Mathf.CeilToInt((1f - progress) * 2f)}s";
                _teleportText.color = progress >= 1f ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        /// <summary>Muestra/oculta indicador visual de crouch.</summary>
        public void ShowCrouch(bool isCrouching)
        {
            if (_crouchText != null)
            {
                _crouchText.gameObject.SetActive(isCrouching);
                _crouchText.text = "AGACHADO";
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
