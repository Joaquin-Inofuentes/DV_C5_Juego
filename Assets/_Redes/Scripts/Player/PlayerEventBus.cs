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
        public Action OnShoot;

        // 3. Moverse (Velocity)
        public Action<Vector3> OnMove;

        // 4. Aparecer (Spawn)
        public Action OnSpawned;

        // 5. Morir (Die)
        public Action OnDied;

        // 6. Recargar (Reload)
        public Action OnReload;

        public void TriggerTookDamage(int attackerId, GameObject bulletObj)
        {
            OnTookDamage?.Invoke(attackerId, bulletObj);
        }

        public void TriggerShoot()
        {
            OnShoot?.Invoke();
        }

        public void TriggerMove(Vector3 velocity)
        {
            OnMove?.Invoke(velocity);
        }

        public void TriggerSpawned()
        {
            OnSpawned?.Invoke();
        }

        public void TriggerDied()
        {
            OnDied?.Invoke();
        }

        public void TriggerReload()
        {
            OnReload?.Invoke();
        }
    }
}
