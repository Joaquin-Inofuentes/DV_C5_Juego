using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Player
{
    /// <summary>
    /// Top-down movement (one of the "seen in class" mechanics).
    /// NetworkBehaviour so movement is simulated identically on host + clients
    /// (no desfase). Logic is implemented by another agent.
    /// </summary>
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("Tuning")]
        [SerializeField] private float _moveSpeed = GameConstants.DEFAULT_MOVE_SPEED;

        // Optional cached refs (assigned by the Prefab tool).
        [SerializeField] private Rigidbody _body;

        public override void FixedUpdateNetwork()
        {
            // TODO (other agent): read input direction and move using TickTimer/physics.
            // Example flag log when needed:
            // RedesLog.Info(RedesLog.PLAYER, "El jugador se movio");
        }
    }
}
