using System;
using UnityEngine;
using Fusion;

namespace Redes.Core
{
    /// <summary>
    /// GLOBAL EVENT BUS
    /// Desacopla la lógica de red de las respuestas visuales/auditivas,
    /// permitiendo que cualquier sistema (como la UI, Audio o el GameManager)
    /// se entere de eventos clave sin necesidad de referencias cruzadas directas.
    /// </summary>
    [CreateAssetMenu(fileName = "GameEventBus", menuName = "Redes/Game Event Bus")]
    public class GameEventBus : ScriptableObject
    {
        // -----------------------
        // EVENTOS DEL JUGADOR
        // -----------------------

        /// <summary>
        /// Se dispara cuando el jugador dispara (útil para animaciones, sonido, UI).
        /// </summary>
        public event Action<PlayerRef> OnPlayerShooting;

        /// <summary>
        /// Se dispara cuando el jugador inicia la recarga.
        /// </summary>
        public event Action<PlayerRef> OnPlayerReloadStarted;

        /// <summary>
        /// Se dispara cuando un jugador recibe daño. 
        /// Parámetros: victima, nuevaSalud, atacante.
        /// </summary>
        public event Action<PlayerRef, int, PlayerRef> OnPlayerTookDamage;

        /// <summary>
        /// Se dispara cuando un jugador muere (salud = 0).
        /// Parámetros: victima, atacante.
        /// </summary>
        public event Action<PlayerRef, PlayerRef> OnPlayerDied;

        // -----------------------
        // MÉTODOS DE DISPARO
        // -----------------------

        public void TriggerPlayerShooting(PlayerRef player)
        {
            OnPlayerShooting?.Invoke(player);
        }

        public void TriggerPlayerReloadStarted(PlayerRef player)
        {
            OnPlayerReloadStarted?.Invoke(player);
        }

        public void TriggerPlayerTookDamage(PlayerRef victim, int newHealth, PlayerRef attacker)
        {
            OnPlayerTookDamage?.Invoke(victim, newHealth, attacker);
        }

        public void TriggerPlayerDied(PlayerRef victim, PlayerRef attacker)
        {
            OnPlayerDied?.Invoke(victim, attacker);
        }
    }
}
