using System.Collections.Generic;
using Fusion;

namespace Redes.Network
{
    /// <summary>
    /// SOLID (Single Responsibility): isolates the "what do we do with the
    /// session list" decision (create a new room vs. join an existing one)
    /// away from the runner callbacks plumbing.
    /// Logic is implemented by another agent.
    /// </summary>
    public interface ISessionListHandler
    {
        /// <summary>
        /// Called whenever Fusion reports the available sessions.
        /// Decides whether to CREATE a room (0 sessions) or JOIN one (X sessions).
        /// </summary>
        void HandleSessionList(NetworkRunner runner, List<SessionInfo> sessionList);
    }
}
