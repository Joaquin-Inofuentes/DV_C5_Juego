using UnityEngine;
using System;

namespace Game.Squad
{
    /// <summary>
    /// Modelo de datos que almacena las propiedades y estadísticas individuales de cada soldado.
    /// </summary>
    public class SoldierModel : MonoBehaviour
    {
        [Header("Información del Soldado")]
        [Tooltip("Nombre identificador del soldado.")]
        public string nombreSoldado = "Soldado";

        [Header("Estadísticas de Salud")]
        [Tooltip("Vida máxima del soldado.")]
        public float vidaMaxima = 100f;
        
        [Tooltip("Vida actual del soldado.")]
        public float vidaActual = 100f;

        [Tooltip("Tasa de autocuración por segundo.")]
        public float healRate = 5f;

        [Tooltip("Tiempo en segundos sin recibir daño requerido antes de empezar a autocurarse.")]
        public float healDelay = 2f;

        [Header("Estadísticas de Combate")]
        [Tooltip("Daño infligido por disparo.")]
        public float dano = 15f;

        [Tooltip("Cadencia de disparo en segundos (tiempo de cooldown).")]
        public float fireRate = 0.5f;

        [Tooltip("Capacidad actual de munición.")]
        public int balasActuales = 300;

        [Tooltip("Capacidad máxima de munición.")]
        public int maxBalas = 300;

        [Header("Movilidad")]
        [Tooltip("Velocidad de movimiento del soldado.")]
        public float velocidad = 5f;

        // Propiedades de estado interno
        public bool IsLeader { get; set; } = false;
        public bool IsDead { get; private set; } = false;

        private float lastDamageTime;

        private void Start()
        {
            vidaActual = vidaMaxima;
            balasActuales = maxBalas;
        }

        private void Update()
        {
            if (IsDead) return;

            // Regeneración pasiva de salud
            if (Time.time - lastDamageTime >= healDelay && vidaActual < vidaMaxima)
            {
                vidaActual = Mathf.MoveTowards(vidaActual, vidaMaxima, healRate * Time.deltaTime);
            }
        }

        /// <summary>
        /// Aplica daño al soldado.
        /// </summary>
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

        public void Curar(float cantidad)
        {
            if (IsDead) return;
            vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
        }

        public bool PuedeDisparar()
        {
            return balasActuales > 0;
        }

        public void GastarBala()
        {
            if (balasActuales > 0) balasActuales--;
        }
    }
}
