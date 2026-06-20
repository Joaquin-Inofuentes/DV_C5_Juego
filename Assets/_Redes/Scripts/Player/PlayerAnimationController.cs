using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Player
{
    /// <summary>
    /// ANIMATION CONTROL (assignment requirement).
    ///
    /// Reads the networked player state (moving, shooting, dead) and drives the
    /// Animator parameters. Because it reads networked values, the animation is
    /// consistent on every client. Use Render() (visual, runs every frame) rather
    /// than the simulation tick for smooth animation.
    /// Logic is implemented by another agent.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : NetworkBehaviour
    {
        [Header("Refs (assigned by the Prefab tool)")]
        [SerializeField] private Animator _animator;

        // Common animator parameter names the other agent will set.
        // Kept as constants so View/animation stay consistent.
        public const string PARAM_SPEED   = "Speed";
        public const string PARAM_SHOOT   = "Shoot";
        public const string PARAM_DEAD    = "Dead";

        private PlayerMovement _movement;
        private PlayerHealth _health;
        private PlayerShooting _shooting;
        private int _lastShootCount;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _health = GetComponent<PlayerHealth>();
            _shooting = GetComponent<PlayerShooting>();
        }

        public override void Render()
        {
            if (_animator == null) return;

            if (_movement != null)
            {
                _animator.SetFloat(PARAM_SPEED, _movement.NetworkVelocity.magnitude);
            }

            if (_health != null)
            {
                _animator.SetBool(PARAM_DEAD, !_health.IsAlive);
            }

            if (_shooting != null && _shooting.ShootCount != _lastShootCount)
            {
                _lastShootCount = _shooting.ShootCount;
                _animator.SetTrigger(PARAM_SHOOT);
            }
        }
    }
}
