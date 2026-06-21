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

        void StartAsHost();   // Jugador 1: crea sala GameMode.Host
        void StartAsClient(); // Jugador 2: se une a sala GameMode.Client
        void Shutdown();

        event Action OnHostStarted;
        event Action<int> OnPlayerCountChanged;
        event Action OnEnoughPlayersToStart;
        event Action<string> OnConnectionFailed; // startGame fallo -> motivo
    }
}
