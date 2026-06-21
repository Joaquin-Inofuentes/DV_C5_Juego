using Fusion;
using UnityEngine;
using Redes.Core;
using Redes.Controllers;

namespace Redes.Gameplay
{
    /// <summary>
    /// NETWORKED win/lose broadcaster. Lives on a scene "GameManager" Network Object.
    ///
    /// When a player dies, GameEventBus fires OnPlayerDied. Host listens to this
    /// and sends an RPC to ALL clients to announce the result.
    /// Also handles the Rematch logic.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class MatchNetworkController : NetworkBehaviour
    {
        [Header("Local controller (assigned by the Link tool)")]
        [SerializeField] private MatchController _matchController;

        [Header("Event Bus")]
        [SerializeField] private GameEventBus _eventBus;

        [Networked] public int ReadyForRematchCount { get; set; }

        public override void Spawned()
        {
            if (_eventBus != null && Object.HasStateAuthority)
            {
                _eventBus.OnPlayerDied += HandlePlayerDied;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_eventBus != null && hasState)
            {
                _eventBus.OnPlayerDied -= HandlePlayerDied;
            }
        }

        private void HandlePlayerDied(PlayerRef victim, PlayerRef attacker)
        {
            if (Object != null && Object.IsValid && Object.HasStateAuthority)
            {
                AnnounceResult(victim, attacker);
            }
        }

        public void AnnounceResult(PlayerRef loser, PlayerRef winner)
        {
            RedesLog.Info(RedesLog.MATCH, $"Anunciando resultado a todos. Perdedor={loser}, Ganador={winner}");
            RpcAnnounceResult(loser, winner);
        }

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

        // --- RETRY LOGIC ---

        /// <summary>
        /// Called by MatchController when the local player clicks "Retry".
        /// Sends to the server that we are ready.
        /// </summary>
        public void SetLocalPlayerReady()
        {
            RedesLog.Info(RedesLog.MATCH, ">> MatchNetworkController.SetLocalPlayerReady() [IN]");
            try
            {
                RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [IN] Calling RpcSetReadyForRematch()");
                RpcSetReadyForRematch();
                RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [OUT] RpcSetReadyForRematch completed");
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchNetworkController: [ERROR] Exception in SetLocalPlayerReady: {ex.Message}\n{ex.StackTrace}");
            }
            RedesLog.Info(RedesLog.MATCH, "<< MatchNetworkController.SetLocalPlayerReady() [OUT]");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcSetReadyForRematch(RpcInfo info = default)
        {
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcSetReadyForRematch", info.Source.ToString(), $"ReadyForRematchCount={ReadyForRematchCount}, IsServer={Object.HasStateAuthority}");
            try
            {
                ReadyForRematchCount++;
                RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcSetReadyForRematch", info.Source.ToString(), $"Increment completed. ReadyForRematchCount={ReadyForRematchCount}/2");

                if (ReadyForRematchCount == 1)
                {
                    // Find the player who sent this RPC and target the other player
                    PlayerRef readyPlayer = info.Source;
                    RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcSetReadyForRematch", readyPlayer.ToString(), "One player is ready, sending notification to the other");
                    RpcBroadcastOpponentReady(readyPlayer);
                }
                else if (ReadyForRematchCount >= 2)
                {
                    RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcSetReadyForRematch", info.Source.ToString(), "Both players ready. Starting RematchSequenceCoroutine");
                    StartCoroutine(RematchSequenceCoroutine());
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchNetworkController: [ERROR] Exception in RpcSetReadyForRematch: {ex.Message}\n{ex.StackTrace}");
            }
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcSetReadyForRematch", info.Source.ToString(), "Completed SetReadyForRematch execution");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBroadcastOpponentReady(PlayerRef readyPlayer)
        {
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcBroadcastOpponentReady", readyPlayer.ToString(), "Broadcast received");
            // Only update the screen for the player who is NOT readyPlayer
            if (Runner.LocalPlayer != readyPlayer)
            {
                RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcBroadcastOpponentReady", Runner.LocalPlayer.ToString(), "Opponent ready notification received. Updating ResultView text.");
                var matchCtrl = FindFirstObjectByType<MatchController>();
                if (matchCtrl != null)
                {
                    matchCtrl.UpdateRematchStatus("El otro jugador ya puso q si quiere re intntear. Dale a re intentar para seguir la lucha");
                }
            }
        }

        /// <summary>
        /// Called by MatchController when this local player wants to return to the lobby.
        /// Broadcasts to the other client so they know they are disconnected/returning to lobby.
        /// </summary>
        public void NotifyReturnToLobby()
        {
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "NotifyReturnToLobby", Runner.LocalPlayer.ToString(), "Notifying server and other players of lobby exit");
            try
            {
                RpcNotifyLobbyClicked();
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"Exception in NotifyReturnToLobby: {ex.Message}");
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RpcNotifyLobbyClicked(RpcInfo info = default)
        {
            PlayerRef sender = info.Source;
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "RpcNotifyLobbyClicked", sender.ToString(), $"Lobby exit notification received on client. LocalPlayer={Runner.LocalPlayer}");

            // Notify local UI that the other player returned to lobby (if we are NOT the sender)
            if (Runner.LocalPlayer != sender)
            {
                var matchCtrl = FindFirstObjectByType<MatchController>();
                if (matchCtrl != null)
                {
                    matchCtrl.UpdateRematchStatus("El otro jugador volvio al lobby");
                }

                // Wait 1.5 seconds and return the client to the booting phase automatically so both end up in the lobby.
                StartCoroutine(AutoReturnToLobbyCoroutine());
            }
        }

        private System.Collections.IEnumerator AutoReturnToLobbyCoroutine()
        {
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "AutoReturnToLobbyCoroutine [IN]", Runner.LocalPlayer.ToString(), "Waiting before returning to lobby...");
            yield return new WaitForSeconds(1.5f);
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "AutoReturnToLobbyCoroutine", Runner.LocalPlayer.ToString(), "Timer elapsed. Finding GameFlowController to exit...");
            var flow = FindFirstObjectByType<GameFlowController>();
            if (flow != null)
            {
                flow.TriggerReturnToLobby();
            }
            RedesLog.Trace(RedesLog.MATCH, "MatchNetworkController", "AutoReturnToLobbyCoroutine [OUT]", Runner.LocalPlayer.ToString(), "AutoReturn sequence finished");
        }

        private System.Collections.IEnumerator RematchSequenceCoroutine()
        {
            RedesLog.Info(RedesLog.MATCH, ">> MatchNetworkController.RematchSequenceCoroutine() [IN] - Coroutine started on Host");
            
            RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: Broadcasting first status...");
            try
            {
                RpcBroadcastRematchStatus("El otro jugador tambien quiere re intentar. Iniciando partida");
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchNetworkController: [ERROR] Exception broadcasting status: {ex.Message}");
            }
            
            RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [IN] Waiting 2.0 seconds...");
            yield return new WaitForSeconds(2.0f);
            RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [OUT] Wait completed.");

            RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: Broadcasting second status...");
            try
            {
                RpcBroadcastRematchStatus("Partida re iniciada");
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchNetworkController: [ERROR] Exception broadcasting second status: {ex.Message}");
            }
            
            RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [IN] Waiting 0.5 seconds...");
            yield return new WaitForSeconds(0.5f);
            RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [OUT] Wait completed.");

            try
            {
                ReadyForRematchCount = 0;
                int buildIdx = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                RedesLog.Info(RedesLog.MATCH, $"   MatchNetworkController: [IN] Calling Runner.LoadScene(buildIndex={buildIdx})");
                Runner.LoadScene(SceneRef.FromIndex(buildIdx));
                RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [OUT] Runner.LoadScene call completed");
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchNetworkController: [ERROR] Exception during reload/loadscene call: {ex.Message}\n{ex.StackTrace}");
            }
            RedesLog.Info(RedesLog.MATCH, "<< MatchNetworkController.RematchSequenceCoroutine() [OUT] - Coroutine finished");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBroadcastRematchStatus(string text)
        {
            RedesLog.Info(RedesLog.MATCH, $">> MatchNetworkController.RpcBroadcastRematchStatus() [IN] - text='{text}', IsServer={Object.HasStateAuthority}");
            try
            {
                var matchCtrl = FindFirstObjectByType<MatchController>();
                if (matchCtrl != null)
                {
                    RedesLog.Info(RedesLog.MATCH, $"   MatchNetworkController: [IN] Calling matchCtrl.UpdateRematchStatus('{text}')");
                    matchCtrl.UpdateRematchStatus(text);
                    RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [OUT] matchCtrl.UpdateRematchStatus completed");
                }
                else
                {
                    RedesLog.Warn(RedesLog.MATCH, "   MatchNetworkController: Could not find MatchController in scene to dispatch status update.");
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchNetworkController: [ERROR] Exception in RpcBroadcastRematchStatus: {ex.Message}\n{ex.StackTrace}");
            }
            RedesLog.Info(RedesLog.MATCH, "<< MatchNetworkController.RpcBroadcastRematchStatus() [OUT]");
        }
    }
}
