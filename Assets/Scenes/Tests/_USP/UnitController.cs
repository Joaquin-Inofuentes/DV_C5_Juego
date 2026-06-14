using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Game.Sensors;
using Game.Core;

namespace Game.Squad
{
    public partial class UnitController : MonoBehaviour, IDaniable, IDetectable
    {
        [Header("MVC")]
        public UnitModel model;
        public UnitView view;

        [Header("Estado Caído")]
        public GameObject vivoGO;
        public GameObject caidoGO;

        [Header("Referencias")]
        public IA_P2_AgentIA agent;
        public Disparador shooter;
        [Tooltip("Collider de la unidad que se desactivará al morir y se reactivará al ser revivido.")]
        public Collider2D unitCollider;

        // Propiedades necesarias para la FSM
        public Transform currentSlot { get; set; } // Reemplaza slotAsignado
        public Transform target;
        public Vector3 targetPos; // Para �rdenes manuales

        private GenericDetector detector;
        private float nextFireTime;

        [HideInInspector] public bool isWaitingOrder;

        // --- IDetectable ---
        public string GetName() => name;
        public DetectableType GetDetectableType() =>
            model.IsDown ? DetectableType.Invisible :
            model.team == UnitTeam.PlayerTeam ? DetectableType.Aliado : DetectableType.Enemigo;
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

            // Configurar stats y dispersión según especialidad
            ConfigurarEspecialidad();
        }

        private void ConfigurarEspecialidad()
        {
            if (model == null || shooter == null) return;
            switch (model.specialization)
            {
                case UnitSpecialization.Flancotirador:
                    shooter.dañoBala   = 50f;
                    model.fireRate     = 1.2f; // Sniper bolt-action realista (1.2 segundos)
                    shooter.dispersión = 0f;
                    break;
                case UnitSpecialization.Apoyo:
                    shooter.dañoBala   = 5f;
                    model.fireRate     = 0.08f;
                    shooter.dispersión = 30f;
                    break;
                case UnitSpecialization.Medico:
                    shooter.dañoBala   = 5f;
                    model.fireRate     = 0.1f; // Cadencia tipo AK-47 (10 disparos/seg = 0.1 segundos)
                    shooter.dispersión = 6f;
                    break;
                case UnitSpecialization.Asalto:
                    shooter.dañoBala   = 5f;
                    model.fireRate     = 0.1f; // Cadencia tipo AK-47 (10 disparos/seg = 0.1 segundos)
                    shooter.dispersión = 5f;
                    break;
            }
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

        // Leash: si un aliado se aleja demasiado vuelve automáticamente
        private const float LEASH_DISTANCE = 16f;
        private static readonly string[] _leashDialogLines = {
            "¡Vamos en equipo!", "¡Aguardá, jefe!", "¡No me dejés!",
            "¡No te sigo tan lejos!", "¡Esperate, campeón!"
        };

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
            if (victim == this || model.IsLeader || model.IsDead) return;
            // Órdenes pendientes solo bloquean ayuda a aliados (prioridad 2); el líder (1) siempre interrumpe
            if (isWaitingOrder && priority > 1) return;
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
                // Si el objetivo está caído, ignorarlo
                if (other.model.IsDown) return;

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

                // Si es enemigo, reacciona siempre. Si es aliado, reacciona si no es el líder y no tiene orden pendiente
                bool puedeReaccionar = (model.team != UnitTeam.PlayerTeam) || (!model.IsLeader && !isWaitingOrder);

                if (puedeReaccionar && !(_currentStateLogic is AtacarState) && !(_currentStateLogic is PerseguirState))
                {
                    float dist = Vector3.Distance(transform.position, target.position);
                    CambiarEstado(dist <= model.attackRange ? new AtacarState() : new PerseguirState());
                }

                // Group aggro (Dinámico y Divertido)
                float aggroRadius = 8f;
                Collider2D[] vecinos = Physics2D.OverlapCircleAll(transform.position, aggroRadius);
                foreach (var col in vecinos)
                {
                    UnitController vecino = col.GetComponent<UnitController>();
                    if (vecino != null && vecino != this && vecino.model.team == model.team && !vecino.model.IsDead)
                    {
                        if (!vecino.model.IsLeader && !vecino.isWaitingOrder && !(vecino.GetCurrentState() is AtacarState) && !(vecino.GetCurrentState() is PerseguirState))
                        {
                            vecino.target = atacante.transform;
                            float dist = Vector3.Distance(vecino.transform.position, vecino.target.position);
                            vecino.CambiarEstado(dist <= vecino.model.attackRange ? new AtacarState() : new PerseguirState());
                        }
                    }
                }
            }

            if (model.IsDead)
            {
                if (model.team == Game.Core.UnitTeam.PlayerTeam)
                {
                    if (!isDown)
                    {
                        EnterDamagedState();
                        Debug.Log($"[RecibirDano] {name} fue derrotado. Entrando en estado caído");
                        CheckAllPlayerUnitsDown();
                    }
                }
                else
                {
                    Morir();
                }
            }
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

        /// <summary>Recibe alerta de explosión cercana y va a investigar la zona.</summary>
        public void AlertFromExplosion(Vector3 explosionPos)
        {
            if (model.IsDown || model.IsLeader) return;
            if (_currentStateLogic is AtacarState || _currentStateLogic is PerseguirState) return;

            Vector2 rand = UnityEngine.Random.insideUnitCircle * 2.5f;
            MoveToPoint(explosionPos + new Vector3(rand.x, rand.y, 0f));
            CambiarEstado(new IrADestinoState());
        }

        private void Morir()
        {
            agent.StopAgent();
            ReleaseSlot();
            Destroy(gameObject, 0.1f);
        }

        private void CheckAllPlayerUnitsDown()
        {
            foreach (var u in FindObjectsOfType<UnitController>())
                if (u.model.team == UnitTeam.PlayerTeam && !u.IsDown())
                    return;
            SceneManager.LoadScene(0);
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
            if (model.IsDown && !isDown) return; // Prevenir doble entrada a caído

            // Lógica de revivimiento del líder (si está en LiderandoState)
            if (model.IsLeader && _currentStateLogic is LiderandoState)
            {
                UpdateLeaderRevivalInput();
            }

            // Lógica de IA para aliados: detectar y revivir caídos
            if (!model.IsLeader && !model.IsDown && !(_currentStateLogic is RevivingState))
            {
                CheckForDamagedAllies();
            }

            // Leash: solo aliados jugador que se alejan demasiado del líder
            if (!model.IsLeader && !model.IsDown && model.team == UnitTeam.PlayerTeam &&
                GlobalData.liderActual != null && GlobalData.liderActual != this)
            {
                float leashDist = Vector3.Distance(transform.position, GlobalData.liderActual.transform.position);
                if (leashDist > LEASH_DISTANCE &&
                    !(_currentStateLogic is SeguirFormacionState) &&
                    !(_currentStateLogic is RevivingState) &&
                    !(_currentStateLogic is DamagedState))
                {
                    target = null;
                    ResetHelpPriority();
                    string leashMsg = _leashDialogLines[UnityEngine.Random.Range(0, _leashDialogLines.Length)];
                    view.ShowSpeech(leashMsg, 2f);
                    CambiarEstado(new SeguirFormacionState());
                }
            }

            _currentStateLogic?.Update(this);
        }

        private void CheckForDamagedAllies()
        {
            // Buscar aliados caídos cercanos para revivir
            UnitController closestDamaged = FindClosestDamagedAlly();

            if (closestDamaged != null && CanReviveAlly(closestDamaged))
            {
                LogMethodEntry($"[CheckForDamagedAllies] Detecté aliado caído {closestDamaged.name}. Iniciando revivimiento");
                StartRevivingAlly(closestDamaged);
            }
        }

        // En el FixedUpdate de UnitController
        void FixedUpdate()
        {
            if (model.IsDead) return;
            _currentStateLogic?.FixedUpdate(this);
        }

    }
}