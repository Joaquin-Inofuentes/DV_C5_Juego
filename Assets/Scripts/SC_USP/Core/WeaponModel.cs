using System;
using UnityEngine;

namespace USP.Core
{
    public class WeaponModel : MonoBehaviour
    {
        [Header("Nombres de Armas")]
        [Tooltip("Lista con los nombres identificadores de cada arma (ej. Pistola, Metralleta, Escopeta).")]
        public string[] tiposDeArmas = { "Pistola", "Metralleta", "Escopeta" };

        [Header("Estadísticas de Daño y Cadencia")]
        [Tooltip("Daño infligido por impacto para cada arma respectiva.")]
        public float[] danoArmas = { 10f, 4f, 40f };

        [Tooltip("Cadencia de disparo en segundos (tiempo de cooldown entre disparos).")]
        public float[] cadenciaArmas = { 0.2f, 0.08f, 1.0f };

        [Header("Comportamiento de Disparo")]
        [Tooltip("Define si el arma es automática (mantiene presionado click para disparar) o semiautomática (click por click).")]
        public bool[] armasAutomáticas = { false, true, false };

        [Header("Capacidades de Munición (Estilo COD)")]
        [Tooltip("Tamaño máximo del cargador para cada arma.")]
        public int[] cargadorMaximo = { 15, 30, 6 };

        [Tooltip("Balas actualmente cargadas en el cargador activo del arma.")]
        public int[] balasEnCargador;

        [Tooltip("Reserva total de munición disponible fuera del cargador.")]
        public int[] reservaTotal;

        [Header("Estado de Equipamiento")]
        [Tooltip("Índice de la lista del arma actualmente activa (0 = Pistola, 1 = Metralleta, 2 = Escopeta).")]
        public int NumeroDeArmaActual = 0;

        // Variables de runtime internas
        public float TiempoDesdeUltimoDisparo { get; set; }

        public event Action OnWeaponUpdated;
        public event Action OnAmmoUpdated;

        private void Awake()
        {
            // Inicializar munición si los arrays vienen vacíos
            if (balasEnCargador == null || balasEnCargador.Length == 0)
            {
                balasEnCargador = new int[cargadorMaximo.Length];
                for (int i = 0; i < cargadorMaximo.Length; i++)
                {
                    balasEnCargador[i] = cargadorMaximo[i];
                }
            }

            if (reservaTotal == null || reservaTotal.Length == 0)
            {
                reservaTotal = new int[] { 45, 90, 18 };
            }
        }

        public string GetWeaponName() => tiposDeArmas[NumeroDeArmaActual];
        public float GetCurrentWeaponDamage() => danoArmas[NumeroDeArmaActual];
        public float GetCurrentWeaponCadence() => cadenciaArmas[NumeroDeArmaActual];
        public bool IsCurrentWeaponAutomatic() => armasAutomáticas[NumeroDeArmaActual];
        public int GetCurrentWeaponMaxMag() => cargadorMaximo[NumeroDeArmaActual];

        public int BalasActuales
        {
            get => balasEnCargador[NumeroDeArmaActual];
            set
            {
                balasEnCargador[NumeroDeArmaActual] = value;
                OnAmmoUpdated?.Invoke();
            }
        }

        public int ReservaActual
        {
            get => reservaTotal[NumeroDeArmaActual];
            set
            {
                reservaTotal[NumeroDeArmaActual] = value;
                OnAmmoUpdated?.Invoke();
            }
        }

        public void SelectWeapon(int index)
        {
            if (index >= 0 && index < tiposDeArmas.Length && index != NumeroDeArmaActual)
            {
                NumeroDeArmaActual = index;
                OnWeaponUpdated?.Invoke();
                OnAmmoUpdated?.Invoke();
            }
        }

        public bool CanFire()
        {
            return TiempoDesdeUltimoDisparo >= GetCurrentWeaponCadence() && BalasActuales > 0;
        }

        public void ConsumeBullet()
        {
            BalasActuales--;
            TiempoDesdeUltimoDisparo = 0f;
        }

        public bool TryReload()
        {
            int current = NumeroDeArmaActual;
            if (balasEnCargador[current] == cargadorMaximo[current] || reservaTotal[current] <= 0)
                return false;

            int needed = cargadorMaximo[current] - balasEnCargador[current];
            int toReload = Mathf.Min(needed, reservaTotal[current]);

            balasEnCargador[current] += toReload;
            reservaTotal[current] -= toReload;

            OnAmmoUpdated?.Invoke();
            return true;
        }
    }
}
