using System;
using UnityEngine;
using UnityEngine.UI; // Legacy Text.
using Redes.Core;

namespace Redes.Views
{
    /// <summary>
    /// MVC - VIEW for the end-of-match notification (WIN / LOSE).
    /// Every user is notified of the result (assignment requirement) and the
    /// notification is delivered "con action" (C# Action) so other systems can
    /// react too.
    /// Uses legacy UnityEngine.UI.Text.
    /// </summary>
    public class ResultView : MonoBehaviour
    {
        [Header("Legacy Text ref (assigned by the Link tool)")]
        [SerializeField] private Text _resultText; // "¡GANASTE!" / "PERDISTE"

        [Header("Root panel toggled on game end (assigned by the Link tool)")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Retry Button")]
        [SerializeField] private Button _retryButton;

        [Header("Lobby Button")]
        [SerializeField] private Button _lobbyButton;

        [Header("Aesthetic Fullscreen Backgrounds")]
        [SerializeField] private RawImage _winBackground;
        [SerializeField] private RawImage _loseBackground;

        /// <summary>
        /// REQUIRED: result is broadcast "con action". Other systems subscribe.
        /// </summary>
        public event Action<MatchResult> OnResultNotified;

        public event Action OnRetryClicked;
        public event Action OnLobbyClicked;

        private void Awake()
        {
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "Awake", null, "No dynamic button listener required, relying on persistent events");
        }

        public void TriggerRetry()
        {
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "TriggerRetry", null, "Retry button clicked (traditional)");
            OnRetryClicked?.Invoke();
        }

        public void TriggerLobby()
        {
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "TriggerLobby", null, "Lobby button clicked (traditional)");
            OnLobbyClicked?.Invoke();
        }

        /// <summary>
        /// Called by MatchController when the match ends. Shows text AND fires the Action.
        /// </summary>
        public void ShowResult(MatchResult result)
        {
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "ShowResult", null, $"result={result}");
            if (_panelRoot != null) _panelRoot.SetActive(true);

            if (_winBackground != null) _winBackground.gameObject.SetActive(false);
            if (_loseBackground != null) _loseBackground.gameObject.SetActive(false);

            if (result == MatchResult.Win)
            {
                if (_resultText != null) _resultText.text = "¡GANASTE!";
                if (_winBackground != null) _winBackground.gameObject.SetActive(true);
                // REQUIRED LOG -> "El jugador A recibio la notitifacion de q gano con action"
                RedesLog.Info(RedesLog.MATCH, "El jugador A recibio la notitifacion de q gano con action");
            }
            else if (result == MatchResult.Lose)
            {
                if (_resultText != null) _resultText.text = "PERDISTE";
                if (_loseBackground != null) _loseBackground.gameObject.SetActive(true);
                // REQUIRED LOG -> "El jugador B recibio la notificacion de q perdio con action"
                RedesLog.Info(RedesLog.MATCH, "El jugador B recibio la notificacion de q perdio con action");
            }

            // Fire the Action so any subscriber reacts to the outcome.
            OnResultNotified?.Invoke(result);
        }

        public void ShowRematchStatus(string status)
        {
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "ShowRematchStatus", null, $"status='{status}'");
            if (_resultText != null) _resultText.text = status;
        }

        public void HideResult()
        {
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "HideResult [IN]", null, "Hiding results panel and resetting states");
            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_winBackground != null) _winBackground.gameObject.SetActive(false);
            if (_loseBackground != null) _loseBackground.gameObject.SetActive(false);
            if (_retryButton != null) _retryButton.interactable = true;
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "HideResult [OUT]", null, "Results panel hidden");
        }

        public void SetRetryButtonInteractable(bool interactable)
        {
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "SetRetryButtonInteractable [IN]", null, $"interactable={interactable}");
            if (_retryButton != null) _retryButton.interactable = interactable;
            RedesLog.Trace(RedesLog.MATCH, "ResultView", "SetRetryButtonInteractable [OUT]", null, "Completed");
        }
    }
}
