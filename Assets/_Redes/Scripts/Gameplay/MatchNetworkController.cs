using Fusion;
using UnityEngine;
using Redes.Core;
using Redes.Controllers;

namespace Redes.Gameplay
{
    /// <summary>
    /// NETWORKED win/lose broadcaster. Lives on a scene "GameManager" Network Object.
    ///
    /// When a player dies (PlayerHealth), the host calls AnnounceResult, which sends
    /// an RPC to ALL clients. Each client compares the loser to its own local player
    /// and shows WIN or LOSE through its MatchController -> ResultView ("con action").
    /// This guarantees every user is notified and there is NO desfase (single RPC,
    /// resolved locally and identically).
    /// Logic is implemented by another agent.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class MatchNetworkController : NetworkBehaviour
    {
        [Header("Local controller (assigned by the Link tool)")]
        [SerializeField] private MatchController _matchController;

        /// <summary>Called on the host when the match is decided.</summary>
        public void AnnounceResult(PlayerRef loser, PlayerRef winner)
        {
            RedesLog.Info(RedesLog.MATCH, $"Anunciando resultado a todos. Perdedor={loser}, Ganador={winner}");
            RpcAnnounceResult(loser, winner);
        }

        /// <summary>
        /// RPC delivered to EVERY client. Each one resolves win/lose locally.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RpcAnnounceResult(PlayerRef loser, PlayerRef winner)
        {
            if (_matchController == null)
            {
                _matchController = FindFirstObjectByType<MatchController>();
            }

            var result = (Runner.LocalPlayer == loser) ? MatchResult.Lose : MatchResult.Win;
            if (_matchController != null)
            {
                _matchController.NotifyResult(result);
            }
        }
    }
}
