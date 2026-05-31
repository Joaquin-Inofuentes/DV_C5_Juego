using UnityEngine;
using Game.Squad;
using USP.Core;
using Game.Sensors;

namespace USP.Entities
{
    /// <summary>
    /// Controlador principal del soldado (FSM orientada a Patrón Estado y Control Manual).
    /// Implementa IDaniable para integrarse con colisiones de proyectiles.
    /// </summary>
    public class SoldierController : MonoBehaviour, global::IDaniable, IDetectable
    {
        // Implementacion de IDetectable
        public string GetName() => name;
        public DetectableType GetDetectableType() => DetectableType.Aliado;
        public Transform GetTransform() => transform;
        // Enlace al estado del FSM anterior por retrocompatibilidad con scripts externos
        public enum State 
        { 
            IrAFormacion, 
            Atacar, 
            IrAAtacar, 
            Investigar, 
            IrAObjetivo, 
            Liderando, 
            Esperando, 
            Interactuando,
            HuirDetrasLider
        }

        [Header("Componentes MVC")]
        public SoldierModel model;
        public SoldierView view;

        [Header("Referencias de Inteligencia / Combate")]
        public IA_P2_AgentIA agent;
        public Disparador dispara;

        [Header("Navegación y FSM")]
        public State currentState = State.Esperando;
        public Transform objetivo;
        public Vector3? investigarPos = null;
        public Transform slotAsignado;

        [Header("Configuración Inteligente")]
        public float distanciaFuego = 6f;
        public float distanciaPersecucion = 15f;

        [Header("Órdenes Manuales")]
        public Vector3 destinoPos;
        public bool tieneOrdenManual = false;
        private IInteractable objetoAInteractuar;

        [Header("Tiempos de Cooldown")]
        public float waitTimer = 0f;
        public float returnCooldown;

        [Header("Feedback Visual de Impacto")]
        [Tooltip("El SpriteRenderer que parpadeará en rojo al recibir daño (asociar manualmente).")]
        public SpriteRenderer feedbackRenderer;
        private float blinkTimer = 0f;

        private GenericDetector detector;
        // Implementación del Patrón Estado (SOLID)
        private ISoldierState stateLogic;

        private void OnEnable()
        {
            SquadEventBus.OnSoldierDamaged += AyudarCompanero;
        }

        private void OnDisable()
        {
            SquadEventBus.OnSoldierDamaged -= AyudarCompanero;
        }

        private void AyudarCompanero(SoldierController herido, float damage, GameObject atacante)
        {
            if (herido == this) return;
            if (model == null || model.IsDead) return;
            if (currentState == State.Liderando) return; // Si es el líder actual no toma decisiones de IA

            if (atacante != null)
            {
                // Si el herido es el liderActual, ayudamos con máxima prioridad
                if (herido == GlobalData.liderActual)
                {
                    objetivo = atacante.transform;
                    investigarPos = null;
                    tieneOrdenManual = false;
                    waitTimer = 0f;
                    if (Vector3.Distance(transform.position, atacante.transform.position) <= distanciaPersecucion)
                    {
                        CambiarEstado(new IrAAtacarState());
                    }
                }
                // Si es otro soldado, ayudamos si no tenemos objetivo actual y está dentro del rango
                else if (objetivo == null)
                {
                    objetivo = atacante.transform;
                    investigarPos = null;
                    tieneOrdenManual = false;
                    waitTimer = 0f;
                    if (Vector3.Distance(transform.position, atacante.transform.position) <= distanciaPersecucion)
                    {
                        CambiarEstado(new IrAAtacarState());
                    }
                }
            }
        }

        private void Start()
        {
            if (CompareTag("Enemy") || name.Contains("Enemy") || name.Contains("Enemigo") || name.StartsWith("E") && char.IsDigit(name, 1))
            {
                Debug.LogError($"[SoldierController - {name}] ¡ADVERTENCIA CRÍTICA! Este componente está en un objeto marcado como ENEMIGO ({name}). Desactivando SoldierController.");
                enabled = false;
                return;
            }

            ValidarReferencias();

            // Configurar estado inicial
            if (currentState == State.Liderando)
            {
                CambiarEstado(new LiderandoState());
            }
            else
            {
                CambiarEstado(new EsperandoState());
            }
        }

        private void ValidarReferencias()
        {
            // Validar referencias cruzadas (si apuntan a otro personaje/root, limpiarlas)
            if (model != null && model.transform.root != transform.root)
            {
                Debug.LogWarning($"[SoldierController - {name}] Referencia cruzada detectada en 'model' ({model.name} pertenece a {model.transform.root.name}). Limpiando...");
                model = null;
            }
            if (view != null && view.transform.root != transform.root)
            {
                Debug.LogWarning($"[SoldierController - {name}] Referencia cruzada detectada en 'view' ({view.name} pertenece a {view.transform.root.name}). Limpiando...");
                view = null;
            }
            if (agent != null && agent.transform.root != transform.root)
            {
                Debug.LogWarning($"[SoldierController - {name}] Referencia cruzada detectada en 'agent' ({agent.name} pertenece a {agent.transform.root.name}). Limpiando...");
                agent = null;
            }
            if (dispara != null && dispara.transform.root != transform.root)
            {
                Debug.LogWarning($"[SoldierController - {name}] Referencia cruzada detectada en 'dispara' ({dispara.name} pertenece a {dispara.transform.root.name}). Limpiando...");
                dispara = null;
            }

            if (model == null) model = GetComponent<SoldierModel>();
            if (view == null) view = GetComponent<SoldierView>();
            if (agent == null) agent = GetComponent<IA_P2_AgentIA>();
            if (dispara == null) dispara = GetComponent<Disparador>();
            if (dispara == null) dispara = GetComponentInChildren<Disparador>();

            if (model == null) Debug.LogError($"[SoldierController] '{name}' falta SoldierModel.");
            if (view == null) Debug.LogError($"[SoldierController] '{name}' falta SoldierView.");
            if (agent == null) Debug.LogError($"[SoldierController] '{name}' falta IA_P2_AgentIA — no podrá navegar.");
            if (dispara == null) Debug.LogError($"[SoldierController] '{name}' falta Disparador — no podrá disparar.");
            if (feedbackRenderer == null) feedbackRenderer = GetComponentInChildren<SpriteRenderer>();
            if (detector == null) detector = GetComponentInChildren<GenericDetector>();
        }

        private void Update()
        {
            // Actualización del parpadeo de daño
            if (blinkTimer > 0f)
            {
                blinkTimer -= Time.deltaTime;
                if (feedbackRenderer != null)
                {
                    bool toggle = Mathf.Repeat(blinkTimer, 0.1f) > 0.05f;
                    feedbackRenderer.color = toggle ? Color.red : Color.white;
                }
            }
            else if (feedbackRenderer != null && feedbackRenderer.color != Color.white)
            {
                feedbackRenderer.color = Color.white;
            }

            if (model == null || model.IsDead) return;

            if (returnCooldown > 0) returnCooldown -= Time.deltaTime;

            // Determinar cambios de estado basados en FSM
            DeterminarTransicionEstado();

            // Buscar objetivos a través del detector genérico
            ActualizarObjetivoDesdeDetector();

            // Ejecutar actualización del estado actual
            stateLogic?.Update(this);
        }

        private void FixedUpdate()
        {
            if (model == null || model.IsDead) return;

            stateLogic?.FixedUpdate(this);
        }

        /// <summary>
        /// Cambia el estado lógico interno utilizando el patrón de diseño Estado.
        /// </summary>
        public void CambiarEstado(ISoldierState nuevoEstado)
        {
            if (nuevoEstado == null)
            {
                Debug.LogError($"[SoldierController - {name}] Se intentó cambiar a un estado lógico NULO.");
                return;
            }

            if (model != null && model.IsDead)
            {
                Debug.LogWarning($"[SoldierController - {name}] Se intentó cambiar de estado a {nuevoEstado.GetType().Name} pero el soldado está MUERTO.");
                return;
            }

            if (stateLogic != null && stateLogic.GetType() == nuevoEstado.GetType())
            {
                // Mismo estado lógico, omitir
                return;
            }

            stateLogic?.Exit(this);
            stateLogic = nuevoEstado;
            stateLogic.Enter(this);

            // Sincronizar el Enum actual por compatibilidad con HUD y GUI antiguos
            ActualizarEnumEstado(nuevoEstado);
        }

        private void DeterminarTransicionEstado()
        {
            if (currentState == State.Liderando) return;

            // Alerta de poca vida: si la vida está al 30% o menos, y hay un líder que no es sí mismo, huir detrás de él.
            if (model != null && (model.vidaActual / model.vidaMaxima) <= 0.3f && GlobalData.liderActual != null && GlobalData.liderActual != this)
            {
                if (!(stateLogic is HuirDetrasLiderState))
                {
                    CambiarEstado(new HuirDetrasLiderState());
                }
                return;
            }

            // Si está en estado de huida pero ya no cumple los requisitos (ej: recuperó vida o no hay líder)
            if (stateLogic is HuirDetrasLiderState)
            {
                CambiarEstado(new IrAFormacionState());
                return;
            }

            // Transiciones lógicas de estados ordinarios
            if (objetivo != null && returnCooldown <= 0)
            {
                float dist = Vector3.Distance(transform.position, objetivo.position);
                if (dist <= distanciaFuego && !(stateLogic is AtacarState))
                {
                    CambiarEstado(new AtacarState());
                }
                else if (dist > distanciaFuego && dist <= distanciaPersecucion && !(stateLogic is IrAAtacarState))
                {
                    CambiarEstado(new IrAAtacarState());
                }
                else if (dist > distanciaPersecucion)
                {
                    objetivo = null;
                    CambiarEstado(new IrAFormacionState());
                }
            }
            else if (investigarPos.HasValue && !(stateLogic is InvestigarState))
            {
                CambiarEstado(new InvestigarState());
            }
            else if (objetoAInteractuar != null && !(stateLogic is InteractuandoState))
            {
                CambiarEstado(new InteractuandoState());
            }
            else if (tieneOrdenManual && !(stateLogic is IrAObjetivoState))
            {
                CambiarEstado(new IrAObjetivoState());
            }
            else if (waitTimer > 0 && !(stateLogic is EsperandoState))
            {
                CambiarEstado(new EsperandoState());
            }
            else if (objetivo == null && !investigarPos.HasValue && objetoAInteractuar == null && !tieneOrdenManual && waitTimer <= 0 && !(stateLogic is IrAFormacionState))
            {
                CambiarEstado(new IrAFormacionState());
            }
        }

        private void ActualizarEnumEstado(ISoldierState nuevoEstado)
        {
            if (nuevoEstado is LiderandoState) currentState = State.Liderando;
            else if (nuevoEstado is AtacarState) currentState = State.Atacar;
            else if (nuevoEstado is IrAAtacarState) currentState = State.IrAAtacar;
            else if (nuevoEstado is InvestigarState) currentState = State.Investigar;
            else if (nuevoEstado is IrAObjetivoState) currentState = State.IrAObjetivo;
            else if (nuevoEstado is IrAFormacionState) currentState = State.IrAFormacion;
            else if (nuevoEstado is EsperandoState) currentState = State.Esperando;
            else if (nuevoEstado is InteractuandoState) currentState = State.Interactuando;
            else if (nuevoEstado is HuirDetrasLiderState) currentState = State.HuirDetrasLider;
        }

        // --- Helper Methods para el Estado ---

        public void SetSelectionRing(bool active)
        {
            view?.SetSelectionActive(active);
        }

        public void DispararProyectil()
        {
            if (model == null)
            {
                Debug.LogError($"[SoldierController - {name}] DispararProyectil abortado: SoldierModel es NULO.");
                return;
            }

            if (dispara == null)
            {
                dispara = GetComponentInChildren<Disparador>();
                if (dispara == null)
                {
                    Debug.LogError($"[SoldierController - {name}] DispararProyectil abortado: Disparador es NULO y no se encontró en hijos.");
                    return;
                }
            }

            if (model.PuedeDisparar())
            {
                dispara.Disparar();
                model.GastarBala();
            }
            else
            {
                Debug.LogWarning($"[SoldierController - {name}] Intento de disparo sin munición (balasActuales: {model.balasActuales}).");
            }
        }

        public void MoverAgenteA(Vector3 pos)
        {
            if (agent != null && agent.enabled)
            {
                agent.GoTo(pos);
            }
        }

        public void StopAgente()
        {
            if (agent != null && agent.enabled)
            {
                agent.StopAgent();
            }
        }

        public IInteractable ObtenerObjetoAInteractuar() => objetoAInteractuar;
        public void LimpiarInteraccion() => objetoAInteractuar = null;

        // --- Órdenes Externas ---

        public void InvestigarPosicion(Vector3 pos)
        {
            if (currentState == State.Liderando || objetivo != null) return;
            investigarPos = pos;
            CambiarEstado(new InvestigarState());
        }

        public void SetInteractionOrder(IInteractable interactuable)
        {
            this.objetoAInteractuar = interactuable;
            this.tieneOrdenManual = false;
            this.investigarPos = null;
            this.objetivo = null;
            CambiarEstado(new InteractuandoState());
        }

        public void SetOrder(Vector3 newPos)
        {
            this.destinoPos = newPos;
            this.tieneOrdenManual = true;
            this.objetoAInteractuar = null;
            this.investigarPos = null;
            this.objetivo = null;
            this.waitTimer = 0;
            CambiarEstado(new IrAObjetivoState());
        }

        public void RegresarAFormacion()
        {
            if (currentState == State.Liderando) return;
            tieneOrdenManual = false;
            objetoAInteractuar = null;
            investigarPos = null;
            objetivo = null;
            returnCooldown = 2.5f;
            CambiarEstado(new IrAFormacionState());
        }

        // --- Interfaz IDaniable ---
        public void RecibirDano(int cantidad, GameObject atacante)
        {
            if (model == null)
            {
                Debug.LogError($"[SoldierController - {name}] RecibirDano falló: model es nulo.");
                return;
            }

            if (model.IsDead)
            {
                Debug.LogWarning($"[SoldierController - {name}] RecibirDano: El soldado ya está muerto.");
                return;
            }

            blinkTimer = 0.3f;
            model.RecibirDano(cantidad);
            view?.TriggerDamageFeedback();

            // Notificar eventos de daño al EventBus
            SquadEventBus.TriggerSoldierDamaged(this, cantidad, atacante);

            if (atacante != null && currentState != State.Liderando)
            {
                objetivo = atacante.transform;
                InvestigarPosicion(atacante.transform.position);
                tieneOrdenManual = false;
                waitTimer = 0;
            }

            if (model.IsDead)
            {
                Morir();
            }
        }

        private void Morir()
        {
            SquadEventBus.TriggerSoldierDied(this);

            if (GlobalData.liderActual == this)
            {
                GlobalData.liderActual = null;
            }

            Destroy(gameObject);
        }

        private void ActualizarObjetivoDesdeDetector()
        {
            if (detector == null || currentState == State.Liderando) return;

            var visibleTargets = detector.GetVisibleTargets();
            Transform mejorObjetivo = null;
            float distanciaCercana = Mathf.Infinity;

            foreach (var target in visibleTargets)
            {
                if (target == null || (target as UnityEngine.Object) == null || target.GetDetectableType() != DetectableType.Enemigo) continue;
                
                float dist = Vector3.Distance(transform.position, target.GetTransform().position);
                if (dist < distanciaCercana)
                {
                    distanciaCercana = dist;
                    mejorObjetivo = target.GetTransform();
                }
            }

            // Si hay un objetivo a la vista que es un enemigo, lo asignamos como objetivo actual
            if (mejorObjetivo != null)
            {
                if (objetivo != mejorObjetivo)
                {
                    Debug.Log($"<color=green>[SoldierController]</color> <b>{name} sigue a {mejorObjetivo.name} por haberlo visto.</b>");
                    objetivo = mejorObjetivo;
                }
            }
            else
            {
                bool teniaObjetivo = (objetivo as object) != null;
                if (teniaObjetivo)
                {
                    string targetName = (objetivo != null) ? objetivo.name : "Destruido";
                    Debug.Log($"<color=red>[SoldierController]</color> <b>{name} dejó de seguir a {targetName} por haberlo perdido de vista.</b>");
                    objetivo = null;
                }
            }
        }
    }
}
