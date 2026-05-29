using UnityEngine;
using Game.Squad;

namespace Game.Enemy
{
    /// <summary>
    /// Gestiona los sensores del enemigo para detectar disparos y cercanía de los soldados de la escuadra.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class EnemySensors : MonoBehaviour
    {
        public EnemyController controller;
        public float visualRange = 10f;
        public LayerMask targetMask; // Capa de los soldados (Jugador/Squad)
        public LayerMask obstacleMask; // Obstáculos físicos

        private void Start()
        {
            if (controller == null) controller = GetComponentInParent<EnemyController>();
            if (controller == null) Debug.LogError($"[EnemySensors] ¡Falta EnemyController asociado en '{name}'!");

            // Configurar el trigger local
            CircleCollider2D trigger = GetComponent<CircleCollider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
                if (controller != null && controller.model != null)
                {
                    trigger.radius = controller.model.radioDeteccion;
                }
                else
                {
                    trigger.radius = visualRange;
                }
            }
        }

        private void Update()
        {
            if (controller == null || controller.model == null || controller.model.IsDead) return;

            // Escanear soldados visibles
            EscanearSoldadosEnLineaDeVision();
        }

        private void EscanearSoldadosEnLineaDeVision()
        {
            float range = (controller != null && controller.model != null) ? controller.model.radioDeteccion : visualRange;
            Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (Collider2D target in targets)
            {
                if (target == null || target.gameObject == gameObject) continue;

                bool isTarget = false;
                SoldierController soldier = target.GetComponent<SoldierController>();
                if (soldier != null && soldier.model != null && !soldier.model.IsDead)
                {
                    isTarget = true;
                }
                else if (target.CompareTag("Player") || target.name.Contains("Soldado"))
                {
                    isTarget = true;
                }

                if (isTarget)
                {
                    Vector2 direction = (target.transform.position - transform.position).normalized;
                    float distance = Vector2.Distance(transform.position, target.transform.position);

                    RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleMask);
                    if (hit.collider == null)
                    {
                        controller.AlertarPresenciaSoldado(target.transform);
                        break;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (controller == null) return;

            // 1. Escuchar balas cercanas (2D)
            Bala bala = other.GetComponent<Bala>();
            if (bala != null)
            {
                // Si la bala no pertenece a un enemigo, alertar
                if (bala.dueno != null && !bala.dueno.CompareTag("Enemy") && !bala.dueno.name.Contains("Enemigo"))
                {
                    Debug.Log($"[EnemySensors] {transform.root.name} escuchó un disparo/bala cercana de {bala.dueno.name}.");
                    controller.AlertarRuidoDisparo(bala.transform.position);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (controller == null) return;

            // Escuchar balas cercanas (3D)
            Bala bala = other.GetComponent<Bala>();
            if (bala != null)
            {
                if (bala.dueno != null && !bala.dueno.CompareTag("Enemy") && !bala.dueno.name.Contains("Enemigo"))
                {
                    Debug.Log($"[EnemySensors] {transform.root.name} escuchó un disparo/bala cercana (3D) de {bala.dueno.name}.");
                    controller.AlertarRuidoDisparo(bala.transform.position);
                }
            }
        }
    }
}
