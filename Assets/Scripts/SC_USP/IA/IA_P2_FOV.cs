using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // Necesario para usar Handles (solo en editor, no debe ir en builds)
#endif

public class IA_P2_FOV : MonoBehaviour
{
    [Range(1f, 180f)]
    public float fovAngle = 90f;
    public float viewDistance = 10f;
    public LayerMask visionObstacles;
    public LayerMask targetLayer;

    [Header("Eventos de Detección")]
    [Tooltip("Se dispara UNA VEZ cuando un objetivo entra en visión.")]
    public Action<GameObject> OnTargetDetected;
    [Tooltip("Se dispara UNA VEZ cuando un objetivo sale de la visión.")]
    public Action<GameObject> OnTargetLost;

    [Header("Debug")]
    [Tooltip("Lista pública de enemigos que están DENTRO del BoxCollider (el trigger).")]
    public List<GameObject> enemiesInTrigger = new List<GameObject>();

    [Header("Visualización Debug")]
    public int resolution = 20;
    public Color fovColor = new Color(0, 1, 0, 0.2f);
    public Color detectionColor = Color.red;
    public Color lostTargetColor = Color.white;

    private List<GameObject> _visibleTargets = new List<GameObject>();
    private HashSet<GameObject> _currentlyVisibleTargets = new HashSet<GameObject>();


    // --- LÓGICA DE DETECCIÓN (NÚCLEO) ---

    private void Update()
    {
        _currentlyVisibleTargets.Clear();

        for (int i = enemiesInTrigger.Count - 1; i >= 0; i--)
        {
            GameObject enemy = enemiesInTrigger[i];

            if (enemy == null)
            {
                enemiesInTrigger.RemoveAt(i);
                continue;
            }
            if (enemy != null)
                ProcessTarget(enemy);
        }

        for (int i = _visibleTargets.Count - 1; i >= 0; i--)
        {
            GameObject target = _visibleTargets[i];

            if (target == null)
            {
                _visibleTargets.RemoveAt(i);
                continue;
            }

            if (!_currentlyVisibleTargets.Contains(target))
            {
                _visibleTargets.RemoveAt(i);
                OnTargetLost?.Invoke(target);
                //Debug.Log("Objetivo PERDIDO: " + target.name, target);
            }
        }
    }

    private void ProcessTarget(GameObject target)
    {
        Transform targetTransform = target.transform;

        // 1. Chequeos físicos básicos (Distancia, Ángulo y Muros)
        if (Vector3.Distance(transform.position, targetTransform.position) > viewDistance) return;
        if (!IsInFOV(targetTransform)) return;
        if (!HasLineOfSight(targetTransform)) return;

        // --- LÓGICA DE IDENTIDAD Y EQUIPOS ---

        GameObject miAgente = transform.parent.gameObject;
        if (target == miAgente) return; // Se ignora a sí mismo (sin debug para no colapsar la consola sobre sí mismo)

        string miNombre = miAgente.name;
        string nombreEnemigo = target.name;

        // Detectar facciones
        bool yoSoyA = miNombre.Contains("EQA");
        bool yoSoyB = miNombre.Contains("EQB");
        bool enemigoEsA = nombreEnemigo.Contains("EQA");
        bool enemigoEsB = nombreEnemigo.Contains("EQB");

        // Determinar qué palabra debería tener el enemigo para que yo lo ataque
        string palabraBuscada = yoSoyA ? "EQB" : (yoSoyB ? "EQA" : "DESCONOCIDO");

        // Condición de ataque: Equipos opuestos
        bool sonEnemigos = (yoSoyA && enemigoEsB) || (yoSoyB && enemigoEsA);

        if (sonEnemigos)
        {
            _currentlyVisibleTargets.Add(target);

            if (!_visibleTargets.Contains(target))
            {
                if (IA_P2_LineOfSight3D.Check(transform.parent.position, target.transform.position, visionObstacles))
                {
                    _visibleTargets.Add(target);

                    // DEBUG ATAQUE
                    Debug.Log($"<color=red>ATAQUE:</color> Se vio a <b>{nombreEnemigo}</b>. Como yo soy <b>{miNombre}</b>, ¡iré por él!");

                    OnTargetDetected?.Invoke(target);
                }
            }
        }
        else
        {
            // OMISIÓN: equipo no enemigo. Log comentado para no inundar la consola cada frame.
            //Debug.Log($"Se vio a {nombreEnemigo} con tal nombre. Se omitió por que no contiene {palabraBuscada} en su nombre en su debug. Soy {miNombre}");
        }
    }

    // --- GESTIÓN DE TRIGGERS (MODIFICADA) ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Chequeo de Layer
        if (((1 << other.gameObject.layer) & targetLayer) == 0) return;

        // 2. [CORREGIDO] Obtener el GameObject "Padre" (el que tiene el Rigidbody)
        GameObject enemyRoot = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        // 3. Ignorarse a sí mismo (si el "Padre" del otro es este mismo objeto)
        if (enemyRoot == this.gameObject) return;

        // 4. Añadir a la lista (solo si no está ya)
        if (!enemiesInTrigger.Contains(enemyRoot))
        {
            enemiesInTrigger.Add(enemyRoot);
            //Debug.Log(enemyRoot.name + " entró al trigger.", enemyRoot);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 1. Chequeo de Layer
        if (((1 << other.gameObject.layer) & targetLayer) == 0) return;

        // 2. [CORREGIDO] Obtener el GameObject "Padre"
        GameObject enemyRoot = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        // 3. Quitar de la lista pública
        if (enemiesInTrigger.Remove(enemyRoot))
        {
            //Debug.Log(enemyRoot.name + " salió del trigger.", enemyRoot);

            // 4. Si lo quitamos, comprobar si además estaba en la lista de VISIBLES
            if (_visibleTargets.Remove(enemyRoot))
            {
                OnTargetLost?.Invoke(enemyRoot);
                Debug.Log("Objetivo PERDIDO (Salió del Trigger): " + enemyRoot.name, enemyRoot);
            }
        }
    }

    // --- MÉTODOS DE SOPORTE (Sin cambios) ---

    private bool IsInFOV(Transform target)
    {
        Vector3 dirToTarget = target.position - transform.position;

        // Ignoramos la profundidad Z para el cálculo del ángulo en 2D
        dirToTarget.z = 0;

        if (dirToTarget.sqrMagnitude < 0.001f) return false;

        // En 2D, el "frente" del objeto suele ser su eje X local (transform.right)
        // o el eje que definas como 'frente' al rotar en Z.
        // Usamos transform.right si tu sprite mira hacia la derecha por defecto.
        Vector3 agentForward = transform.right;
        agentForward.z = 0;

        float angle = Vector3.Angle(agentForward.normalized, dirToTarget.normalized);
        return angle <= fovAngle * 0.5f;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 targetPos = target.position + Vector3.up * 0.5f;

        bool sinObstaculos = IA_P2_LineOfSight3D.Check(rayStart, targetPos, visionObstacles);

        if (!sinObstaculos)
        {
            Debug.DrawLine(rayStart, targetPos, Color.yellow);
        }

        return sinObstaculos;
    }


    // --- CONFIGURACIÓN DEL EDITOR (Sin cambios) ---

    private void Reset()
    {
        // El FOV usa OnTriggerEnter2D/OnTriggerExit2D, por lo que necesita un collider 2D.
        var col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        col.size = new Vector2(viewDistance * 2, viewDistance * 2);
    }

    private void OnValidate()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(viewDistance * 2, viewDistance * 2);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!enabled) return;

        Vector3 origin = transform.position;
        // En 2D, el arco debe girar alrededor del eje Z (Vector3.forward)
        // Y empezar desde la dirección derecha del agente
        Vector3 forward = transform.right;

        float halfFOV = fovAngle * 0.5f;

        // Rotamos el vector inicial para que el arco esté centrado
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.forward);
        Vector3 fromDirection = leftRayRotation * forward;

        UnityEditor.Handles.color = fovColor;
        // El normal del arco ahora es Vector3.forward (sale de la pantalla)
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward, fromDirection, fovAngle, viewDistance);

        // Dibujar bordes
        UnityEditor.Handles.color = new Color(fovColor.r, fovColor.g, fovColor.b, 1f);
        Vector3 leftRayDirection = fromDirection;
        Vector3 rightRayDirection = Quaternion.AngleAxis(fovAngle, Vector3.forward) * fromDirection;

        UnityEditor.Handles.DrawLine(origin, origin + leftRayDirection * viewDistance);
        UnityEditor.Handles.DrawLine(origin, origin + rightRayDirection * viewDistance);
    }
#endif
}
