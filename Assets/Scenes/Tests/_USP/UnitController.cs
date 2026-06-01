using UnityEngine;
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
        public Vector3 targetPos; // Para órdenes manuales

        private GenericDetector detector;
        private float nextFireTime;

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

            // Rotación hacia el enemigo
            Vector3 dir = (objetivo.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 10f);

            // Lógica de disparo con cooldown
            if (Time.time >= nextFireTime && model.CanFire())
            {
                shooter.Disparar();
                model.ConsumeAmmo();
                nextFireTime = Time.time + model.fireRate;
            }
        }

        public bool ReachedDestination()
        {
            // Si no se está moviendo, llegó.
            if (!agent.isMoving) return true;

            // Distancia al destino final
            float dist = Vector3.Distance(transform.position, agent.targetObject != null ? agent.targetObject.transform.position : transform.position);
            return dist < 1.0f;
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

        // --- LÓGICA DE DETECCIÓN Y DAÑO ---

        private void OnEnable()
        {
            if (detector) detector.OnTargetDetected += OnTargetDetected;
        }

        private void OnDisable()
        {
            if (detector) detector.OnTargetDetected -= OnTargetDetected;
        }

        private void OnTargetDetected(IDetectable entity)
        {
            // Si el detectado es de otro equipo, marcar como target
            UnitController other = entity.GetTransform().GetComponent<UnitController>();
            if (other != null && other.model.team != this.model.team)
            {
                target = other.transform;
            }
        }

        public void RecibirDano(int cantidad, GameObject atacante)
        {
            if (model.IsDead) return;

            model.TakeDamage(cantidad, atacante);
            view.TriggerFlash();

            // Si me atacan y no tengo target, me defiendo
            if (atacante != null && target == null)
            {
                target = atacante.transform;
            }

            if (model.IsDead) Morir();
        }

        private void Morir()
        {
            agent.StopAgent();
            // Notificar que morí para liberar el slot si es necesario
            ReleaseSlot();
            Destroy(gameObject, 0.1f);
        }




        // Dentro de UnitController.cs
        private IUnitState _currentStateLogic;

        public void CambiarEstado(IUnitState nuevoEstado)
        {
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