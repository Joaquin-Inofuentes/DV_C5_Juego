using Fusion;
using UnityEngine;
using System;

public class TopDownPlayerHealth : NetworkBehaviour
{
    [Networked] public int Health { get; set; }
    [Networked] public int PlayerNumber { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Health = 100;
            PlayerNumber = Object.InputAuthority.PlayerId;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage, int attackerId)
    {
        if (Health <= 0) return;

        Health -= damage;
        Debug.Log($"[CLASS: TopDownPlayerHealth] P{PlayerNumber} Daño. HP: {Health}");

        if (Health <= 0)
        {
            Health = 0;
            // Buscamos al atacante para declararlo ganador
            PlayerRef winnerRef = PlayerRef.None;
            foreach (var p in Runner.ActivePlayers)
            {
                if (p.PlayerId == attackerId) winnerRef = p;
            }

            if (TopDownGameManager.Instance != null)
                TopDownGameManager.Instance.RPC_SetWinner(winnerRef);

            Invoke(nameof(DelayedDespawn), 0.5f);
        }
    }

    private void DelayedDespawn() => Runner.Despawn(Object);
}