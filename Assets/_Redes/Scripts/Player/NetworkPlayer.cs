using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Player
{
    /// <summary>
    /// Root NetworkBehaviour for a player (the prefab's "brain"/facade).
    ///
    /// SOLID/SRP: it does NOT implement movement/shooting/health itself; it just
    /// holds references to the sibling systems and exposes the local-player hook.
    /// Each concrete system (movement, shooting, health, ammo, animation) is its
    /// own component so they can change independently.
    ///
    /// This is a Network Object (the prefab carries a NetworkObject component).
    /// Logic is implemented by another agent.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Player systems (auto-assigned by the Prefab tool on the same prefab)")]
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerShooting _shooting;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private AmmoSystem _ammo;
        [SerializeField] private PlayerAnimationController _animation;

        public PlayerMovement Movement => _movement;
        public PlayerShooting Shooting => _shooting;
        public PlayerHealth Health => _health;
        public AmmoSystem Ammo => _ammo;
        public PlayerAnimationController Animation => _animation;

        public override void Spawned()
        {
            // REQUIRED LOG -> "Inicio el jugador A" / "Inicio el Jugador B"
            // (We tag with the InputAuthority; player 0 == A, player 1 == B.)
            RedesLog.Info(RedesLog.PLAYER, $"Inicio el jugador {Object.InputAuthority}");

            if (Object.HasInputAuthority)
            {
                // TODO (other agent): find the scene PlayerController and call Bind(this);
                // and set up the local camera follow.
            }
        }

        public override void FixedUpdateNetwork()
        {
            // TODO (other agent): read GetInput() and drive _movement / _shooting / _ammo.
            // Running here keeps server & clients in sync (no desfase).
        }
    }
}
