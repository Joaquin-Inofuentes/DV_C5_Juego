using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Redes.Player;

namespace Redes.Views
{
    /// <summary>
    /// MVC - VIEW del cooldown radial de teleporte.
    ///
    /// Principio de responsabilidad única (SRP):
    ///   - Lee CooldownProgress del modelo (PlayerTeleport) en Update.
    ///   - Escucha OnTeleport del PlayerEventBus para disparar la animación "uso".
    ///   - NUNCA llama a lógica de negocio; solo dibuja la UI y escala el jugador.
    ///
    /// Colores:
    ///   - Amarillo  → listo (fillAmount == 1)
    ///   - Azul      → cargando (fillAmount < 1)
    ///   - Verde     → animación de uso (expand → shrink en < 0.5s)
    ///
    /// Escala del jugador:
    ///   - Al teleportarse: shrink (0.3s) → grow (0.3s) en la transform del jugador.
    /// </summary>
    public class TeleportCooldownView : MonoBehaviour
    {
        // ─── Referencias UI ──────────────────────────────────────────────
        [Header("UI Refs (asignadas por el Link tool)")]
        [SerializeField] private Image _radialImage;   // Image.Type.Filled Radial360
        [SerializeField] private Text  _labelText;     // "TELEPORT [SPACE]" / "Xs"

        // ─── Referencias Modelo ──────────────────────────────────────────
        [Header("Model Refs (asignadas por el Link tool / PlayerController)")]
        [SerializeField] private PlayerTeleport  _teleport;   // modelo
        [SerializeField] private PlayerEventBus  _eventBus;   // bus de eventos

        // ─── Colores ─────────────────────────────────────────────────────
        private static readonly Color ColorReady    = new Color(1.00f, 0.88f, 0.00f, 1f); // amarillo
        private static readonly Color ColorCharging = new Color(0.10f, 0.55f, 1.00f, 1f); // azul
        private static readonly Color ColorUsed     = new Color(0.20f, 1.00f, 0.45f, 1f); // verde

        // ─── Animación "uso" ─────────────────────────────────────────────
        private Coroutine _useAnimCoroutine;
        private Coroutine _scaleAnimCoroutine;
        private Transform _playerTransform;  // root del jugador (para escalar)
        private Vector3   _playerOriginalScale;

        private const float USE_ANIM_DURATION   = 0.45f; // segundos totales de la animación verde
        private const float SCALE_SHRINK_TIME    = 0.20f; // tiempo en achicarse
        private const float SCALE_GROW_TIME      = 0.25f; // tiempo en volver al tamaño original

        // ─── Lifecycle ───────────────────────────────────────────────────
        private void OnEnable()
        {
            if (_eventBus != null)
                _eventBus.OnTeleport += HandleTeleportUsed;
        }

        private void OnDisable()
        {
            if (_eventBus != null)
                _eventBus.OnTeleport -= HandleTeleportUsed;
        }

        private void Update()
        {
            if (_teleport == null || _radialImage == null) return;

            float progress = _teleport.CooldownProgress;

            // No sobreescribir durante la animación de "uso" (verde)
            if (_useAnimCoroutine != null) return;

            _radialImage.fillAmount = progress;

            bool isReady = progress >= 0.999f;
            _radialImage.color = isReady ? ColorReady : ColorCharging;

            if (_labelText != null)
            {
                if (isReady)
                {
                    _labelText.text = "TELEPORT\n[SPACE]";
                    _labelText.color = Color.white;
                }
                else
                {
                    // Calcular segundos restantes a partir del cooldown del runner
                    float remaining = (1f - progress) * Core.GameConstants.TELEPORT_COOLDOWN;
                    _labelText.text = $"TELEPORT\n{Mathf.CeilToInt(remaining)}s";
                    _labelText.color = new Color(0.7f, 0.85f, 1f); // azul claro
                }
            }
        }

        // ─── Binding ─────────────────────────────────────────────────────
        /// <summary>
        /// Llamado por PlayerController cuando vincula este View al jugador local.
        /// </summary>
        public void Bind(PlayerTeleport teleport, PlayerEventBus eventBus, Transform playerRoot)
        {
            // Desuscribir del anterior
            if (_eventBus != null)
                _eventBus.OnTeleport -= HandleTeleportUsed;

            _teleport        = teleport;
            _eventBus        = eventBus;
            _playerTransform = playerRoot;

            if (_playerTransform != null)
                _playerOriginalScale = _playerTransform.localScale;

            // Suscribir al nuevo
            if (_eventBus != null)
                _eventBus.OnTeleport += HandleTeleportUsed;
        }

        // ─── Evento: teleporte usado ──────────────────────────────────────
        private void HandleTeleportUsed(Vector3 origin, Vector3 destination)
        {
            // Animación radial verde expand → shrink
            if (_useAnimCoroutine != null)
                StopCoroutine(_useAnimCoroutine);
            _useAnimCoroutine = StartCoroutine(UseAnimCoroutine());

            // Animación de escala del jugador
            if (_playerTransform != null)
            {
                if (_scaleAnimCoroutine != null)
                    StopCoroutine(_scaleAnimCoroutine);
                _scaleAnimCoroutine = StartCoroutine(PlayerScaleCoroutine());
            }
        }

        // ─── Corrutinas ───────────────────────────────────────────────────
        /// <summary>
        /// Animación del radial: verde, expande de 1→1.3, luego vuelve a 1, en USE_ANIM_DURATION segundos.
        /// </summary>
        private IEnumerator UseAnimCoroutine()
        {
            if (_radialImage == null) { _useAnimCoroutine = null; yield break; }

            _radialImage.color      = ColorUsed;
            _radialImage.fillAmount = 1f;

            float half    = USE_ANIM_DURATION * 0.5f;
            float elapsed = 0f;

            // Fase 1: expand (escala UI del radial sube de 1x → 1.3x)
            Vector3 baseScale = _radialImage.rectTransform.localScale;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float s = Mathf.Lerp(1f, 1.3f, t);
                _radialImage.rectTransform.localScale = baseScale * s;
                yield return null;
            }

            elapsed = 0f;

            // Fase 2: shrink (escala UI vuelve de 1.3x → 1x)
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float s = Mathf.Lerp(1.3f, 1f, t);
                _radialImage.rectTransform.localScale = baseScale * s;
                yield return null;
            }

            _radialImage.rectTransform.localScale = baseScale;
            _useAnimCoroutine = null;
        }

        /// <summary>
        /// Animación de escala del jugador: se achica (~0.3x) al inicio y vuelve al 100% al llegar.
        /// Total: SCALE_SHRINK_TIME + SCALE_GROW_TIME ≈ 0.45s.
        /// </summary>
        private IEnumerator PlayerScaleCoroutine()
        {
            if (_playerTransform == null) { _scaleAnimCoroutine = null; yield break; }

            Vector3 originalScale = _playerOriginalScale;
            Vector3 shrunkScale   = originalScale * 0.3f;

            float elapsed = 0f;

            // Fase 1: achicarse
            Vector3 startScale = _playerTransform.localScale;
            while (elapsed < SCALE_SHRINK_TIME)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / SCALE_SHRINK_TIME;
                _playerTransform.localScale = Vector3.Lerp(startScale, shrunkScale, t);
                yield return null;
            }
            _playerTransform.localScale = shrunkScale;

            elapsed = 0f;

            // Fase 2: agrandarse de vuelta
            while (elapsed < SCALE_GROW_TIME)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / SCALE_GROW_TIME;
                _playerTransform.localScale = Vector3.Lerp(shrunkScale, originalScale, t);
                yield return null;
            }
            _playerTransform.localScale = originalScale;
            _scaleAnimCoroutine = null;
        }
    }
}
