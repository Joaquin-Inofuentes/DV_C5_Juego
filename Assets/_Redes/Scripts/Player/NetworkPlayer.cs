using Fusion;
using UnityEngine;
using Redes.Core;
using Redes.Network;

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

        [Networked] private NetworkButtons _previousButtons { get; set; }

        public override void Spawned()
        {
            RedesLog.Info(RedesLog.PLAYER, $">> NetworkPlayer.Spawned() InputAuthority={Object.InputAuthority} HasInputAuthority={Object.HasInputAuthority} HasStateAuthority={Object.HasStateAuthority}");
            RedesLog.Info(RedesLog.PLAYER, $"   Inicio el jugador {Object.InputAuthority}");

            Color playerColor = (Object.InputAuthority.PlayerId % 2 == 0) ? Color.blue : Color.red;
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = playerColor;
            }
            else
            {
                var renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = playerColor;
                }
            }

            if (Object.HasInputAuthority)
            {
                RedesLog.Info(RedesLog.PLAYER, $"   [LOCAL PLAYER] Bindando controles para jugador {Object.InputAuthority}");
                var pc = FindFirstObjectByType<Redes.Controllers.PlayerController>();
                pc?.Bind(this);

                if (Camera.main != null)
                {
                    Camera.main.transform.position = transform.position + new Vector3(0, 15f, -10f);
                    Camera.main.transform.rotation = Quaternion.Euler(60f, 0, 0);
                }
            }
        }

        public override void Render()
        {
            if (Object.HasInputAuthority && Camera.main != null)
            {
                Camera.main.transform.position = transform.position + new Vector3(0, 15f, -10f);
                Camera.main.transform.rotation = Quaternion.Euler(60f, 0, 0);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                _movement.SetInput(data);

                var pressed = data.Buttons.GetPressed(_previousButtons);
                _previousButtons = data.Buttons;

                if (pressed.IsSet(InputButton.Fire))
                {
                    _shooting.Fire();
                }

                if (pressed.IsSet(InputButton.Reload))
                {
                    _ammo.StartReload();
                }
            }
        }
    }
}
