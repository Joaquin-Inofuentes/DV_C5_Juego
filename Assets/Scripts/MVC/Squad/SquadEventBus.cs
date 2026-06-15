using UnityEngine;
using System;
using Game.Squad;

public static class SquadEventBus
{
    // --- Eventos de dano y muerte ---
    public static event Action<UnitController, float, GameObject> OnUnitDamaged;
    public static event Action<UnitController> OnUnitDied;
    public static event Action<UnitController> OnLeaderChanged;

    // --- Pedido de ayuda con prioridad ---
    // priority: 1 = lider atacado (maxima), 2 = aliado atacado
    public static event Action<UnitController, Transform, int> OnHelpRequested;

    // --- Estado caido ---
    /// <summary>Se dispara cuando un soldado aliado cae (HP llega a 0). No muere, puede ser revivido.</summary>
    public static event Action<UnitController> OnUnitDowned;

    /// <summary>Se dispara cuando un soldado es revivido exitosamente.</summary>
    public static event Action<UnitController, UnitController> OnUnitRevived;

    public static void TriggerUnitDamaged(UnitController unit, float damage, GameObject attacker) => OnUnitDamaged?.Invoke(unit, damage, attacker);
    public static void TriggerUnitDied(UnitController unit) => OnUnitDied?.Invoke(unit);
    public static void TriggerLeaderChanged(UnitController leader) => OnLeaderChanged?.Invoke(leader);

    /// <summary>
    /// Emite un pedido de ayuda al escuadron.
    /// priority 1 = lider atacado (urgente), priority 2 = aliado atacado.
    /// </summary>
    public static void TriggerHelpRequested(UnitController victim, Transform attacker, int priority)
    {
        // Debug.Log($"<color=orange>[SquadEventBus]</color> HelpRequested: {victim.name} atacado por {(attacker != null ? attacker.name : "?")} (prioridad {priority})");
        OnHelpRequested?.Invoke(victim, attacker, priority);
    }

    /// <summary>Notifica al escuadron que un soldado ha caido.</summary>
    public static void TriggerUnitDowned(UnitController downed)
    {
        // Debug.Log($"<color=red>[SquadEventBus]</color> <b>OnUnitDowned</b>: {downed.name} ha caido. El escuadron es notificado.");
        OnUnitDowned?.Invoke(downed);
    }

    /// <summary>Notifica al escuadron que un soldado fue revivido.</summary>
    public static void TriggerUnitRevived(UnitController revived, UnitController reviver)
    {
        // Debug.Log($"<color=lime>[SquadEventBus]</color> <b>OnUnitRevived</b>: {revived.name} fue revivido por {reviver.name}.");
        OnUnitRevived?.Invoke(revived, reviver);
    }
}
