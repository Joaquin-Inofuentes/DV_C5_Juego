using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Network
{
    /// <summary>
    /// SOLID/SRP: only responsibility = spawn / despawn the player NetworkObject.
    /// In a HOST architecture, ONLY the server (host) is allowed to Spawn.
    ///
    /// Keeps a map of PlayerRef -> spawned NetworkObject so it can despawn on leave.
    /// Logic is implemented by another agent.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Optional spawn points (assigned by the Link tool).")]
        [Tooltip("If empty, the other agent can spawn at Vector3.zero / random offset.")]
        [SerializeField] private Transform[] _spawnPoints;

        // Tracks which NetworkObject belongs to which player.
        private readonly Dictionary<PlayerRef, NetworkObject> _spawned = new Dictionary<PlayerRef, NetworkObject>();

        /// <summary>Spawns one player. Called by HostNetworkService.OnPlayerJoined (server only).</summary>
        public void SpawnPlayer(NetworkRunner runner, PlayerRef player, NetworkObject prefab)
        {
            RedesLog.Info(RedesLog.NET, $">> PlayerSpawner.SpawnPlayer(player={player})");
            Vector3 pos = GetSpawnPosition(player);
            RedesLog.Info(RedesLog.NET, $"   spawnPos={pos}  prefab={prefab.name}");
            var obj = runner.Spawn(prefab, pos, Quaternion.identity, player);
            if (obj != null)
            {
                _spawned[player] = obj;
                RedesLog.Info(RedesLog.PLAYER, $"   Inicio el jugador {player} en {pos}");
            }
            else
            {
                RedesLog.Error(RedesLog.NET, $"   runner.Spawn devolvio NULL para player={player}. Verifica que el prefab este registrado en NetworkProjectConfig.");
            }
            RedesLog.Info(RedesLog.NET, $"<< PlayerSpawner.SpawnPlayer(player={player}) total_spawned={_spawned.Count}");
        }

        public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $">> PlayerSpawner.DespawnPlayer(player={player})");
            if (_spawned.TryGetValue(player, out var obj))
            {
                runner.Despawn(obj);
                _spawned.Remove(player);
                RedesLog.Info(RedesLog.NET, $"   Despawneado. total_spawned={_spawned.Count}");
            }
            else
            {
                RedesLog.Warn(RedesLog.NET, $"   player={player} no estaba en _spawned");
            }
            RedesLog.Info(RedesLog.NET, $"<< PlayerSpawner.DespawnPlayer(player={player})");
        }

        public bool IsPlayerSpawned(PlayerRef player)
        {
            return _spawned.ContainsKey(player) && _spawned[player] != null;
        }

        public void DespawnAllActivePlayers(NetworkRunner runner)
        {
            RedesLog.Info(RedesLog.NET, ">> PlayerSpawner.DespawnAllActivePlayers()");
            var list = new List<NetworkObject>(_spawned.Values);
            foreach (var obj in list)
            {
                if (obj != null && obj.IsValid)
                {
                    runner.Despawn(obj);
                }
            }
            _spawned.Clear();
            RedesLog.Info(RedesLog.NET, "<< PlayerSpawner.DespawnAllActivePlayers()");
        }

        public void ClearSpawned()
        {
            _spawned.Clear();
        }

        // Helper for the other agent to pick a spawn position.
        private Vector3 GetSpawnPosition(PlayerRef player)
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
                Transform sp = _spawnPoints[randomIndex];
                if (sp != null)
                {
                    RedesLog.Info(RedesLog.PLAYER, $"[PlayerSpawner] Se spameo al jugador {player} en posicion {sp.position} (SpawnPoint seleccionado: '{sp.name}', Indice: {randomIndex})");
                    return sp.position;
                }
            }
            
            // Fallback
            Vector3 fallbackPos = new Vector3(player.PlayerId * 3f, 0f, player.PlayerId * 3f);
            RedesLog.Warn(RedesLog.PLAYER, $"[PlayerSpawner] No hay _spawnPoints validos asignados en el inspector. Usando posicion fallback {fallbackPos} para el jugador {player}");
            return fallbackPos;
        }
    }
}
