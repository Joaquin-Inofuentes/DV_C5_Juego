using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TopDownGameManager : NetworkBehaviour
{
    public static TopDownGameManager Instance { get; private set; }

    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Networked] public NetworkBool MatchStarted { get; set; }
    [Networked] public int ConnectedPlayers { get; set; }

    public override void Spawned()
    {
        Instance = this;
        Debug.Log("<color=cyan>[Manager] Iniciado en modo compartido.</color>");
        if (Runner.IsRunning) SpawnLocalPlayer(Runner.LocalPlayer);
    }

    private void SpawnLocalPlayer(PlayerRef player)
    {
        int index = Runner.ActivePlayers.ToList().IndexOf(player);
        if (index < 0) index = 0;
        Transform sp = spawnPoints[index % spawnPoints.Length];

        Debug.Log($"<color=yellow>[Manager] Spawneando jugador local ID: {player.PlayerId}</color>");

        Runner.Spawn(playerPrefab, sp.position, sp.rotation, player, (runner, obj) => {
            runner.SetPlayerObject(player, obj);
        });
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.IsSharedModeMasterClient && !MatchStarted)
        {
            ConnectedPlayers = Runner.ActivePlayers.Count();
            if (ConnectedPlayers >= 2)
            {
                Debug.Log("<color=green>[Manager] 🚀 ¡Suficientes jugadores! Partida iniciada.</color>");
                MatchStarted = true;
            }
        }
    }

    public void EvaluateWinLose()
    {
        // Delay pequeño para que el Despawn se limpie de la lista
        Invoke(nameof(CheckVictory), 0.3f);
    }

    private void CheckVictory()
    {
        // Buscamos jugadores que tengan objeto activo en la red
        var alive = Runner.ActivePlayers.Where(p => Runner.TryGetPlayerObject(p, out _)).ToList();

        Debug.Log($"<color=white>[Manager] Jugadores restantes: {alive.Count}</color>");

        if (alive.Count == 1)
        {
            PlayerRef winner = alive[0];
            if (winner == Runner.LocalPlayer)
                Debug.Log("<color=green><b>🏆 ¡VICTORIA MAGISTRAL! Eres el ganador.</b></color>");
            else
                Debug.Log($"<color=red><b>💀 DERROTA. El ganador es P{winner.PlayerId}</b></color>");

            MatchStarted = false;
        }
    }
}