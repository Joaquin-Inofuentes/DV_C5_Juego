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

        /// <summary>
        /// REQUIRED: result is broadcast "con action". Other systems subscribe.
        /// </summary>
        public event Action<MatchResult> OnResultNotified;

        /// <summary>
        /// Called by MatchController when the match ends. Shows text AND fires the Action.
        /// </summary>
        public void ShowResult(MatchResult result)
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);

            if (result == MatchResult.Win)
            {
                if (_resultText != null) _resultText.text = "¡GANASTE!";
                // REQUIRED LOG -> "El jugador A recibio la notitifacion de q gano con action"
                RedesLog.Info(RedesLog.MATCH, "El jugador A recibio la notitifacion de q gano con action");
            }
            else if (result == MatchResult.Lose)
            {
                if (_resultText != null) _resultText.text = "PERDISTE";
                // REQUIRED LOG -> "El jugador B recibio la notificacion de q perdio con action"
                RedesLog.Info(RedesLog.MATCH, "El jugador B recibio la notificacion de q perdio con action");
            }

            // Fire the Action so any subscriber reacts to the outcome.
            OnResultNotified?.Invoke(result);
        }
    }
}
