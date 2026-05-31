using UnityEngine;

namespace USP.Core
{
    /// <summary>
    /// Modelo de datos que almacena las propiedades y estadísticas de salud/combate del enemigo.
    /// Preparado para el futuro para multijugador en red (Photon) con propiedades de sincronización local/remota.
    /// </summary>
    public class EnemyModel : MonoBehaviour, IHealth
    {
        [Header("Estadísticas de Salud")]
        [Tooltip("Vida máxima del enemigo.")]
        public float vidaMaxima = 100f;
        
        [Tooltip("Vida actual del enemigo.")]
        public float vidaActual = 100f;

        [Tooltip("Tasa de autocuración por segundo.")]
        public float healRate = 2f;

        [Tooltip("Tiempo sin recibir daño antes de autocurarse.")]
        public float healDelay = 3f;

        [Header("Estadísticas de Combate")]
        [Tooltip("Daño infligido por disparo.")]
        public float dano = 10f;

        [Tooltip("Cadencia de disparo en segundos.")]
        public float fireRate = 1.5f;

        [Tooltip("Velocidad de las balas disparadas.")]
        public float velocidadBala = 15f;

        [Header("IA y Navegación")]
        [Tooltip("Velocidad al patrullar o investigar.")]
        public float velocidadPatrulla = 3.5f;

        [Tooltip("Velocidad al perseguir activamente al jugador.")]
        public float velocidadPersecucion = 5f;

        [Tooltip("Radio visual para detectar enemigos.")]
        public float radioDeteccion = 10f;

        // Estado Interno (Photon-ready en el futuro)
        public bool IsDead { get; private set; } = false;
        
        [HideInInspector]
        public bool hasNetworkAuthority = true; // Simulado. En el futuro se brinda por Photon

        private float lastDamageTime;

        private void Start()
        {
            vidaActual = vidaMaxima;
        }

        private void Update()
        {
            if (IsDead) return;

            // Regeneración pasiva si es el dueño del estado (Desactivada)
            /*
            if (hasNetworkAuthority && Time.time - lastDamageTime >= healDelay && vidaActual < vidaMaxima)
            {
                vidaActual = Mathf.MoveTowards(vidaActual, vidaMaxima, healRate * Time.deltaTime);
            }
            */
        }

        public void RecibirDano(float cantidad)
        {
            if (IsDead) return;

            vidaActual -= cantidad;
            lastDamageTime = Time.time;

            if (vidaActual <= 0f)
            {
                vidaActual = 0f;
                IsDead = true;
            }
        }

        // IHealth Implementation
        public float CurrentHealth => vidaActual;
        public float MaxHealth => vidaMaxima;
        public void TakeDamage(float amount, GameObject attacker) => RecibirDano(amount);
    }
}
