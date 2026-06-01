using UnityEngine;
using System;
using Game.Squad;

public static class SquadEventBus
{
    public static event Action<UnitController, float, GameObject> OnUnitDamaged;
    public static event Action<UnitController> OnUnitDied;
    public static event Action<UnitController> OnLeaderChanged;

    public static void TriggerUnitDamaged(UnitController unit, float damage, GameObject attacker) => OnUnitDamaged?.Invoke(unit, damage, attacker);
    public static void TriggerUnitDied(UnitController unit) => OnUnitDied?.Invoke(unit);
    public static void TriggerLeaderChanged(UnitController leader) => OnLeaderChanged?.Invoke(leader);
}