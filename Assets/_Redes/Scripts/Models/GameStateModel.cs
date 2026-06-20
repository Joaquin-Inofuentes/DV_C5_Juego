using System;
using Redes.Core;

namespace Redes.Models
{
    /// <summary>
    /// MVC - MODEL. Pure data + state for the overall game flow.
    /// No Unity, no Fusion: just state and change notifications (Observer).
    /// Controllers mutate it; Views read it / listen to it.
    /// </summary>
    public class GameStateModel
    {
        public GamePhase Phase { get; private set; } = GamePhase.Booting;
        public int ConnectedPlayers { get; private set; }

        /// <summary>Raised whenever the phase changes (the View redraws).</summary>
        public event Action<GamePhase> OnPhaseChanged;

        /// <summary>Raised whenever the connected player count changes.</summary>
        public event Action<int> OnPlayersChanged;

        public void SetPhase(GamePhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            // TODO (other agent): any side effects.
            OnPhaseChanged?.Invoke(phase);
        }

        public void SetPlayers(int count)
        {
            ConnectedPlayers = count;
            OnPlayersChanged?.Invoke(count);
        }
    }
}
