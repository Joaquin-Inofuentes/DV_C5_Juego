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
        private PlayerEventBus _eventBus;

        [Networked] public Vector3 NetworkVelocity { get; set; }

        private void Awake()
        {
            _eventBus = GetComponent<PlayerEventBus>();
        }

        public override void FixedUpdateNetwork()
        {
            if (Runner == null || !Runner.IsRunning || Runner.SessionInfo.PlayerCount < 2)
            {
                NetworkVelocity = Vector3.zero;
                return;
            }

            if (GetInput(out NetworkInputData data))
            {
                Vector3 dir = new Vector3(data.Move.x, 0, data.Move.y);
                Vector3 moveVelocity = dir.normalized * _moveSpeed;

                // Move transform directly. Rigidbody is set to kinematic.
                transform.position += moveVelocity * Runner.DeltaTime;
                NetworkVelocity = moveVelocity;

                if (_eventBus != null && moveVelocity.sqrMagnitude > 0.01f)
                {
                    _eventBus.TriggerMove(moveVelocity);
                }

                Vector3 lookPos = new Vector3(data.AimDirection.x, transform.position.y, data.AimDirection.y);
                Vector3 lookDir = lookPos - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }
    }
}
