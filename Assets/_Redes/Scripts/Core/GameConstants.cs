namespace Redes.Core
{
    /// <summary>
    /// Single source of truth for tuning values shared across systems.
    /// Keeping them here (instead of magic numbers) follows SOLID:
    /// every system depends on this abstraction, not on scattered literals.
    /// </summary>
    public static class GameConstants
    {
        // --- Session / Host architecture ---
        // The match must NOT start until at least this many players are connected.
        public const int MIN_PLAYERS_TO_START = 2;

        // Default room/session name used when the Host creates a session.
        public const string DEFAULT_ROOM_NAME = "RedesRoom";

        // Max players allowed in a single Host session.
        public const int MAX_PLAYERS = 4;

        // --- Gameplay tuning (values only; logic is implemented by another agent) ---
        public const float DEFAULT_MOVE_SPEED   = 5f;
        public const int   DEFAULT_MAX_HEALTH   = 100;
        public const int   DEFAULT_BULLET_DAMAGE = 25;

        // --- Extra mechanic: Ammo / Reload ---
        public const int   DEFAULT_MAGAZINE_SIZE = 6;
        public const float DEFAULT_RELOAD_TIME   = 1.5f;
    }
}
