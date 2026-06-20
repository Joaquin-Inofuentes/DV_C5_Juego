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
            RedesLog.Info(RedesLog.NET, $"Spawneando jugador para PlayerRef={player}");
            // TODO (other agent):
            // Vector3 pos = GetSpawnPosition(player);
            // var obj = runner.Spawn(prefab, pos, Quaternion.identity, player);
            // _spawned[player] = obj;
        }

        public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $"Despawneando jugador PlayerRef={player}");
            // TODO (other agent):
            // if (_spawned.TryGetValue(player, out var obj)) { runner.Despawn(obj); _spawned.Remove(player); }
        }

        // Helper for the other agent to pick a spawn position.
        private Vector3 GetSpawnPosition(PlayerRef player)
        {
            // TODO (other agent): use _spawnPoints if assigned.
            return Vector3.zero;
        }
    }
}
