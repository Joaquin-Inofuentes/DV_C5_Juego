using UnityEngine;
using System;
using Game.Squad;

public static class SquadEventBus
{
    // --- Eventos existentes ---
    public static event Action<UnitController, float, GameObject> OnUnitDamaged;
    public static event Action<UnitController> OnUnitDied;
    public static event Action<UnitController> OnLeaderChanged;

    // --- Nuevo: Pedido de ayuda con prioridad ---
    // priority: 1 = líder atacado (máxima), 2 = aliado atacado
    public static event Action<UnitController, Transform, int> OnHelpRequested;

    public static void TriggerUnitDamaged(UnitController unit, float damage, GameObject attacker) => OnUnitDamaged?.Invoke(unit, damage, attacker);
    public static void TriggerUnitDied(UnitController unit) => OnUnitDied?.Invoke(unit);
    public static void TriggerLeaderChanged(UnitController leader) => OnLeaderChanged?.Invoke(leader);

    /// <summary>
    /// Emite un pedido de ayuda al escuadrón.
    /// priority 1 = líder atacado (urgente), priority 2 = aliado atacado.
    /// </summary>
    public static void TriggerHelpRequested(UnitController victim, Transform attacker, int priority)
    {
        Debug.Log($"<color=orange>[SquadEventBus]</color> HelpRequested: {victim.name} atacado por {(attacker != null ? attacker.name : "?")} (prioridad {priority})");
        OnHelpRequested?.Invoke(victim, attacker, priority);
    }
}
