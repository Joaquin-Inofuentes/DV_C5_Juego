using System;
using Redes.Core;

namespace Redes.Models
{
    /// <summary>
    /// MVC - MODEL for a single player's gameplay data (health + ammo).
    /// Pure data; the networked source of truth lives in the NetworkBehaviours,
    /// this is the local view-model the HUD binds to.
    /// </summary>
    public class PlayerModel
    {
        public int Health { get; private set; } = GameConstants.DEFAULT_MAX_HEALTH;
        public int Ammo { get; private set; } = GameConstants.DEFAULT_MAGAZINE_SIZE;
        public bool IsAlive => Health > 0;

        public event Action<int> OnHealthChanged;
        public event Action<int> OnAmmoChanged;

        public void SetHealth(int value)
        {
            Health = value;
            OnHealthChanged?.Invoke(value);
        }

        public void SetAmmo(int value)
        {
            Ammo = value;
            OnAmmoChanged?.Invoke(value);
        }
    }
}
