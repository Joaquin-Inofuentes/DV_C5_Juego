using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Sensors
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class GenericDetector : MonoBehaviour
    {
        [Header("Configuracion de Deteccion")]
        
        public LayerMask obstacleMask;
        public List<DetectableType> typesToDetect = new List<DetectableType>();

        [Header("Estado")]
        [SerializeField] private List<MonoBehaviour> targetsInRangeSerializables = new List<MonoBehaviour>(); // Para debug visual en inspector
        
        private List<IDetectable> targetsInRange = new List<IDetectable>();
        private List<IDetectable> visibleTargets = new List<IDetectable>();
        private List<IDetectable> previouslyVisibleTargets = new List<IDetectable>();

        private CircleCollider2D trigger;
        private float scanInterval = 0.15f;
        private float nextScanTime;

        public event Action<IDetectable> OnTargetDetected;
        public event Action<IDetectable> OnTargetLost;

        public List<IDetectable> GetVisibleTargets() => visibleTargets;
        public List<IDetectable> GetTargetsInRange() => targetsInRange;


        private void Update()
        {
            if (Time.time >= nextScanTime)
            {
                EscanearLíneaDeVisión();
                nextScanTime = Time.time + scanInterval;
            }
        }

        private void EscanearLíneaDeVisión()
        {
            // 1. Limpiar nulos (por destrucción de objetos)
            int removed = targetsInRange.RemoveAll(t => t == null || (t as UnityEngine.Object) == null || t.GetTransform() == null || !t.GetTransform().gameObject.activeInHierarchy);
            if (removed > 0)
            {
                Debug.Log($"<color=orange>[GenericDetector]</color> {transform.root.name} limpio {removed} objetivos nulos o desactivados.");
                SincronizarInspector();
            }

            // 2. Remover targets que se volvieron invisibles (e.g. unidades caídas)
            var nowInvisible = targetsInRange.FindAll(t => t.GetDetectableType() == DetectableType.Invisible);
            foreach (var t in nowInvisible)
            {
                targetsInRange.Remove(t);
                if (previouslyVisibleTargets.Contains(t))
                {
                    Debug.Log($"<color=red>[GenericDetector]</color> {transform.root.name} <b>DEJÓ DE VER</b> a {t.GetName()} (caído/invisible)");
                    OnTargetLost?.Invoke(t);
                }
            }
            if (nowInvisible.Count > 0) SincronizarInspector();

            visibleTargets.Clear();

            // 3. Comprobar obstrucciones visuales con Raycast
            foreach (var target in targetsInRange)
            {
                if (target.GetDetectableType() == DetectableType.Invisible) continue;

                Transform targetTransform = target.GetTransform();
                Vector2 direction = (targetTransform.position - transform.position).normalized;
                float distance = Vector2.Distance(transform.position, targetTransform.position);

                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleMask);
                if (hit.collider == null)
                {
                    visibleTargets.Add(target);
                    // Dibujar línea a todos los visibles
                    Debug.DrawLine(transform.position, targetTransform.position, Color.cyan, scanInterval);
                }
            }

            // 4. Evaluar cambios de visibilidad para disparar eventos e imprimir debugs
            foreach (var target in visibleTargets)
            {
                if (!previouslyVisibleTargets.Contains(target))
                {
                    Debug.Log($"<color=green>[GenericDetector]</color> {transform.root.name} <b>VIO</b> a {target.GetName()} ({target.GetDetectableType()})");
                    OnTargetDetected?.Invoke(target);
                }
            }

            foreach (var target in previouslyVisibleTargets)
            {
                if (target != null && (target as UnityEngine.Object) != null && !visibleTargets.Contains(target))
                {
                    Debug.Log($"<color=red>[GenericDetector]</color> {transform.root.name} <b>DEJÓ DE VER</b> a {target.GetName()} ({target.GetDetectableType()})");
                    OnTargetLost?.Invoke(target);
                }
            }

            previouslyVisibleTargets = new List<IDetectable>(visibleTargets);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Validar si tiene el contrato IDetectable
            IDetectable detectable = other.GetComponent<IDetectable>();
            if (detectable != null)
            {
                if (detectable.GetDetectableType() != DetectableType.Invisible &&
                    typesToDetect.Contains(detectable.GetDetectableType()))
                {
                    if (!targetsInRange.Contains(detectable))
                    {
                        targetsInRange.Add(detectable);
                        Debug.Log($"<color=yellow>[GenericDetector]</color> {transform.root.name} <b>DETECTÓ EN RADIO</b> a {detectable.GetName()} ({detectable.GetDetectableType()})");
                        SincronizarInspector();
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            IDetectable detectable = other.GetComponent<IDetectable>();
            if (detectable != null)
            {
                if (targetsInRange.Contains(detectable))
                {
                    targetsInRange.Remove(detectable);
                    Debug.Log($"<color=yellow>[GenericDetector]</color> {transform.root.name} <b>PERDIÓ DE SU RADIO</b> a {detectable.GetName()} ({detectable.GetDetectableType()})");
                    SincronizarInspector();
                }
            }
        }

        private void SincronizarInspector()
        {
            targetsInRangeSerializables.Clear();
            foreach (var t in targetsInRange)
            {
                if (t is MonoBehaviour mb)
                {
                    targetsInRangeSerializables.Add(mb);
                }
            }
        }
    }
}
