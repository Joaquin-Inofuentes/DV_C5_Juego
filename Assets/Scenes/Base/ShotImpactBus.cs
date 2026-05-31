using System;
using UnityEngine;

/// <summary>
/// Bus de eventos estático para impactos de disparos en el mundo.
/// Cualquier proyectil (Proyectil.cs, Bala.cs) llama Trigger() al impactar.
/// Enemigos y aliados se suscriben para reaccionar a ruidos de disparo cercanos.
/// 
/// Flujo:
///   Proyectil impacta → ShotImpactBus.Trigger(pos, owner)
///   EnemyController.OnShotNearby() filtra por distancia + LOS y reacciona
///   AllyResponseSystem.OnShotNearby() filtra por distancia + LOS y reacciona
/// </summary>
public static class ShotImpactBus
{
    /// <summary>
    /// Vector3 = posición del impacto en el mundo.
    /// GameObject = dueño del proyectil (quién disparó).
    /// </summary>
    public static event Action<Vector3, GameObject> OnShotImpact;

    public static void Trigger(Vector3 impactPos, GameObject owner)
    {
        OnShotImpact?.Invoke(impactPos, owner);
    }
}
