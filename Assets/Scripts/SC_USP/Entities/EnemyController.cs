using USP.Weapons;
using UnityEngine;
using Game.Squad;
using USP.Core;
using USP.Services;

namespace USP.Entities
{
    /// <summary>
    /// Controlador principal del enemigo (MVC - Controller).
    /// FSM distribuida en ticks via TickManager para no quemar el Profiler.
    /// Reacciona a ruidos de disparo cercanos (ShotImpactBus) con verificación de LOS.
    /// </summary>
    [RequireComponent(typeof(EnemyModel))]
    [RequireComponent(typeof(EnemyView))]
    public class EnemyController : MonoBehaviour, global::IDaniable
    {
        public enum EnemyState { Patrullar, Perseguir, Atacar, Investigar }

        [Header("MVC")]
        public EnemyModel model;
        public EnemyView view;

        [Header("Referencias Navegación y Combate")]
        public IA_P2_AgentIA agent;
        public Transform puntoDisparo;
        public GameObject prefabBala;

        [Header("Estado FSM")]
        public EnemyState currentState = EnemyState.Patrullar;
        public Transform objetivoActual;
        public Vector3 investigarPos;

        [Header("Detección de Ruido")]
        [Tooltip("Radio máximo para reaccionar a impactos de disparos cercanos.")]
        public float radioRuidoDisparo = 8f;
        [Tooltip("Máscara de capas que bloquean la visión (LOS). Capa 6 = Obstáculos.")]
        public LayerMask capasLOS = 1 << 6;

        private float nextFireTime;
        private Vector3 spawnPoint;
        private float patrolTimer;
        private Vector3 patrolTarget;
        private Vector3 lastTargetPos;

        // ─── Lifecycle ────────────────────────────────────────────────
        private void Start()
        {
            spawnPoint    = transform.position;
            patrolTarget  = ObtenerNuevaPosicionPatrulla();

            if (model  == null) model  = GetComponent<EnemyModel>();
            if (view   == null) view   = GetComponent<EnemyView>();
            if (agent  == null) agent  = GetComponent<IA_P2_AgentIA>();

            if (model  == null) Debug.LogError($"[EnemyController] '{name}' falta EnemyModel.");
            if (view   == null) Debug.LogError($"[EnemyController] '{name}' falta EnemyView.");
            if (agent  == null) Debug.LogWarning($"[EnemyController] '{name}' falta IA_P2_AgentIA — no podrá moverse.");
        }

        private void OnEnable()
        {
            // Suscribirse a los ticks — reducimos raycasts de cada frame a ticks controlados
            if (TickManager.Instance != null)
            {
                TickManager.Instance.OnTick_0_1s += Tick_Rapido;
                TickManager.Instance.OnTick_0_5s += Tick_Medio;
                TickManager.Instance.OnTick_1s   += Tick_Lento;
            }

            // Escuchar ruidos de disparo para reaccionar con LOS
            ShotImpactBus.OnShotImpact += OnShotNearby;
        }

        private void OnDisable()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.OnTick_0_1s -= Tick_Rapido;
                TickManager.Instance.OnTick_0_5s -= Tick_Medio;
                TickManager.Instance.OnTick_1s   -= Tick_Lento;
            }

            ShotImpactBus.OnShotImpact -= OnShotNearby;
        }

        // ─── Update mínimo: solo disparo y rotación (requiere cada frame) ─────
        private void Update()
        {
            if (model == null || model.IsDead || !model.hasNetworkAuthority) return;

            // La rotación hacia el objetivo y disparo se mantienen en Update
            // porque necesitan precisión frame a frame. El pathfinding va al tick.
            if (currentState == EnemyState.Atacar)
                ManejarAtaque_Update();
        }

        // ─── Ticks ────────────────────────────────────────────────────

        /// Tick 0.1s — ataque: solo confirmar distancia y disparar
        private void Tick_Rapido()
        {
            if (model == null || model.IsDead || !model.hasNetworkAuthority) return;
            if (currentState == EnemyState.Perseguir)
                VerificarEntradaRangoAtaque();
        }

        /// Tick 0.5s — pathfinding de persecución e investigación
        private void Tick_Medio()
        {
            if (model == null || model.IsDead || !model.hasNetworkAuthority) return;

            switch (currentState)
            {
                case EnemyState.Perseguir:  ManejarPersecucion_Tick(); break;
                case EnemyState.Investigar: ManejarInvestigacion_Tick(); break;
            }
        }

        /// Tick 1s — patrulla (la más barata, no necesita frecuencia)
        private void Tick_Lento()
        {
            if (model == null || model.IsDead || !model.hasNetworkAuthority) return;

            if (currentState == EnemyState.Patrullar)
                ManejarPatrulla_Tick();
        }

        // ─── FSM por tick ─────────────────────────────────────────────

        private void ManejarPatrulla_Tick()
        {
            if (agent == null || !agent.enabled) return;

            agent.SetSpeed(model.velocidadPatrulla);

            if (patrolTarget != lastTargetPos)
            {
                agent.GoTo(patrolTarget);
                lastTargetPos = patrolTarget;
            }

            if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            {
                patrolTimer += 1f; // 1 segundo por tick
                if (patrolTimer >= 3f)
                {
                    patrolTarget = ObtenerNuevaPosicionPatrulla();
                    patrolTimer  = 0f;
                }
            }
        }

        private void ManejarPersecucion_Tick()
        {
            if (objetivoActual == null) { RegresarAPatrulla(); return; }

            if (agent != null && agent.enabled)
            {
                agent.SetSpeed(model.velocidadPersecucion);
                if (objetivoActual.position != lastTargetPos)
                {
                    agent.GoTo(objetivoActual.position);
                    lastTargetPos = objetivoActual.position;
                }
            }

            float dist = Vector3.Distance(transform.position, objetivoActual.position);
            if (dist > model.radioDeteccion * 1.5f)
            {
                objetivoActual = null;
                RegresarAPatrulla();
            }
        }

        private void VerificarEntradaRangoAtaque()
        {
            if (objetivoActual == null) return;
            float dist = Vector3.Distance(transform.position, objetivoActual.position);
            if (dist <= 6f)
                currentState = EnemyState.Atacar;
        }

        private void ManejarAtaque_Update()
        {
            if (objetivoActual == null) { RegresarAPatrulla(); return; }

            // Rotar hacia objetivo (precisa cada frame)
            Vector3 dir = (objetivoActual.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (agent != null && agent.enabled) agent.StopAgent();

            float dist = Vector3.Distance(transform.position, objetivoActual.position);
            if (dist > 7.5f)
            {
                currentState = EnemyState.Perseguir;
            }
            else if (Time.time >= nextFireTime)
            {
                DispararAlObjetivo();
                nextFireTime = Time.time + model.fireRate;
            }
        }

        private void ManejarInvestigacion_Tick()
        {
            if (agent == null || !agent.enabled) return;

            agent.SetSpeed(model.velocidadPatrulla);
            if (investigarPos != lastTargetPos)
            {
                agent.GoTo(investigarPos);
                lastTargetPos = investigarPos;
            }

            if (Vector3.Distance(transform.position, investigarPos) < 1f)
            {
                patrolTimer += 0.5f; // 0.5s por tick medio
                if (patrolTimer >= 4f)
                {
                    patrolTimer = 0f;
                    RegresarAPatrulla();
                }
            }
        }

        // ─── Reacción a Ruido de Disparo (ShotImpactBus) ─────────────

        /// <summary>
        /// Llamado por ShotImpactBus cuando cualquier bala impacta.
        /// Filtra por distancia y luego verifica LOS (Physics2D.Linecast).
        /// Solo reacciona si está en Patrullar o Investigar (no interrumpe combate).
        /// </summary>
        private void OnShotNearby(Vector3 impactPos, GameObject owner)
        {
            if (model == null || model.IsDead) return;

            // 1. No interrumpir si ya está en combate activo
            if (currentState == EnemyState.Atacar || currentState == EnemyState.Perseguir) return;

            // 2. Filtro de distancia — si el impacto no está en el radio, ignorar
            float dist = Vector3.Distance(transform.position, impactPos);
            if (dist > radioRuidoDisparo) return;

            // 3. LOS — ¿puede "escuchar"/ver la posición del impacto sin obstáculos?
            //    Usamos Linecast 2D (capa 6 = Obstáculos)
            bool bloqueado = Physics2D.Linecast(transform.position, impactPos, capasLOS);
            if (bloqueado) return;

            // 4. Reaccionar: ir a investigar la posición del ruido
            investigarPos = impactPos;
            currentState  = EnemyState.Investigar;
            patrolTimer   = 0f;
        }

        // ─── Disparo ──────────────────────────────────────────────────

        private void DispararAlObjetivo()
        {
            if (prefabBala == null || puntoDisparo == null) return;

            Debug.DrawLine(puntoDisparo.position, objetivoActual.position, Color.white, 1f);

            GameObject bala = Instantiate(prefabBala, puntoDisparo.position, puntoDisparo.rotation);

            Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();
            if (rbBala != null) rbBala.velocity = puntoDisparo.right * model.velocidadBala;

            Bala b = bala.GetComponent<Bala>();
            if (b != null) { b.damage = model.dano; b.velocidad = model.velocidadBala; b.dueno = gameObject; }

            Proyectil p = bala.GetComponent<Proyectil>();
            if (p != null) { p.dano = model.dano; p.velocidadInicial = model.velocidadBala; p.owner = gameObject; }
        }

        // ─── API pública (para EnemyDetector, etc.) ──────────────────

        /// <summary>Alertar al enemigo de la presencia de un soldado (llamado por EnemyDetector).</summary>
        public void AlertarPresenciaSoldado(Transform soldado)
        {
            if (objetivoActual != null) return; // Ya tiene objetivo
            objetivoActual = soldado;
            currentState   = EnemyState.Perseguir;
        }

        // ─── Helpers ──────────────────────────────────────────────────

        private void RegresarAPatrulla()
        {
            currentState = EnemyState.Patrullar;
            patrolTarget = ObtenerNuevaPosicionPatrulla();
        }

        private Vector3 ObtenerNuevaPosicionPatrulla()
        {
            Vector2 rnd = Random.insideUnitCircle * 5f;
            return spawnPoint + new Vector3(rnd.x, rnd.y, 0f);
        }

        // ─── IDaniable ────────────────────────────────────────────────
        public void RecibirDano(int cantidad, GameObject atacante)
        {
            if (model == null || model.IsDead) return;

            model.RecibirDano(cantidad);
            view?.TriggerDamageFeedback();

            if (atacante != null)
            {
                objetivoActual = atacante.transform;
                currentState   = EnemyState.Perseguir;
            }

            if (model.IsDead) Morir();
        }

        private void Morir()
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.AñadirPuntos(15);
                GameManager.instance.EliminarEnemigo(gameObject);
            }
            Destroy(gameObject);
        }

        internal void AlertarRuidoDisparo(Vector3 position)
        {
            // TODO Falta completar
            Debug.Log($"[EnemyController] '{name}' alertado por ruido de disparo en {position}.");
            throw new System.NotImplementedException();
        }
    }
}
