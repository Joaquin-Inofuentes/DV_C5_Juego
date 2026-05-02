using Fusion;
using System.Linq;
using UnityEngine;
using System;

public class TopDownGameManager : NetworkBehaviour
{
    public static TopDownGameManager Instance { get; private set; }
    public static event Action<PlayerRef> OnGameEndedStatic;

    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Networked] public NetworkBool MatchStarted { get; set; }
    [Networked, OnChangedRender(nameof(WinnerChanged))] public PlayerRef Winner { get; set; }

    public override void Spawned()
    {
        Instance = this;
        Winner = PlayerRef.None;
        Debug.Log("[CLASS: TopDownGameManager] Manager Spawneado.");
        if (Runner.IsRunning) SpawnLocalPlayer(Runner.LocalPlayer);
    }

    public void WinnerChanged()
    {
        if (Winner != PlayerRef.None)
        {
            Debug.Log($"[CLASS: TopDownGameManager] Sincronización: Ganador P{Winner.PlayerId}");
            OnGameEndedStatic?.Invoke(Winner);
        }
    }

    private void SpawnLocalPlayer(PlayerRef player)
    {
        int index = player.PlayerId - 1;
        Transform sp = spawnPoints[index % spawnPoints.Length];
        Runner.Spawn(playerPrefab, sp.position, sp.rotation, player, (r, obj) => r.SetPlayerObject(player, obj));
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.IsValid && Runner.IsSharedModeMasterClient && !MatchStarted)
        {
            if (Runner.ActivePlayers.Count() >= 2) MatchStarted = true;
        }
    }

    // RPC para que CUALQUIERA pueda avisar al Master Client quién ganó
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetWinner(PlayerRef winner)
    {
        Debug.Log($"[CLASS: TopDownGameManager] RPC Recibido. Seteando ganador a P{winner.PlayerId}");
        Winner = winner;
        MatchStarted = false;
    }
}