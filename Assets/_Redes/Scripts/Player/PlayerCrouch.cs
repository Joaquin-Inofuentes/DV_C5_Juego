using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Player
{
    /// <summary>
    /// MECÁNICA EXTRA: Agacharse (Crouch).
    ///
    /// El jugador mantiene presionado CTRL para agacharse:
    ///   - Su escala Y se reduce al 50 % → hitbox más pequeño.
    ///   - Puede moverse mientras está agachado.
    ///   - El estado IsCrouching está [Networked] → todos los clientes ven la animación.
    ///
    /// El input se lee en FixedUpdateNetwork (server-authoritative) para evitar
    /// cualquier desfase entre host y cliente.
    /// </summary>
    public class PlayerCrouch : NetworkBehaviour
    {
        // ─── Estado de red ─────────────────────────────────────────────
        [Networked, OnChangedRender(nameof(OnCrouchChangedRender))]
        public NetworkBool IsCrouching { get; set; }

        // ─── Escala ─────────────────────────────────────────────────────
        private Vector3 _standingScale  = Vector3.one;
        private Vector3 _crouchingScale;

        // ─── Refs opcionales ────────────────────────────────────────────
        private CharacterController _controller;
        private PlayerEventBus _eventBus;

        private float _standingHeight;
        private float _crouchingHeight;
        private Vector3 _standingCenter;
        private Vector3 _crouchingCenter;

        // Velocidad de interpolación de escala
        private const float SCALE_SPEED = 10f;
        private float _currentAlpha = 1f;

        private void Awake()
        {
            _eventBus      = GetComponent<PlayerEventBus>();
            _controller    = GetComponent<CharacterController>();
            _standingScale = transform.localScale;
            _crouchingScale = new Vector3(
                _standingScale.x,
                _standingScale.y * GameConstants.CROUCH_SCALE_Y,
                _standingScale.z);

            if (_controller != null)
            {
                _standingHeight = _controller.height;
                _crouchingHeight = _standingHeight * GameConstants.CROUCH_SCALE_Y;
                _standingCenter = _controller.center;
                _crouchingCenter = new Vector3(_standingCenter.x, _standingCenter.y * GameConstants.CROUCH_SCALE_Y, _standingCenter.z);
            }
        }

        // ─── Servidor: leer input y actualizar estado ───────────────────
        public override void FixedUpdateNetwork()
        {
            // Sincronizar el tamaño físico del CharacterController en el tick de físicas
            if (_controller != null && _controller.enabled)
            {
                _controller.height = IsCrouching ? _crouchingHeight : _standingHeight;
                _controller.center = IsCrouching ? _crouchingCenter : _standingCenter;
            }

            if (!Object.HasStateAuthority) return;
            if (Runner.SessionInfo.PlayerCount < 2) return;

            if (GetInput(out Network.NetworkInputData data))
            {
                bool wantsCrouch = data.Buttons.IsSet(Network.InputButton.Crouch);

                if (wantsCrouch != IsCrouching)
                {
                    IsCrouching = wantsCrouch;

                    string accion = wantsCrouch ? "se agachó" : "se levantó";
                    RedesLog.Info(RedesLog.PLAYER,
                        $"[Crouch] Jugador {Object.InputAuthority} (ActorId={Object.InputAuthority.PlayerId}) {accion}");
                }
            }
        }

        // ─── Render: interpolar escala en todos los clientes ───────────
        public override void Render()
        {
            Vector3 targetScale = IsCrouching ? _crouchingScale : _standingScale;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * SCALE_SPEED);

            float targetAlpha = IsCrouching ? 0.6f : 1.0f;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, Time.deltaTime * SCALE_SPEED);
            SetVisualsAlpha(_currentAlpha);
        }

        private void SetVisualsAlpha(float alpha)
        {
            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in spriteRenderers)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r is SpriteRenderer) continue;
                if (r.material != null)
                {
                    Color c = r.material.color;
                    c.a = alpha;
                    r.material.color = c;
                }
            }
        }

        // ─── Callback de cambio de red ───────────────────────────────────
        private void OnCrouchChangedRender()
        {
            _eventBus?.TriggerCrouch(IsCrouching);

            string contexto = Object.HasInputAuthority ? "Local" : "Remoto";
            string status = IsCrouching ? "Agachado (Transparencia 0.6)" : "De pie (Transparencia 1.0)";
            Debug.Log($"[CROUCH] Jugador {Object.InputAuthority} ({contexto}) estado visual cambiado a: {status}");
        }
    }
}
