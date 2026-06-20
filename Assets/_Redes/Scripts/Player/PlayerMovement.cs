using Fusion;
using UnityEngine;
using Redes.Core;
using Redes.Network;

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

        [Networked] public Vector3 NetworkVelocity { get; set; }

        private NetworkInputData _lastInput;

        public void SetInput(NetworkInputData data)
        {
            _lastInput = data;
        }

        public override void FixedUpdateNetwork()
        {
            Vector3 dir = new Vector3(_lastInput.Move.x, 0, _lastInput.Move.y);
            Vector3 moveVelocity = dir.normalized * _moveSpeed;

            if (_body != null)
            {
                _body.velocity = moveVelocity;
            }
            else
            {
                transform.position += moveVelocity * Runner.DeltaTime;
            }
            NetworkVelocity = moveVelocity;

            if (moveVelocity.sqrMagnitude > 0.01f)
            {
                RedesLog.Info(RedesLog.PLAYER, "El jugador se movio");
            }

            Vector3 lookPos = new Vector3(_lastInput.AimDirection.x, transform.position.y, _lastInput.AimDirection.y);
            Vector3 lookDir = lookPos - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }
}
