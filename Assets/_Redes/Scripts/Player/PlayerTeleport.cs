using Fusion;
using UnityEngine;
using Redes.Core;

namespace Redes.Player
{
    /// <summary>
    /// MECÁNICA EXTRA: Teletransporte (Teleport).
    ///
    /// Al presionar SPACE (y si el cooldown expiró), el jugador se teletransporta
    /// a una posición aleatoria dentro de la arena (radio TELEPORT_RANGE).
    ///
    /// - [Networked] TickTimer TeleportCooldown → cooldown sincronizado en red.
    /// - [Networked] Vector3 LastTeleportOrigin / Destination → para VFX en clientes.
    /// - OnChangedRender notifica a la vista para mostrar el efecto visual.
    /// - Logs con PlayerRef para identificar al actor en todos los clientes.
    /// </summary>
    public class PlayerTeleport : NetworkBehaviour
    {
        // ─── Estado de red ─────────────────────────────────────────────
        [Networked] public TickTimer  TeleportCooldown    { get; set; }
        [Networked, OnChangedRender(nameof(OnTeleportChangedRender))]
        public Vector3 LastTeleportDest { get; set; }
        [Networked] public Vector3 LastTeleportOrigin { get; set; }

        // ─── Config ─────────────────────────────────────────────────────
        [Header("Tuning")]
        [SerializeField] private float _range    = GameConstants.TELEPORT_RANGE;
        [SerializeField] private float _cooldown = GameConstants.TELEPORT_COOLDOWN;

        // ─── VFX ────────────────────────────────────────────────────────
        [Header("VFX Prefabs (asignados por el Editor Tool)")]
        [SerializeField] private GameObject _teleportOriginVfxPrefab;
        [SerializeField] private GameObject _teleportDestVfxPrefab;

        // ─── Refs ────────────────────────────────────────────────────────
        private PlayerEventBus _eventBus;

        private void Awake()
        {
            _eventBus = GetComponent<PlayerEventBus>();
        }

        // ─── Propiedad pública para UI ───────────────────────────────────
        /// <summary>Progreso del cooldown entre 0 (recargando) y 1 (listo).</summary>
        public float CooldownProgress
        {
            get
            {
                if (Runner == null) return 1f;
                float rem = TeleportCooldown.RemainingTime(Runner) ?? 0f;
                return Mathf.Clamp01(1f - (rem / _cooldown));
            }
        }

        public bool IsReady => TeleportCooldown.ExpiredOrNotRunning(Runner);

        // ─── Servidor: leer input y ejecutar teleporte ──────────────────
        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            if (Runner.SessionInfo.PlayerCount < 2) return;

            if (GetInput(out Network.NetworkInputData data))
            {
                bool wantsTeleport = data.Buttons.IsSet(Network.InputButton.Teleport);

                if (wantsTeleport && TeleportCooldown.ExpiredOrNotRunning(Runner))
                {
                    ExecuteTeleport();
                }
            }
        }

        private void ExecuteTeleport()
        {
            Vector3 origin = transform.position;

            // Posición aleatoria en el plano XZ dentro del rango
            Vector2 rand2D = Random.insideUnitCircle * _range;
            Vector3 dest   = new Vector3(
                origin.x + rand2D.x,
                origin.y,
                origin.z + rand2D.y);

            // Guardar en red (dispara OnTeleportChangedRender en todos los clientes)
            LastTeleportOrigin = origin;
            LastTeleportDest   = dest;

            // Mover al jugador
            transform.position = dest;

            // Iniciar cooldown
            TeleportCooldown = TickTimer.CreateFromSeconds(Runner, _cooldown);

            RedesLog.Info(RedesLog.PLAYER,
                $"[Teleport] Jugador {Object.InputAuthority} (ActorId={Object.InputAuthority.PlayerId}) " +
                $"teletransportado de {origin:F1} → {dest:F1}  (cooldown={_cooldown}s)");
        }

        // ─── Render: VFX en todos los clientes ──────────────────────────
        private void OnTeleportChangedRender()
        {
            string contexto = Object.HasInputAuthority ? "Local" : "Remoto";
            RedesLog.Info(RedesLog.PLAYER,
                $"[Teleport VFX] Jugador {Object.InputAuthority} ({contexto}) " +
                $"{LastTeleportOrigin:F1} → {LastTeleportDest:F1}");

            // Partícula de origen
            if (_teleportOriginVfxPrefab != null)
                Instantiate(_teleportOriginVfxPrefab, LastTeleportOrigin, Quaternion.identity);

            // Partícula de destino
            if (_teleportDestVfxPrefab != null)
                Instantiate(_teleportDestVfxPrefab, LastTeleportDest, Quaternion.identity);

            // Notificar vistas
            _eventBus?.TriggerTeleport(LastTeleportOrigin, LastTeleportDest);
        }
    }
}
