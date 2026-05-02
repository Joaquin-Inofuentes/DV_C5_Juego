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

    [Networked] public NetworkBool _matchStarted { get; set; }

    public bool MatchStarted
    {
        get
        {
            // Usamos solo IsValid. Si el objeto no está en red o fue destruido, devuelve false.
            if (Object == null || !Object.IsValid) return false;
            return _matchStarted;
        }
        set
        {
            // Solo el que tiene autoridad puede cambiar el estado
            if (Object != null && Object.IsValid && Object.HasStateAuthority)
                _matchStarted = value;
        }
    }
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
        // Obtenemos la lista actual de jugadores conectados de forma segura
        var currentPlayers = Runner.ActivePlayers.ToList();
        int index = currentPlayers.IndexOf(player);

        // Si el jugador no está en la lista (raro en Spawned, pero posible por lag de red)
        // intentamos usar el recuento total como índice temporal
        if (index == -1) index = currentPlayers.Count - 1;

        // Validación de límites de los índices de spawn
        if (index >= spawnPoints.Length)
        {
            Debug.LogError($"[TopDownGameManager] ERROR: No hay suficientes Spawn Points para el jugador {player.PlayerId}. Máximo: {spawnPoints.Length}, Índice intentado: {index}");
            return;
        }

        // Si el índice es válido, procedemos con el spawn
        Transform sp = spawnPoints[index];

        Debug.Log($"[TopDownGameManager] Spawneando Jugador {player.PlayerId} en el índice de spawn: {index}");

        Runner.Spawn(playerPrefab, sp.position, sp.rotation, player, (r, obj) =>
        {
            r.SetPlayerObject(player, obj);
        });
    }

    // Implementa esta interfaz en la clase: IPlayerLeft
    public override void FixedUpdateNetwork()
    {
        // Verificación de seguridad extrema
        if (Object == null || !Object.IsValid) return;

        // Si el dueño se fue y yo soy el nuevo Master, tomo la autoridad para que el juego siga
        if (!Object.HasStateAuthority && Runner.IsSharedModeMasterClient)
        {
            Object.RequestStateAuthority();
        }

        if (Object.HasStateAuthority && !MatchStarted)
        {
            if (Runner.ActivePlayers.Count() >= 2) MatchStarted = true;
        }
    }

    // Este método se llama automáticamente si implementas IPlayerLeft o heredas de NetworkBehaviour
    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.StateAuthority)
        {
            Debug.LogWarning("[GameManager] El Authority se desconectó. Intentando migrar...");

            // En Shared Mode, si el StateAuthority se va, podemos pedir la autoridad
            // desde el nuevo Master Client asignado por Fusion
            if (Runner.IsSharedModeMasterClient)
            {
                Object.RequestStateAuthority();
            }
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