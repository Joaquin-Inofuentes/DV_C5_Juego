using System.Collections.Generic;
using Fusion;
using Redes.Core;

namespace Redes.Network
{
    /// <summary>
    /// SOLID/SRP: the ONLY thing this class decides is
    /// "create a new room" vs. "join an existing one", based on the session list.
    ///
    /// It is a plain C# class (not a MonoBehaviour) so it is trivially testable.
    /// It holds a reference to HostNetworkService to ask it to start a session.
    ///
    /// Logic is implemented by another agent; only the required logs + structure
    /// are present here.
    /// </summary>
    public class RoomSessionHandler : ISessionListHandler
    {
        private readonly HostNetworkService _service;

        public RoomSessionHandler(HostNetworkService service)
        {
            _service = service;
        }

        public void HandleSessionList(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            bool noSessions = sessionList == null || sessionList.Count == 0;

            if (noSessions)
            {
                // --- CREATE PATH ---
                // REQUIRED LOG -> "Se creo una sala llamada X"
                RedesLog.Info(RedesLog.LOBBY, $"Se creo una sala llamada {GameConstants.DEFAULT_ROOM_NAME}");

                // REQUIRED LOG -> "Se esta esperando al otro jugador"
                RedesLog.Info(RedesLog.LOBBY, "Se esta esperando al otro jugador");

                // TODO (other agent): create the host session, e.g.
                // _service.Runner.StartGame(new StartGameArgs {
                //     GameMode = GameMode.Host,
                //     SessionName = GameConstants.DEFAULT_ROOM_NAME });
            }
            else
            {
                // --- JOIN PATH ---
                SessionInfo target = sessionList[0];

                // REQUIRED LOG -> "Se unio a la sala de X nombre y X datos"
                RedesLog.Info(RedesLog.LOBBY,
                    $"Se unio a la sala de {target.Name} nombre y {target.PlayerCount}/{target.MaxPlayers} datos");

                // TODO (other agent): join the session, e.g.
                // _service.Runner.StartGame(new StartGameArgs {
                //     GameMode = GameMode.Client,
                //     SessionName = target.Name });
            }
        }
    }
}
