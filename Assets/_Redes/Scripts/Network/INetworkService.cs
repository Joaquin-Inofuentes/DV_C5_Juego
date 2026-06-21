using System;

namespace Redes.Network
{
    /// <summary>
    /// SOLID (DIP): controllers/views hablan con la red a través de esta abstracción.
    /// </summary>
    public interface INetworkService
    {
        bool IsRunning { get; }
        int ConnectedPlayers { get; }

        void StartAsHost();
        void StartAsClient();
        void Shutdown();

        // ── Eventos ──────────────────────────────────────────────────────
        event Action OnHostStarted;
        event Action<int> OnPlayerCountChanged;
        event Action OnEnoughPlayersToStart;
        event Action<string> OnConnectionFailed;

        /// <summary>
        /// Fired cuando cambia la disponibilidad de la sala "RedesRoom".
        /// true  = sala existe, tiene lugar y acepta conexiones  → habilitar botón Unirse.
        /// false = sala no existe o está llena                   → deshabilitar botón Unirse.
        /// </summary>
        event Action<bool> OnRoomAvailabilityChanged;
    }
}
