using Fusion;
using UnityEngine;

public class TopDownPlayerHealth : NetworkBehaviour
{
    [Networked] public int Health { get; set; } = 100;
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public int PlayerNumber { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Health = 100;
            IsDead = false;
            PlayerNumber = Object.InputAuthority.PlayerId; // Corregido: PlayerId
            Debug.Log($"<color=white>[SALUD] Inicializada para P{PlayerNumber}</color>");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Debug de vida propia solo para el dueño
        if (Object.HasInputAuthority && !IsDead && Runner.Tick % 100 == 0)
        {
            Debug.Log($"<color=red>[MI VIDA] Soy Jugador {PlayerNumber} | HP: {Health}</color>");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage, int attackerNumber)
    {
        if (IsDead) return;

        Debug.Log($"<color=orange>[DAÑO] P{PlayerNumber} impactado por P{attackerNumber}. Daño: {damage}</color>");
        Health -= damage;

        if (Health <= 0)
        {
            Health = 0;
            IsDead = true;
            Debug.Log($"<color=black><b>[ELIMINACIÓN] P{PlayerNumber} ha muerto.</b></color>");

            if (TopDownGameManager.Instance != null)
                TopDownGameManager.Instance.EvaluateWinLose();

            Runner.Despawn(Object);
        }
    }
}