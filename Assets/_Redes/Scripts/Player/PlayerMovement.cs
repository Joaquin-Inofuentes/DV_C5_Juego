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
        private CharacterController _controller;
        private PlayerEventBus _eventBus;

        [Networked] public Vector3 NetworkVelocity { get; set; }

        private void Awake()
        {
            _eventBus = GetComponent<PlayerEventBus>();
            _controller = GetComponent<CharacterController>();
            
            if (_controller != null)
            {
                // DESACTIVAR INICIALMENTE para que NetworkTransform pueda hacer rollback libremente.
                // Solo lo activaremos 1 milisegundo durante el Move().
                _controller.enabled = false;
            }
        }

        public override void Spawned()
        {
            if (_controller != null)
            {
                // El servidor (StateAuthority) necesita el collider activo para que las balas (OverlapSphere) lo detecten.
                // Los clientes lo mantienen apagado por defecto para permitir el Rollback de NetworkTransform.
                _controller.enabled = Object.HasStateAuthority;
            }
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

                if (_controller != null)
                {
                    // Si somos cliente prediciendo, lo activamos temporalmente.
                    // Si somos servidor, ya estaba activado.
                    bool wasEnabled = _controller.enabled;
                    if (!wasEnabled) _controller.enabled = true;

                    if (!_controller.isGrounded)
                    {
                        moveVelocity.y = -9.81f;
                    }

                    _controller.Move(moveVelocity * Runner.DeltaTime);
                    NetworkVelocity = _controller.velocity;

                    // Volvemos a desactivar solo si lo activamos temporalmente (cliente prediciendo).
                    if (!wasEnabled) _controller.enabled = false;
                }
                else
                {
                    transform.position += moveVelocity * Runner.DeltaTime;
                    NetworkVelocity = moveVelocity;
                }

                Vector3 horizontalVelocity = moveVelocity;
                horizontalVelocity.y = 0;
                
                if (_eventBus != null && horizontalVelocity.sqrMagnitude > 0.01f)
                {
                    _eventBus.TriggerMove(horizontalVelocity);
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
