using UnityEngine;
using Game.Squad;

namespace Game.Squad
{
    /// <summary>
    /// Controlador principal del soldado (FSM orientada a Patrón Estado y Control Manual).
    /// Implementa IDaniable para integrarse con colisiones de proyectiles.
    /// </summary>
    public class SoldierController : MonoBehaviour, IDaniable
    {
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
            Interactuando 
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

        // Implementación del Patrón Estado (SOLID)
        private ISoldierState stateLogic;

        private void Start()
        {
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
            if (model == null) model = GetComponent<SoldierModel>();
            if (view == null) view = GetComponent<SoldierView>();
            if (agent == null) agent = GetComponent<IA_P2_AgentIA>();
            if (dispara == null) dispara = GetComponent<Disparador>();

            // Validaciones detalladas de nulls con feedback descriptivo en consola
            if (model == null) Debug.LogError($"[SoldierController] ¡Falta SoldierModel! El objeto '{name}' no tiene estadísticas asignadas.");
            if (view == null) Debug.LogError($"[SoldierController] ¡Falta SoldierView! El objeto '{name}' no tiene representación visual.");
            if (agent == null) Debug.LogError($"[SoldierController] ¡Falta IA_P2_AgentIA! El objeto '{name}' no podrá navegar automáticamente.");
            if (dispara == null) Debug.LogError($"[SoldierController] ¡Falta Disparador! El objeto '{name}' no podrá disparar balas.");
        }

        private void Update()
        {
            if (model == null || model.IsDead) return;

            if (returnCooldown > 0) returnCooldown -= Time.deltaTime;

            // Determinar cambios de estado basados en FSM
            DeterminarTransicionEstado();

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
            if (nuevoEstado == null) return;

            stateLogic?.Exit(this);
            stateLogic = nuevoEstado;
            stateLogic.Enter(this);

            // Sincronizar el Enum actual por compatibilidad con HUD y GUI antiguos
            ActualizarEnumEstado(nuevoEstado);
        }

        private void DeterminarTransicionEstado()
        {
            if (currentState == State.Liderando) return;

            // Transiciones lógicas de estados
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
        }

        // --- Helper Methods para el Estado ---

        public void SetSelectionRing(bool active)
        {
            view?.SetSelectionActive(active);
        }

        public void DispararProyectil()
        {
            if (model != null && model.PuedeDisparar() && dispara != null)
            {
                dispara.Disparar();
                model.GastarBala();
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
            if (model == null || model.IsDead) return;

            model.RecibirDano(cantidad);
            view?.TriggerDamageFeedback();

            // Notificar eventos de daño al EventBus
            SquadEventBus.TriggerSoldierDamaged(this, cantidad);

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
    }
}
