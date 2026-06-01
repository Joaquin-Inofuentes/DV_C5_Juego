using UnityEngine;
using System.Collections.Generic;
using Game.Squad;

namespace Game.Squad
{
    public class UnitFSM : MonoBehaviour
    {
        public enum State { Controlado, Atacando, SeguirLider, IrADestino, Esperando }

        [Header("Configuración de Estado")]
        public State currentState = State.SeguirLider;

        [Header("Referencias")]
        public UnitController controller;

        [Header("Ajustes de IA")]
        [Tooltip("Si es true, buscará enemigos automáticamente cuando esté en formación")]
        public bool autoCombat = true;

        private void Start()
        {
            if (controller == null) controller = GetComponent<UnitController>();

            // Suscribirse al sistema de Ticks para optimizar la IA
            if (TickManager.Instance != null)
            {
                TickManager.Instance.OnTick_0_1s += TickLento;
            }
        }

        private void OnDestroy()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.OnTick_0_1s -= TickLento;
            }
        }

        void Update()
        {
            if (controller.model.IsDead) return;

            // Lógica de movimiento y ejecución (Frame a Frame)
            switch (currentState)
            {
                case State.Controlado:
                case State.SeguirLider:
                    controller.FollowLeader(); // Ya no necesita la lista como parámetro
                    break;

                case State.Atacando:
                    if (controller.target != null)
                    {
                        controller.Attack(controller.target);
                    }
                    else
                    {
                        SetState(State.SeguirLider);
                    }
                    break;

                case State.IrADestino:
                    if (controller.ReachedDestination())
                    {
                        SetState(State.SeguirLider);
                    }
                    break;
            }
        }

        /// <summary>
        /// Lógica de decisión de IA (Se ejecuta 10 veces por segundo, no 60-120)
        /// </summary>
        private void TickLento()
        {
            if (controller.model.IsDead || !autoCombat) return;

            // Si detectamos un enemigo y no tenemos órdenes manuales, atacamos
            if (currentState != State.IrADestino && controller.target != null)
            {
                if (currentState != State.Atacando)
                {
                    SetState(State.Atacando);
                }
            }
        }

        // --- MÉTODOS DE TRANSICIÓN ---

        public void SetState(State newState)
        {
            if (currentState == newState) return;

            // Salida del estado anterior
            ExitState(currentState);

            currentState = newState;

            // Entrada al nuevo estado
            EnterState(newState);

            Debug.Log($"<color=orange>[FSM]</color> <b>{name}</b> pasó a <b>{newState}</b>");
        }

        private void EnterState(State state)
        {
            switch (state)
            {
                case State.IrADestino:
                    controller.ReleaseSlot(); // Al ir a un punto manual, liberamos el slot de formación
                    break;
            }
        }

        private void ExitState(State state)
        {
            // Limpieza si es necesaria
        }

        public void SetEnemy(Transform enemy)
        {
            controller.target = enemy;
            SetState(State.Atacando);
        }

        public void SetDestination(Vector3 pos)
        {
            controller.MoveToPoint(pos);
            SetState(State.IrADestino);
        }

        // --- DEBUG VISUAL ---

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || controller == null) return;

            switch (currentState)
            {
                case State.SeguirLider:
                    Gizmos.color = Color.blue;
                    if (controller.currentSlot) Gizmos.DrawLine(transform.position, controller.currentSlot.position);
                    break;
                case State.Atacando:
                    Gizmos.color = Color.red;
                    if (controller.target) Gizmos.DrawLine(transform.position, controller.target.position);
                    break;
                case State.IrADestino:
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, controller.GetTargetPoint());
                    break;
            }
        }
    }
}