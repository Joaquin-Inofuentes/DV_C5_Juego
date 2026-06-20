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

        public override void Render()
        {
            // TODO (other agent): read networked state and update _animator parameters,
            // e.g. _animator.SetFloat(PARAM_SPEED, velocity);
        }
    }
}
