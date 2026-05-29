using UnityEngine;
using System.Collections;
using Game.Squad;

namespace Game.Enemy
{
    /// <summary>
    /// Controlador principal del enemigo (MVC - Controller).
    /// Coordina las acciones del enemigo utilizando sensores, navegación y comportamiento reactivo con FSM.
    /// Listo para Photon en el futuro (limita la simulación en red).
    /// </summary>
    [RequireComponent(typeof(EnemyModel))]
    [RequireComponent(typeof(EnemyView))]
    public class EnemyController : MonoBehaviour, IDaniable
    {
        public enum EnemyState { Patrullar, Perseguir, Atacar, Investigar }

        [Header("MVC")]
        public EnemyModel model;
        public EnemyView view;

        [Header("Referencias Navegación y Combate")]
        [Tooltip("Agente IA de navegación.")]
        public IA_P2_AgentIA agent;
        [Tooltip("Punto de salida del proyectil del enemigo.")]
        public Transform puntoDisparo;
        [Tooltip("Prefab de la bala.")]
        public GameObject prefabBala;

        [Header("Estado FSM")]
        public EnemyState currentState = EnemyState.Patrullar;
        public Transform objetivoActual;
        public Vector3 investigarPos;

        private float nextFireTime;
        private Vector3 spawnPoint;
        private float patrolTimer;
        private Vector3 patrolTarget;

        private void Start()
        {
            spawnPoint = transform.position;
            patrolTarget = ObtenerNuevaPosicionPatrulla();

            if (model == null) model = GetComponent<EnemyModel>();
            if (view == null) view = GetComponent<EnemyView>();
            if (agent == null) agent = GetComponent<IA_P2_AgentIA>();

            // Validaciones detalladas de referencias con feedback
            if (model == null) Debug.LogError($"[EnemyController] ¡Falta EnemyModel! El objeto '{name}' no tiene estadísticas.");
            if (view == null) Debug.LogError($"[EnemyController] ¡Falta EnemyView! El objeto '{name}' no tiene visualización de vida.");
            if (agent == null) Debug.LogWarning($"[EnemyController] ¡Falta IA_P2_AgentIA! El objeto '{name}' no podrá moverse de forma autónoma.");
        }

        private void Update()
        {
            if (model == null || model.IsDead) return;

            // En el futuro, si no somos el servidor/host en red (hasNetworkAuthority), omitimos el ciclo FSM
            if (!model.hasNetworkAuthority) return;

            ActualizarFSM();

            // Dibujar línea de debug verde del recorrido de patrulla o persecución
            if (agent != null && agent.enabled)
            {
                Color pathColor = (currentState == EnemyState.Perseguir || currentState == EnemyState.Atacar) ? Color.red : Color.green;
                Debug.DrawLine(transform.position, agent.transform.position + Vector3.up * 0.1f, pathColor);
            }
        }

        private void ActualizarFSM()
        {
            switch (currentState)
            {
                case EnemyState.Patrullar:
                    ManejarPatrulla();
                    break;
                case EnemyState.Perseguir:
                    ManejarPersecucion();
                    break;
                case EnemyState.Atacar:
                    ManejarAtaque();
                    break;
                case EnemyState.Investigar:
                    ManejarInvestigacion();
                    break;
            }
        }

        private void ManejarPatrulla()
        {
            if (agent != null && agent.enabled)
            {
                agent.SetSpeed(model.velocidadPatrulla);
                agent.GoTo(patrolTarget);

                if (Vector3.Distance(transform.position, patrolTarget) < 1f)
                {
                    patrolTimer += Time.deltaTime;
                    if (patrolTimer >= 3f)
                    {
                        patrolTarget = ObtenerNuevaPosicionPatrulla();
                        patrolTimer = 0f;
                    }
                }
            }
        }

        private void ManejarPersecucion()
        {
            if (objetivoActual == null)
            {
                RegresarAPatrulla();
                return;
            }

            if (agent != null && agent.enabled)
            {
                agent.SetSpeed(model.velocidadPersecucion);
                agent.GoTo(objetivoActual.position);
            }

            float dist = Vector3.Distance(transform.position, objetivoActual.position);
            if (dist <= 6f) // Distancia de ataque
            {
                currentState = EnemyState.Atacar;
                Debug.Log($"[EnemyController] {name} entró en rango de ataque contra {objetivoActual.name}. Empezando a disparar.");
            }
            else if (dist > model.radioDeteccion * 1.5f)
            {
                Debug.Log($"[EnemyController] {name} perdió de vista a {objetivoActual.name}. Regresando a patrullar.");
                objetivoActual = null;
                RegresarAPatrulla();
            }
        }

        private void ManejarAtaque()
        {
            if (objetivoActual == null)
            {
                RegresarAPatrulla();
                return;
            }

            // Mirar al objetivo
            Vector3 dir = (objetivoActual.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Detener navegación física al disparar
            if (agent != null && agent.enabled) agent.StopAgent();

            float dist = Vector3.Distance(transform.position, objetivoActual.position);
            if (dist > 7.5f) // El objetivo huyó del rango
            {
                currentState = EnemyState.Perseguir;
                Debug.Log($"[EnemyController] {name} persigue de nuevo a {objetivoActual.name} (se alejó de rango de disparo).");
            }
            else
            {
                if (Time.time >= nextFireTime)
                {
                    DispararAlObjetivo();
                    nextFireTime = Time.time + model.fireRate;
                }
            }
        }

        private void ManejarInvestigacion()
        {
            if (agent != null && agent.enabled)
            {
                agent.SetSpeed(model.velocidadPatrulla);
                agent.GoTo(investigarPos);

                if (Vector3.Distance(transform.position, investigarPos) < 1f)
                {
                    patrolTimer += Time.deltaTime;
                    if (patrolTimer >= 4f)
                    {
                        patrolTimer = 0f;
                        RegresarAPatrulla();
                    }
                }
            }
        }

        private void DispararAlObjetivo()
        {
            if (prefabBala == null || puntoDisparo == null) return;

            // Dibujar una línea de debug por 3 segundos de color blanco (Inicio de ataque/Disparo)
            Debug.DrawLine(puntoDisparo.position, objetivoActual.position, Color.white, 3f);
            Debug.Log($"[EnemyController] {name} disparó a {objetivoActual.name}. (Debug DrawLine visible por 3s)");

            GameObject bala = Instantiate(prefabBala, puntoDisparo.position, puntoDisparo.rotation);
            Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();
            if (rbBala != null)
            {
                rbBala.velocity = puntoDisparo.right * model.velocidadBala;
            }

            Bala b = bala.GetComponent<Bala>();
            if (b != null)
            {
                b.damage = model.dano;
                b.velocidad = model.velocidadBala;
                b.dueno = gameObject;
            }

            Proyectil p = bala.GetComponent<Proyectil>();
            if (p != null)
            {
                p.dano = model.dano;
                p.velocidadInicial = model.velocidadBala;
                p.owner = gameObject;
            }
        }

        public void AlertarRuidoDisparo(Vector3 posicionBala)
        {
            if (currentState == EnemyState.Atacar || currentState == EnemyState.Perseguir) return;

            investigarPos = posicionBala;
            currentState = EnemyState.Investigar;
            patrolTimer = 0f;
            Debug.Log($"[EnemyController] {name} detectó un disparo cercano. Investigando posición: {posicionBala}");
        }

        public void AlertarPresenciaSoldado(Transform soldado)
        {
            if (objetivoActual != null) return;

            objetivoActual = soldado;
            currentState = EnemyState.Perseguir;
            Debug.Log($"[EnemyController] {name} detectó cercanía de {soldado.name}. Empezando a seguirlo.");
        }

        private void RegresarAPatrulla()
        {
            currentState = EnemyState.Patrullar;
            patrolTarget = ObtenerNuevaPosicionPatrulla();
        }

        private Vector3 ObtenerNuevaPosicionPatrulla()
        {
            Vector2 randomCircle = Random.insideUnitCircle * 5f;
            return spawnPoint + new Vector3(randomCircle.x, randomCircle.y, 0f);
        }

        // --- IDaniable Implementation ---
        public void RecibirDano(int cantidad, GameObject atacante)
        {
            if (model == null || model.IsDead) return;

            model.RecibirDano(cantidad);
            view?.TriggerDamageFeedback();

            Debug.Log($"[EnemyController] {name} recibió {cantidad} de daño. Vida restante: {model.vidaActual}");

            if (atacante != null)
            {
                objetivoActual = atacante.transform;
                currentState = EnemyState.Perseguir;
            }

            if (model.IsDead)
            {
                Morir();
            }
        }

        private void Morir()
        {
            Debug.Log($"[EnemyController] {name} ha sido eliminado.");
            if (GameManager.instance != null)
            {
                GameManager.instance.AñadirPuntos(15);
                GameManager.instance.EliminarEnemigo(gameObject);
            }
            Destroy(gameObject);
        }
    }
}
