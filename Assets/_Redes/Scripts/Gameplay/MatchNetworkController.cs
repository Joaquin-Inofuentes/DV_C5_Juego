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
            if (Object.HasStateAuthority)
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
        public void RpcSetReadyForRematch()
        {
            RedesLog.Info(RedesLog.MATCH, $">> MatchNetworkController.RpcSetReadyForRematch() [IN] - ReadyForRematchCount={ReadyForRematchCount}, IsServer={Object.HasStateAuthority}");
            try
            {
                ReadyForRematchCount++;
                RedesLog.Info(RedesLog.MATCH, $"   MatchNetworkController: Increment completed. ReadyForRematchCount={ReadyForRematchCount}/2");

                if (ReadyForRematchCount >= 2)
                {
                    RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: Both players ready. [IN] Starting RematchSequenceCoroutine");
                    StartCoroutine(RematchSequenceCoroutine());
                    RedesLog.Info(RedesLog.MATCH, "   MatchNetworkController: [OUT] RematchSequenceCoroutine successfully started");
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchNetworkController: [ERROR] Exception in RpcSetReadyForRematch: {ex.Message}\n{ex.StackTrace}");
            }
            RedesLog.Info(RedesLog.MATCH, "<< MatchNetworkController.RpcSetReadyForRematch() [OUT]");
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
