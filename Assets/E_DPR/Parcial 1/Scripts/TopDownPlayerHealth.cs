using Fusion;
using System;
using UnityEngine;

public class TopDownPlayerHealth : NetworkBehaviour
{
    [Networked] public int Health { get; set; }
    // Cambiamos int por PlayerRef para evitar confusiones de IDs
    [Networked] public PlayerRef OwnerRef { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Health = 100;
            OwnerRef = Object.InputAuthority;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage, PlayerRef attacker)
    {
        try
        {
            if (Health <= 0) return;

            Health -= damage;

            // Sincronización de animación vía Trigger en el StateAuthority
            // Esto se replicará a los demás si usas un NetworkAnimator 
            // o mediante la lógica de cambio de estado.
            TriggerHurtAnimation();

            if (Health <= 0)
            {
                Health = 0;
                if (TopDownGameManager.Instance != null)
                    TopDownGameManager.Instance.RPC_SetWinner(attacker);

                Runner.Despawn(Object);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Health Error] Fallo al procesar daño: {e.Message}");
        }
    }
    [SerializeField] private NetworkMecanimAnimator networkAnimator;

    private void TriggerHurtAnimation()
    {
        try
        {
            if (networkAnimator != null)
                networkAnimator.SetTrigger("OnHurt");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"NetworkAnimator no encontrado: {ex.Message}");
        }
    }
}