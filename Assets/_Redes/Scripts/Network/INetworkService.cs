using System;
using Fusion;

namespace Redes.Network
{
    /// <summary>
    /// SOLID (Dependency Inversion): controllers/views talk to the network
    /// through this abstraction instead of the concrete HostNetworkService /
    /// Photon Fusion types. Makes the flow testable and swappable.
    /// </summary>
    public interface INetworkService
    {
        /// <summary>True once a NetworkRunner has been started as Host.</summary>
        bool IsRunning { get; }

        /// <summary>How many players are currently connected to the session.</summary>
        int ConnectedPlayers { get; }

        // --- Lifecycle (Host architecture) ---
        void StartAsHost();   // Crea sala en GameMode.Host
        void StartAsClient(); // Se une a sala en GameMode.Client
        void Shutdown();

        // --- Events the rest of the game subscribes to (Observer pattern) ---
        event Action OnHostStarted;            // Runner activo (host o client).
        event Action<int> OnPlayerCountChanged; // Fired when players join/leave.
        event Action OnEnoughPlayersToStart;    // >= MIN_PLAYERS_TO_START reached.
    }
}
