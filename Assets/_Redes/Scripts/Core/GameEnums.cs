namespace Redes.Core
{
    /// <summary>
    /// High level phases of the game flow (used by the MVC Model + Controllers).
    /// The GameFlowController moves the GameStateModel through these phases.
    /// </summary>
    public enum GamePhase
    {
        Booting = 0,            // App just started.
        SearchingSession = 1,   // Looking at the session list.
        WaitingForPlayers = 2,  // In a room, waiting for MIN_PLAYERS_TO_START.
        Playing = 3,            // Match running (2+ players connected).
        Finished = 4            // Someone won / someone lost.
    }

    /// <summary>
    /// Result of the match from the perspective of a single client.
    /// Notified to every user (win/lose condition requirement).
    /// </summary>
    public enum MatchResult
    {
        None = 0,
        Win = 1,
        Lose = 2
    }
}
