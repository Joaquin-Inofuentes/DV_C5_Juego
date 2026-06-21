using System;
using System.Collections.Generic;
using Fusion;

namespace Redes.Network
{
    /// <summary>
    /// Abstracción del servicio de red para desacoplar a los controladores
    /// de la librería subyacente (Fusion 2).
    /// </summary>
    public interface INetworkService
    {
        bool IsRunning { get; }
        int ConnectedPlayers { get; }

        void StartAsHost(string sessionName);
        void StartAsClient(string sessionName);
        void Shutdown();

        // ── Eventos ──────────────────────────────────────────────────────
        event Action         OnHostStarted;
        event Action<int>    OnPlayerCountChanged;
        event Action         OnEnoughPlayersToStart;
        event Action<string> OnConnectionFailed;
        event Action<List<SessionInfo>> OnRoomListUpdated;
    }
}
