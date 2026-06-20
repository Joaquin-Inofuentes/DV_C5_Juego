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

        public event System.Action<MatchResult> OnMatchFinished;

        private void OnEnable()
        {
            if (_resultView != null)
            {
                _resultView.OnResultNotified += HandleResultNotified;
            }
        }

        private void OnDisable()
        {
            if (_resultView != null)
            {
                _resultView.OnResultNotified -= HandleResultNotified;
            }
        }

        /// <summary>
        /// Entry point called per-client with this client's outcome.
        /// Shows the view (which also fires the Action + the required logs).
        /// </summary>
        public void NotifyResult(MatchResult result)
        {
            if (_resultView != null)
            {
                _resultView.ShowResult(result);
            }
        }

        private void HandleResultNotified(MatchResult result)
        {
            OnMatchFinished?.Invoke(result);
        }
    }
}
