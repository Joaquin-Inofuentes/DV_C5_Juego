using Fusion;
using UnityEngine;
using Redes.Core;
using Redes.Network;
using Redes.Controllers;

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
    public class NetworkPlayer : NetworkBehaviour, Models.IEntityDisplayModel
    {
        // IEntityDisplayModel implementation
        public Vector3 WorldPosition => transform.position;
        public float HealthProgress => _health != null ? (float)_health.CurrentHealth / GameConstants.DEFAULT_MAX_HEALTH : 1f;
        public bool IsActive => Object != null && Object.IsValid && _health != null && _health.IsAlive;
        public float ReloadProgress => _ammo != null ? _ammo.ReloadProgress : 1f;
        public bool IsReloading => _ammo != null && _ammo.IsReloading;
        
        [Networked, OnChangedRender(nameof(OnNicknameChangedRender))] private NetworkString<_16> NetNickname { get; set; }
        public string Nickname => string.IsNullOrEmpty(NetNickname.ToString()) ? $"Player {Object.InputAuthority.PlayerId}" : NetNickname.ToString();

        private void OnNicknameChangedRender()
        {
            RedesLog.Info(RedesLog.PLAYER, $"[Nickname Sync] Player {Object.InputAuthority} nickname synchronized: {NetNickname}");
            
            Debug.Log($"Jugador {NetNickname} se conecto");
            
            if (Runner != null && Runner.SessionInfo.IsValid)
            {
                // In Fusion Host-Client mode, PlayerId 1 is the Host creator
                bool isHost = (Object.InputAuthority.PlayerId == 1);
                if (!isHost)
                {
                    Debug.Log($"Se unio jugador {NetNickname} a la sala {Runner.SessionInfo.Name}");
                }
            }
        }

        [Header("Player systems (auto-assigned by the Prefab tool on the same prefab)")]
        [SerializeField] private PlayerMovement  _movement;
        [SerializeField] private PlayerShooting  _shooting;
        [SerializeField] private PlayerHealth    _health;
        [SerializeField] private AmmoSystem      _ammo;
        [SerializeField] private PlayerEventBus  _eventBus;
        [SerializeField] private PlayerCrouch    _crouch;
        [SerializeField] private PlayerTeleport  _teleport;

        public PlayerMovement Movement  => _movement;
        public PlayerShooting Shooting  => _shooting;
        public PlayerHealth   Health    => _health;
        public AmmoSystem     Ammo      => _ammo;
        public PlayerEventBus EventBus  => _eventBus;
        public PlayerCrouch   Crouch    => _crouch;
        public PlayerTeleport Teleport  => _teleport;

        [Networked] private NetworkButtons _previousButtons { get; set; }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSetNickname(string nickname)
        {
            string finalNickname = nickname;
            
            // Check for duplicates on the server among all currently spawned NetworkPlayers
            int attempts = 1;
            bool nameExists = true;
            while (nameExists)
            {
                nameExists = false;
                foreach (var otherPlayer in Runner.ActivePlayers)
                {
                    if (otherPlayer == Object.InputAuthority) continue;
                    var otherNp = Runner.GetPlayerObject(otherPlayer)?.GetComponent<NetworkPlayer>();
                    if (otherNp != null && otherNp != this && otherNp.Nickname == finalNickname)
                    {
                        nameExists = true;
                        attempts++;
                        finalNickname = $"{nickname} {attempts}";
                        break;
                    }
                }
            }

            NetNickname = finalNickname;
            RedesLog.Info(RedesLog.PLAYER, $"[Server Nickname Set] Player {Object.InputAuthority} set to '{finalNickname}'");
        }

        public override void Spawned()
        {
            RedesLog.Info(RedesLog.PLAYER, $">> NetworkPlayer.Spawned() InputAuthority={Object.InputAuthority} HasInputAuthority={Object.HasInputAuthority} HasStateAuthority={Object.HasStateAuthority}");
            RedesLog.Info(RedesLog.PLAYER, $"   Inicio el jugador {Object.InputAuthority}");

            if (_eventBus != null)
            {
                _eventBus.TriggerSpawned();
            }

            if (Object.HasInputAuthority)
            {
                RedesLog.Info(RedesLog.PLAYER, $"[Nickname Setup] Sending RpcSetNickname for {Object.InputAuthority} to server with nickname '{GameFlowController.LocalUsername}'");
                RpcSetNickname(GameFlowController.LocalUsername);
            }

            Color playerColor = (Object.InputAuthority.PlayerId % 2 == 0) 
                ? new Color(0.8f, 0.9f, 1f, 1f)      // Soft cold ice-blue tint
                : new Color(1f, 0.85f, 0.85f, 1f);   // Soft warm rose-red tint
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
            // Block movement or shooting until at least 2 players are connected in the session
            if (Runner == null || !Runner.IsRunning || Runner.SessionInfo.PlayerCount < 2)
            {
                return;
            }

            if (GetInput(out NetworkInputData data))
            {

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

                // Crouch: se procesa en PlayerCrouch.FixedUpdateNetwork (lee el botón directo, no pressed)
                // Teleport: se procesa en PlayerTeleport.FixedUpdateNetwork (requiere cooldown check)
                // Ambos usan GetInput() internamente, por lo que no hay doble lectura.
            }
        }
    }
}
