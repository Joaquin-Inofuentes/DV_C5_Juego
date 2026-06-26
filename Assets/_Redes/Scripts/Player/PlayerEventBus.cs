using System;
using UnityEngine;

namespace Redes.Player
{
    /// <summary>
    /// Mini Event Bus for a single player instance to decouple player logic from views.
    /// Each player prefab has this script, which acts as a dispatcher for player-specific actions.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerEventBus : MonoBehaviour
    {
        // 1. Recibir disparo de quien (PlayerRef ID) y GameObject de la bala (o null)
        public Action<int, GameObject> OnTookDamage;

        // 2. Crear/Disparar proyectil
        public Action<float> OnShoot;

        // 3. Moverse (Velocity)
        public Action<Vector3> OnMove;

        // 4. Aparecer (Spawn)
        public Action OnSpawned;

        // 5. Morir (Die)
        public Action<Vector3> OnDied;

        // 6. Recargar (Reload)
        public Action OnReload;

        // 7. Cambio de munición (CurrentAmmo, MaxMagazine)
        public Action<int, int> OnAmmoChanged;

        // 8. Agacharse/levantarse (mecánica extra)
        public Action<bool> OnCrouch;

        // 9. Teletransporte (mecánica extra) — posición de origen y destino
        public Action<Vector3, Vector3> OnTeleport;

        public void TriggerTookDamage(int attackerId, GameObject bulletObj)
        {
            OnTookDamage?.Invoke(attackerId, bulletObj);
        }

        public void TriggerShoot(float animSpeed = 1f)
        {
            OnShoot?.Invoke(animSpeed);
        }

        public void TriggerMove(Vector3 velocity)
        {
            OnMove?.Invoke(velocity);
        }

        public void TriggerSpawned()
        {
            OnSpawned?.Invoke();
        }

        public void TriggerDied(Vector3 hitDirection = default)
        {
            OnDied?.Invoke(hitDirection);
        }

        public void TriggerReload()
        {
            OnReload?.Invoke();
        }

        public void TriggerAmmoChanged(int currentAmmo, int maxMagazine)
        {
            OnAmmoChanged?.Invoke(currentAmmo, maxMagazine);
        }

        public void TriggerCrouch(bool isCrouching)
        {
            OnCrouch?.Invoke(isCrouching);
        }

        public void TriggerTeleport(Vector3 origin, Vector3 destination)
        {
            OnTeleport?.Invoke(origin, destination);
        }
    }
}
