using System;
using UnityEngine;
using USP.Entities;

namespace Game.Squad
{
    /// <summary>
    /// Event Bus desacoplado para la gestión de eventos de la escuadra/soldados.
    /// </summary>
    public static class SquadEventBus
    {
        // Evento cuando se solicita el cambio de líder (1, 2, 3)
        public static event Action<int> OnSoldierSwitchRequested;

        // Evento cuando el líder ha cambiado en el juego
        public static event Action<SoldierController> OnLeaderChanged;

        // Evento cuando un soldado toma daño (para feedback visual o HUD y para que otros ayuden)
        public static event Action<SoldierController, float, GameObject> OnSoldierDamaged;

        // Evento cuando un soldado muere
        public static event Action<SoldierController> OnSoldierDied;

        public static void TriggerSoldierSwitchRequested(int index) => OnSoldierSwitchRequested?.Invoke(index);
        public static void TriggerLeaderChanged(SoldierController newLeader) => OnLeaderChanged?.Invoke(newLeader);
        public static void TriggerSoldierDamaged(SoldierController soldier, float damage, GameObject attacker) => OnSoldierDamaged?.Invoke(soldier, damage, attacker);
        public static void TriggerSoldierDied(SoldierController soldier) => OnSoldierDied?.Invoke(soldier);
    }
}
