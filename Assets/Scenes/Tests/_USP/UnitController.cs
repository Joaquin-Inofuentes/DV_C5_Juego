using UnityEngine;
using System.Collections;
using Game.Sensors;
using Game.Core;

namespace Game.Squad
{
    public class UnitController : MonoBehaviour, IDaniable, IDetectable
    {
        [Header("MVC")]
        public UnitModel model;
        public UnitView view;

        [Header("Referencias")]
        public IA_P2_AgentIA agent;
        public Disparador shooter;

        // Propiedades necesarias para la FSM
        public Transform currentSlot { get; set; } // Reemplaza slotAsignado
        public Transform target;
        public Vector3 targetPos; // Para �rdenes manuales

        private GenericDetector detector;
        private float nextFireTime;

        [HideInInspector] public bool isWaitingOrder;

        // --- IDetectable ---
        public string GetName() => name;
        public DetectableType GetDetectableType() => model.team == UnitTeam.PlayerTeam ? DetectableType.Aliado : DetectableType.Enemigo;
        public Transform GetTransform() => transform;

        void Awake()
        {
            if (!model) model = GetComponent<UnitModel>();
            if (!view) view = GetComponent<UnitView>();
            if (!agent) agent = GetComponent<IA_P2_AgentIA>();
            if (!shooter) shooter = GetComponentInChildren<Disparador>();
            detector = GetComponentInChildren<GenericDetector>();

            if (!model)   Debug.LogError($"[UnitController] {name}: Falta componente UnitModel.");
            if (!view)    Debug.LogError($"[UnitController] {name}: Falta componente UnitView.");
            if (!agent)   Debug.LogError($"[UnitController] {name}: Falta componente IA_P2_AgentIA.");
            if (!shooter) Debug.LogError($"[UnitController] {name}: Falta componente Disparador (hijo).");
            if (!detector) Debug.LogWarning($"[UnitController] {name}: Falta GenericDetector (hijo). No detectará enemigos.");
        }

        void Start()
        {
            if (_currentStateLogic == null)
                CambiarEstado(new EsperandoState());
        }

        // --- FUNCIONES QUE PIDE TU FSM (ERRORES CS1061) ---

        public void FollowLeader()
        {
            if (currentSlot != null)
            {
                agent.GoTo(currentSlot.position);
            }
        }

        public Transform GetEnemy()
        {
            return target;
        }

        public void Attack(Transform objetivo)
        {
            if (objetivo == null) return;

            target = objetivo;
            agent.StopAgent();

            // Rotación gráfica hacia el enemigo (el root NO rota)
            Vector3 dir = (objetivo.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            view.RotateGraphicsSmooth(angle, 10f);

            // L�gica de disparo con cooldown
            if (Time.time >= nextFireTime && model.CanFire())
            {
                shooter.Disparar();
                model.ConsumeAmmo();
                nextFireTime = Time.time + model.fireRate;
            }
        }

        public bool ReachedDestination()
        {
            if (agent.currentPath != null && agent.currentPath.Count > 0)
            {
                float dist = Vector3.Distance(transform.position, agent.currentPath[agent.currentPath.Count - 1]);
                return dist < 1.0f;
            }
            return !agent.isMoving;
        }

        public void ReleaseSlot()
        {
            currentSlot = null;
        }

        public void MoveToPoint(Vector3 point)
        {
            targetPos = point;
            agent.GoTo(point);
        }

        public Vector3 GetTargetPoint()
        {
            return targetPos;
        }

        // --- L�GICA DE DETECCI�N Y DA�O ---

        /// Prioridad del objetivo actual de ayuda (0 = ninguno, 1 = líder, 2 = aliado)
        private int _currentHelpPriority = 0;

        /// <summary>Resetea la prioridad de ayuda cuando se vuelve a formación.</summary>
        public void ResetHelpPriority() => _currentHelpPriority = 0;

        private void OnEnable()
        {
            if (detector) detector.OnTargetDetected += OnTargetDetected;
            SquadEventBus.OnHelpRequested += OnHelpRequested;
        }

        private void OnDisable()
        {
            if (detector) detector.OnTargetDetected -= OnTargetDetected;
            SquadEventBus.OnHelpRequested -= OnHelpRequested;
        }

        /// <summary>
        /// Responde a pedidos de ayuda del escuadrón.
        /// Prioridad 1 (líder) siempre reemplaza prioridad 2 (aliado).
        /// No se auto-ayuda ni ayuda si está liderando.
        /// </summary>
        private void OnHelpRequested(UnitController victim, Transform attacker, int priority)
        {
            if (victim == this || model.IsLeader || model.IsDead || isWaitingOrder) return;
            if (attacker == null) return;
            // Solo aliados del mismo equipo ayudan
            if (model.team != Game.Core.UnitTeam.PlayerTeam) return;

            // Sistema de prioridad: número menor = más urgente (1=líder, 2=aliado)
            bool yaEnCombate = _currentStateLogic is AtacarState || _currentStateLogic is PerseguirState;

            // Si ya estoy en combate con algo de igual o mayor prioridad (número menor), ignorar
            if (yaEnCombate && _currentHelpPriority > 0 && priority >= _currentHelpPriority && target != null && target != attacker)
                return;

            target = attacker;
            _currentHelpPriority = priority;

            float dist = Vector3.Distance(transform.position, attacker.position);
            CambiarEstado(dist <= model.attackRange ? new AtacarState() : new PerseguirState());
        }

        private void OnTargetDetected(IDetectable entity)
        {
            UnitController other = entity.GetTransform().GetComponent<UnitController>();
            if (other != null && other.model.team != this.model.team)
            {
                target = other.transform;
                if (!model.IsLeader && !isWaitingOrder && !(_currentStateLogic is AtacarState) && !(_currentStateLogic is PerseguirState))
                {
                    float dist = Vector3.Distance(transform.position, target.position);
                    CambiarEstado(dist <= model.attackRange ? new AtacarState() : new PerseguirState());
                }
            }
        }

        public void RecibirDano(int cantidad, GameObject atacante)
        {
            if (model.IsDead) return;

            model.TakeDamage(cantidad, atacante);
            view.TriggerFlash();

            Debug.Log($"<color=red>[Daño]</color> {name} recibió {cantidad} de {(atacante != null ? atacante.name : "desconocido")}. HP: {model.healthActual}/{model.healthMax}");

            if (atacante != null)
            {
                target = atacante.transform;

                // Emitir pedido de ayuda al escuadrón via EventBus
                // Prioridad 1 = líder (yo, el jugador), Prioridad 2 = aliado
                if (model.team == Game.Core.UnitTeam.PlayerTeam)
                {
                    int prioridad = model.IsLeader ? 1 : 2;
                    SquadEventBus.TriggerHelpRequested(this, atacante.transform, prioridad);
                }

                if (!model.IsLeader && !isWaitingOrder && !(_currentStateLogic is AtacarState) && !(_currentStateLogic is PerseguirState))
                {
                    float dist = Vector3.Distance(transform.position, target.position);
                    CambiarEstado(dist <= model.attackRange ? new AtacarState() : new PerseguirState());
                }
            }

            if (model.IsDead) Morir();
        }

        public void OnHealPickup()
        {
            view.StartBlink(IndicatorType.Heal);
            StartCoroutine(StopHealBlinkAfterDelay(1.5f));
        }

        private System.Collections.IEnumerator StopHealBlinkAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            view.StopBlink(IndicatorType.Heal);
        }

        private void Morir()
        {
            agent.StopAgent();
            // Notificar que mor� para liberar el slot si es necesario
            ReleaseSlot();
            Destroy(gameObject, 0.1f);
        }




        // Dentro de UnitController.cs
        private IUnitState _currentStateLogic;

        public IUnitState GetCurrentState() => _currentStateLogic;

        public void CambiarEstado(IUnitState nuevoEstado)
        {
            string anterior = _currentStateLogic != null ? _currentStateLogic.GetType().Name : "null";
            string nuevo = nuevoEstado != null ? nuevoEstado.GetType().Name : "null";
            Debug.Log($"<color=lime>[FSM]</color> {name}: {anterior} → {nuevo}");

            _currentStateLogic?.Exit(this);
            _currentStateLogic = nuevoEstado;
            _currentStateLogic?.Enter(this);
        }

        // En el Update de UnitController
        void Update()
        {
            if (model.IsDead) return;
            _currentStateLogic?.Update(this);
        }

        // En el FixedUpdate de UnitController
        void FixedUpdate()
        {
            if (model.IsDead) return;
            _currentStateLogic?.FixedUpdate(this);
        }
    }
}