using UnityEngine;
using Game.Sensors;
using Game.Core;

namespace Game.Squad
{
    /// <summary>
    /// Extensión parcial de UnitController con lógica de revivimiento.
    /// Este archivo contiene:
    /// - Propiedades relacionadas al revivimiento (isDown, revivalTimer, etc.)
    /// - Métodos para entrar/salir del estado de caído
    /// - Lógica de input del jugador para revivir con barra espaciadora
    /// - Detección de aliados cercanos que pueden revivir
    /// </summary>
    public partial class UnitController : MonoBehaviour, IDaniable, IDetectable
    {
        [Header("=== REVIVAL SYSTEM ===")]
        [SerializeField] private bool isDown = false;
        [SerializeField] private float revivalInputTimer = 0f;
        [SerializeField] private float revivalRequiredDuration = 3f;
        [SerializeField] private float revivalDetectionRange = 3f;

        private UnitController lastDamagedAllyTarget; // Para tracking del objetivo a revivir
        private RevivalBarView revivalBarView;

        // =====================================================
        // MÉTODOS PÚBLICOS DE REVIVIMIENTO
        // =====================================================

        /// <summary>El soldado entra en estado caído (HP <= 0)</summary>
        public void EnterDamagedState()
        {
            LogMethodEntry($"[EnterDamagedState] {name} entrando en estado CAIDO");
            isDown = true;

            // Desactivar el collider propio para no recibir balas mientras está caído
            if (unitCollider != null)
            {
                unitCollider.enabled = false;
                LogMethodEntry($"[EnterDamagedState] Collider desactivado en {name}");
            }

            // Cambiar a estado caído
            CambiarEstado(new DamagedState());

            // Obtener o crear la barra de revivimiento
            revivalBarView = GetComponent<RevivalBarView>();
            if (revivalBarView == null)
            {
                revivalBarView = gameObject.AddComponent<RevivalBarView>();
                LogMethodEntry($"[EnterDamagedState] RevivalBarView creada dinámicamente");
            }

            LogMethodEntry($"[EnterDamagedState] {name} completamente CAIDO - esperando revivimiento");
        }

        /// <summary>El soldado sale del estado caído (fue revivido)</summary>
        public void ExitDamagedState()
        {
            LogMethodEntry($"[ExitDamagedState] {name} siendo revivido");
            isDown = false;
            revivalInputTimer = 0f;

            // Reactivar el collider propio para volver a recibir balas
            if (unitCollider != null)
            {
                unitCollider.enabled = true;
                LogMethodEntry($"[ExitDamagedState] Collider reactivado en {name}");
            }

            // La salud ya fue restaurada por el RevivingState
            LogMethodEntry($"[ExitDamagedState] {name} ahora está VIVO nuevamente (HP: {model.healthActual}/{model.healthMax})");
        }

        /// <summary>Verifica si el soldado está caído</summary>
        public bool IsDown() => isDown;

        // =====================================================
        // LÓGICA DE INPUT DEL JUGADOR (LÍDER)
        // =====================================================

        /// <summary>
        /// Ejecutado desde LiderandoState.Update()
        /// El líder intenta revivir a un aliado cercano caído presionando barra espaciadora
        /// </summary>
        public void UpdateLeaderRevivalInput()
        {
            if (!model.IsLeader || model.IsDown) return;

            // Detectar aliado caído cercano
            UnitController damagedAlly = FindClosestDamagedAlly();

            if (damagedAlly != null)
            {
                float distance = Vector3.Distance(transform.position, damagedAlly.transform.position);

                if (distance <= revivalDetectionRange)
                {
                    HandleRevivalInput(damagedAlly);
                }
                else
                {
                    ResetRevivalInput();
                }
            }
            else
            {
                ResetRevivalInput();
            }
        }

        private void HandleRevivalInput(UnitController damagedAlly)
        {
            if (GEN_Inputs.Instance == null) return;

            // Si está presionando barra espaciadora
            if (GEN_Inputs.Instance.RavivicionInput)
            {
                if (lastDamagedAllyTarget != damagedAlly)
                {
                    // Nuevo objetivo
                    lastDamagedAllyTarget = damagedAlly;
                    revivalInputTimer = 0f;
                    LogMethodEntry($"[HandleRevivalInput] Líder comenzando a revivir a {damagedAlly.name}");
                }

                revivalInputTimer += Time.deltaTime;
                LogMethodEntry($"[HandleRevivalInput] Reviviendo a {damagedAlly.name}. Progreso: {revivalInputTimer:F2}/{revivalRequiredDuration:F2}s");

                // Actualizar progreso visual en la view del caído
                damagedAlly.view.revivalProgress = revivalInputTimer / revivalRequiredDuration;

                if (revivalInputTimer >= revivalRequiredDuration)
                {
                    CompleteLeaderRevival(damagedAlly);
                }
            }
            else
            {
                // Soltó la tecla
                if (revivalInputTimer > 0)
                {
                    LogMethodEntry($"[HandleRevivalInput] Líder soltó barra espaciadora. Revivimiento CANCELADO");
                    damagedAlly.view.revivalProgress = 0f;
                }
                ResetRevivalInput();
            }
        }

        private void CompleteLeaderRevival(UnitController damagedAlly)
        {
            LogMethodEntry($"[CompleteLeaderRevival] Líder {name} ha revivido a {damagedAlly.name}");

            damagedAlly.model.ReviveHealth();
            LogMethodEntry($"[CompleteLeaderRevival] Salud de {damagedAlly.name} restaurada: {damagedAlly.model.healthActual}/{damagedAlly.model.healthMax}");

            damagedAlly.view.OnRevivalComplete();
            damagedAlly.ExitDamagedState();
            damagedAlly.CambiarEstado(new SeguirFormacionState());

            ResetRevivalInput();
            LogMethodEntry($"[CompleteLeaderRevival] Soldado {damagedAlly.name} revivido por {name}");
        }

        private void ResetRevivalInput()
        {
            revivalInputTimer = 0f;
            lastDamagedAllyTarget = null;
        }

        // =====================================================
        // DETECCIÓN DE ALIADOS CERCANOS PARA REVIVIR
        // =====================================================

        /// <summary>Busca el aliado caído más cercano al rango de revivimiento</summary>
        private UnitController FindClosestDamagedAlly()
        {
            UnitController[] allUnits = FindObjectsOfType<UnitController>();
            UnitController closest = null;
            float closestDistance = revivalDetectionRange;

            foreach (UnitController unit in allUnits)
            {
                if (unit == this || !unit.isActiveAndEnabled) continue;
                if (unit.model.team != model.team) continue; // Solo aliados
                if (!unit.IsDown()) continue; // Solo caídos

                float distance = Vector3.Distance(transform.position, unit.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = unit;
                }
            }

            return closest;
        }

        /// <summary>
        /// Verifica si este aliado puede revivar a un compañero caído.
        /// Retorna true si está cerca, no está atacando y el objetivo está caído.
        /// </summary>
        public bool CanReviveAlly(UnitController damagedAlly)
        {
            if (damagedAlly == null || !damagedAlly.IsDown()) return false;
            if (model.IsLeader) return false; // El líder revive con input
            if (model.IsDown) return false; // No puedo revivir si estoy caído

            float distance = Vector3.Distance(transform.position, damagedAlly.transform.position);
            if (distance > revivalDetectionRange) return false;

            // Verificar si está en combate
            bool isInCombat = _currentStateLogic is AtacarState || _currentStateLogic is PerseguirState;
            if (isInCombat)
            {
                LogMethodEntry($"[CanReviveAlly] {name} está en combate, no puede revivir a {damagedAlly.name}");
                return false;
            }

            LogMethodEntry($"[CanReviveAlly] {name} PUEDE revivir a {damagedAlly.name} - distancia: {distance:F2}m");
            return true;
        }

        /// <summary>
        /// Inicia el revivimiento de un aliado (para aliados IA, no el líder)
        /// </summary>
        public void StartRevivingAlly(UnitController damagedAlly)
        {
            if (!CanReviveAlly(damagedAlly)) return;

            LogMethodEntry($"[StartRevivingAlly] {name} comenzando a revivir a {damagedAlly.name}");
            CambiarEstado(new RevivingState(damagedAlly));
        }

        // =====================================================
        // INTEGRACIÓN CON TakeDamage (Entrada al estado caído)
        // =====================================================

        /// <summary>
        /// Modificado: Cuando HP llega a 0, entra en estado caído en lugar de morir.
        /// </summary>
        private void ModifyTakeDamageForRevival()
        {
            // Este método debe ser llamado desde RecibirDano después de que TakeDamage reduce HP
            if (model.IsDown && !isDown)
            {
                EnterDamagedState();
            }
        }

        // =====================================================
        // LOGGING
        // =====================================================

        private void LogMethodEntry(string message)
        {
            Debug.Log($"<color=blue>[UnitController_Revival]</color> {message}");
        }
    }
}
