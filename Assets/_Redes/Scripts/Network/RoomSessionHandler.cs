using System.Collections.Generic;
using Fusion;
using Redes.Core;

namespace Redes.Network
{
    /// <summary>
    /// SOLID/SRP: the ONLY thing this class decides is
    /// "create a new room" vs. "join an existing one", based on the session list.
    /// </summary>
    public class RoomSessionHandler : ISessionListHandler
    {
        private readonly HostNetworkService _service;
        private bool _starting = false;

        public RoomSessionHandler(HostNetworkService service)
        {
            _service = service;
        }

        public async void HandleSessionList(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (_starting) return;
            _starting = true;

            try
            {
                bool noSessions = sessionList == null || sessionList.Count == 0;
                var sceneMgr = runner.GetComponent<NetworkSceneManagerDefault>() ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

                var currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                var sceneRef = SceneRef.FromIndex(currentSceneIndex);

                if (noSessions)
                {
                    // --- CREATE PATH ---
                    // REQUIRED LOG -> "Se creo una sala llamada X"
                    RedesLog.Info(RedesLog.LOBBY, $"Se creo una sala llamada {GameConstants.DEFAULT_ROOM_NAME}");

                    // REQUIRED LOG -> "Se esta esperando al otro jugador"
                    RedesLog.Info(RedesLog.LOBBY, "Se esta esperando al otro jugador");

                    await runner.StartGame(new StartGameArgs {
                        GameMode = GameMode.Host,
                        SessionName = GameConstants.DEFAULT_ROOM_NAME,
                        PlayerCount = GameConstants.MAX_PLAYERS,
                        SceneManager = sceneMgr,
                        Scene = sceneRef
                    });
                }
                else
                {
                    // --- JOIN PATH ---
                    SessionInfo target = sessionList[0];

                    // REQUIRED LOG -> "Se unio a la sala de X nombre y X datos"
                    RedesLog.Info(RedesLog.LOBBY,
                        $"Se unio a la sala de {target.Name} nombre y {target.PlayerCount}/{target.MaxPlayers} datos");

                    await runner.StartGame(new StartGameArgs {
                        GameMode = GameMode.Client,
                        SessionName = target.Name,
                        SceneManager = sceneMgr,
                        Scene = sceneRef
                    });
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.LOBBY, $"[RoomSessionHandler.HandleSessionList] Excepción capturada en método. Mensaje: {ex.Message}\nCallstack:\n{ex.StackTrace}");
                _starting = false; // Reset starting so it can try again
            }
        }
    }
}
