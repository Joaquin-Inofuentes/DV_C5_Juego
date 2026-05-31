using System.Collections;
using UnityEngine;

namespace USP.Core
{
    public class CharacterModel : MonoBehaviour
    {
        [Header("Configuración de Velocidad")]
        [Tooltip("Velocidad normal al caminar usando las teclas WASD.")]
        public float velocidadMovimiento = 5f;

        [Tooltip("Velocidad de giro del personaje.")]
        public float velocidadRotacion = 100f;

        [Header("Física y Salto")]
        [Tooltip("Fuerza aplicada para realizar el salto (impulso físico).")]
        public float desplazamientoSalto = 5f;

        [Header("Referencias de Personaje")]
        [Tooltip("Componente InformacionPersonaje que gestiona la vida, granadas y kits médicos en el GameObject del jugador.")]
        public InformacionPersonaje infoPersonaje;

        [Header("Estado de Adrenalina (Power-Ups)")]
        [Tooltip("Duración en segundos del aumento de velocidad por cada inyección de adrenalina.")]
        public float duracionPorVelocidadAumentada = 5f;

        [Tooltip("Multiplicador de velocidad de movimiento por cada nivel de adrenalina activo (ej. 1.5 = +50% de velocidad).")]
        public float multiplicadorVelocidadPorPowerUp = 1.5f;

        // Variables de estado del runtime (ocultas en el inspector para evitar confusión)
        [HideInInspector] public int powerUpsActivos = 0;
        [HideInInspector] public bool activadoPowerUp1 = false;
        [HideInInspector] public bool activadoPowerUp2 = false;
        [HideInInspector] public float duracionPowerUp1 = 0f;
        [HideInInspector] public float duracionPowerUp2 = 0f;

        // Multiplicador de boost temporal (ej. por pickups)
        private float multiplicadorBoostTemporal = 1f;

        public float ObtenerVelocidadActual()
        {
            float velocidadBase = velocidadMovimiento * multiplicadorBoostTemporal;
            if (activadoPowerUp1) velocidadBase *= multiplicadorVelocidadPorPowerUp;
            if (activadoPowerUp2) velocidadBase *= multiplicadorVelocidadPorPowerUp;
            return velocidadBase;
        }

        /// <summary>
        /// Activa un boost de velocidad temporal (por ejemplo, desde un pickup) por una duración en segundos.
        /// </summary>
        public void ActivarBoostVelocidadTemporal(float multiplicador, float duracion)
        {
            StopAllCoroutines(); // Detener boosts anteriores para evitar acumulación infinita
            StartCoroutine(RutinaBoostVelocidad(multiplicador, duracion));
        }

        private IEnumerator RutinaBoostVelocidad(float multiplicador, float duracion)
        {
            multiplicadorBoostTemporal = multiplicador;
            yield return new WaitForSeconds(duracion);
            multiplicadorBoostTemporal = 1f;
        }

        public bool CanUseKitMedico()
        {
            return infoPersonaje != null && infoPersonaje.kitsMedicos > 0;
        }

        public void UsarKitMedico()
        {
            if (infoPersonaje != null)
            {
                infoPersonaje.UsarKitMedico();
            }
        }

        public void CurarAlMaximo()
        {
            if (infoPersonaje != null)
            {
                infoPersonaje.CurarAlMaximo();
            }
        }

        public bool HasGrenades()
        {
            return infoPersonaje != null && infoPersonaje.granadas > 0;
        }

        public void ConsumeGrenade()
        {
            if (infoPersonaje != null)
            {
                infoPersonaje.granadas--;
                infoPersonaje.ActualizarUI();
            }
        }
    }
}
