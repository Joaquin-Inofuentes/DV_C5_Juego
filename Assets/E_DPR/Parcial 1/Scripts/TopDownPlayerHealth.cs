using Fusion;
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
        if (Health <= 0) return;

        Health -= damage;
        Debug.Log($"[HEALTH] P{OwnerRef.PlayerId} recibió daño de P{attacker.PlayerId}. HP: {Health}");

        if (Health <= 0)
        {
            Health = 0;
            if (TopDownGameManager.Instance != null)
            {
                // El atacante es el ganador
                TopDownGameManager.Instance.RPC_SetWinner(attacker);
            }
            Runner.Despawn(Object);
        }
    }
}