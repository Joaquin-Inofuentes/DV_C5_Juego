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
        public Vector3 targetPos; // Para �rdenes manuales

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

            // Rotaci�n hacia el enemigo
            Vector3 dir = (objetivo.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 10f);

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
            // Si no se est� moviendo, lleg�.
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

        // --- L�GICA DE DETECCI�N Y DA�O ---

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
            UnitController other = entity.GetTransform().GetComponent<UnitController>();
            if (other != null && other.model.team != this.model.team)
            {
                target = other.transform;
                if (!model.IsLeader && !(_currentStateLogic is AtacarState) && !(_currentStateLogic is PerseguirState))
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

            if (atacante != null && target == null)
            {
                target = atacante.transform;
                if (!model.IsLeader)
                {
                    float dist = Vector3.Distance(transform.position, target.position);
                    CambiarEstado(dist <= model.attackRange ? new AtacarState() : new PerseguirState());
                }
            }

            if (model.IsDead) Morir();
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