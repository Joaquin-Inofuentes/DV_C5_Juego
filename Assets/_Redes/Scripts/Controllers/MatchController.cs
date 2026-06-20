using UnityEngine;
using Redes.Core;
using Redes.Views;

namespace Redes.Controllers
{
    /// <summary>
    /// MVC - CONTROLLER for the match outcome (win/lose).
    ///
    /// It receives the LOCAL result (already resolved per-client by the networked
    /// MatchNetworkController RPC) and drives the ResultView. It also relays the
    /// ResultView's Action so the GameFlowController can move to Finished.
    ///
    /// Logic is implemented by another agent.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        [Header("View (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private ResultView _resultView;

        private void OnEnable()
        {
            // TODO (other agent): _resultView.OnResultNotified += HandleResultNotified;
        }

        private void OnDisable()
        {
            // TODO (other agent): _resultView.OnResultNotified -= HandleResultNotified;
        }

        /// <summary>
        /// Entry point called per-client with this client's outcome.
        /// Shows the view (which also fires the Action + the required logs).
        /// </summary>
        public void NotifyResult(MatchResult result)
        {
            _resultView.ShowResult(result);
        }

        private void HandleResultNotified(MatchResult result)
        {
            // TODO (other agent): tell GameFlowController/model the match finished.
        }
    }
}
