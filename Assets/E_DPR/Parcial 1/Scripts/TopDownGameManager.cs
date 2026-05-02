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
    private bool _localPlayerSpawned = false;
    public override void Spawned()
    {
        Instance = this;
        Winner = PlayerRef.None;

        // Solo spawneamos si somos el cliente local y no lo hemos hecho ya
        if (Runner.IsRunning && !_localPlayerSpawned)
        {
            _localPlayerSpawned = true;
            SpawnLocalPlayer(Runner.LocalPlayer);
        }
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
        // FIX: No uses ActivePlayers.IndexOf, porque la lista no se sincroniza 
        // instantáneamente al conectar.

        // El PlayerId de Fusion suele empezar en 1, 2, 3... 
        // Usamos el operador % (módulo) por si el ID es muy alto o para ciclar los spawn points.
        // Restamos 1 para que el Player 1 use el índice 0.
        int index = (player.PlayerId - 1) % spawnPoints.Length;

        // Validación de seguridad por si el ID es negativo o extraño
        if (index < 0) index = 0;

        Transform sp = spawnPoints[index];

        Debug.Log($"[TopDownGameManager] Spawneando Jugador {player.PlayerId} en el índice de spawn fijo: {index}");

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

    public void Prueba1(NetworkRunner Runer1, PlayerRef Player1)
    {
        Debug.Log($"[TopDownGameManager] Prueba1: Runner {Runer1}, Player {Player1}");
    }
    public void Prueba2(NetworkRunner Runer2)
    {
        Debug.Log($"[TopDownGameManager] Prueba2: Runner {Runer2}");
    }
}