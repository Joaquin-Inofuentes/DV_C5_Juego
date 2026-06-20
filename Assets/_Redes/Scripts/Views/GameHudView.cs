using UnityEngine;
using UnityEngine.UI; // Legacy Text.

namespace Redes.Views
{
    /// <summary>
    /// MVC - VIEW for the in-match HUD (local player health + ammo).
    /// ONLY draws UI. Binds to PlayerModel via PlayerController.
    /// Uses legacy UnityEngine.UI.Text.
    /// </summary>
    public class GameHudView : MonoBehaviour
    {
        [Header("Legacy Text refs (assigned by the Link tool)")]
        [SerializeField] private Text _healthText; // "Vida: 100"
        [SerializeField] private Text _ammoText;   // "Munición: 6/6"

        public void ShowHealth(int health)
        {
            if (_healthText != null) _healthText.text = $"Vida: {health}";
        }

        public void ShowAmmo(int ammo, int magazineSize)
        {
            if (_ammoText != null) _ammoText.text = $"Munición: {ammo}/{magazineSize}";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
