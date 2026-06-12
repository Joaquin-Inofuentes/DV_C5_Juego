using CustomInspector;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IA_P2_AgentIA : MonoBehaviour
{
    [Button(nameof(GoToGameobject), true)]
    public GameObject targetObject;

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float nodeReachDistance = 0.5f;

    [Header("Rotación Gráfica")]
    [Tooltip("Si se asigna, la rotación se aplica aquí en vez del root.")]
    public Transform graphicsRoot;

    [Header("Debug")]
    public bool debug_BlockMovement = false;
    public bool debug_BlockRotation = false;

    public List<Vector3> currentPath;
    public int currentIndex = 0;
    public bool isMoving = false;
    public float currentSpeed = 0f;

    public float DistanceStop = 1f;

    // Velocidad suavizada para movimiento más natural
    private Vector2 _smoothedVelocity = Vector2.zero;
    [Header("Suavizado de Movimiento")]
    [Tooltip("Qué tan rápido el agente alcanza su velocidad objetivo. Más alto = más brusco.")]
    public float acceleration = 8f;

    private void Awake()
    {
        // Mitad de velocidad global
        moveSpeed *= 0.5f;
    }

    public void OnDisable()
    {
        isMoving = false;
        currentPath = null;
        currentIndex = 0;
        currentSpeed = 0f;
        _smoothedVelocity = Vector2.zero;
    }

    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
    public void AsignarColor(Color color)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;
    }

    public void GoToGameobject(GameObject target)
    {
        GoTo(target.transform.position);
    }

    public int GetStateActual(Vector3 targetPosition)
    {
        var model = IA_P2_PathfindingModel.Instance;
        LayerMask obstacleLayer = 0;

        if (model == null)
        {
            model = FindObjectOfType<IA_P2_PathfindingModel>();
        }

        if (model != null)
        {
            obstacleLayer = model.obstacleLayer;
        }
        else
        {
            obstacleLayer = LayerMask.GetMask("Obstacles", "Obstaculos");
            if (obstacleLayer == 0) obstacleLayer = 1 << 6; // Default a capa 6
        }

        Vector3 PosAAnalizar = transform.position;

        // Determinar radio del agente usando su CircleCollider2D o fallback
        float agentRadius = 0.4f;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            agentRadius = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }

        Vector2 origin = PosAAnalizar;
        Vector2 direction = targetPosition - PosAAnalizar;
        float distance = direction.magnitude;
        if (distance < 0.01f) return 1;
        direction.Normalize();

        // Comprobar si hay línea de visión directa usando CircleCast
        var hit = Physics2D.CircleCast(origin, agentRadius, direction, distance, obstacleLayer);
        if (hit.collider == null)
        {
            return 1; // Visible
        }

        return 0; // Bloqueado, requiere A*
    }

    public void GoTo(Vector3 targetPosition, float Offset = 0)
    {
        // 1. VALIDACIÓN DE REPETICIÓN: Si ya vamos a ese destino, no hacer nada.
        if (isMoving && currentPath != null && currentPath.Count > 0)
        {
            float distAlDestinoFinal = Vector3.Distance(currentPath[currentPath.Count - 1], targetPosition);
            if (distAlDestinoFinal < 0.1f) return;
        }

        int Estado = GetStateActual(targetPosition);
        if (Estado == 1) // Visible directo
        {
            currentPath = new List<Vector3> { targetPosition };
            currentIndex = 0;
            isMoving = true;
            return;
        }

        var model = IA_P2_PathfindingModel.Instance;
        if (model == null)
        {
            model = FindObjectOfType<IA_P2_PathfindingModel>();
        }

        if (model == null)
        {
            // Fallback: Ir directo de todas formas si no hay Pathfinding en escena
            Debug.LogWarning($"[IA_P2_AgentIA - {name}] No se encontró IA_P2_PathfindingModel en escena. Usando movimiento directo hacia {targetPosition}.");
            currentPath = new List<Vector3> { targetPosition };
            currentIndex = 0;
            isMoving = true;
            return;
        }

        Vector3 Origen = transform.position;
        LayerMask obstacleL = model.obstacleLayer;

        float agentRadius = 0.4f;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            agentRadius = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }

        List<Vector3> RecorridoAStar = IA_P2_PathfindingManager.RequestPath(Origen, targetPosition, Offset);
        currentPath = IA_F_PathFinding_Theta.OptimizarConTheta(RecorridoAStar, obstacleL, agentRadius);

        currentIndex = 0;
        isMoving = currentPath != null && currentPath.Count > 0;

        if (!isMoving)
        {
            Debug.LogWarning($"[IA_P2_AgentIA - {name}] No se pudo generar ruta hacia {targetPosition}. Intentando directo.");
            currentPath = new List<Vector3> { targetPosition };
            currentIndex = 0;
            isMoving = true;
        }
    }

    void Update()
    {
        if (!isMoving || currentPath == null || currentPath.Count == 0)
            return;

        Vector3 target = currentPath[currentIndex];
        // Calculamos dirección en 2D (ignorando Z)
        Vector2 toTarget = (Vector2)target - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        // Rotación en Eje Z — aplica al graphicsRoot si existe (instantánea, sin lag)
        if (!debug_BlockRotation && distance > 0.05f && graphicsRoot != null)
        {
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            graphicsRoot.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (debug_BlockMovement) return;

        // Avance de nodos
        if (distance <= nodeReachDistance)
        {
            currentIndex++;
            if (currentIndex >= currentPath.Count)
            {
                isMoving = false;
                _smoothedVelocity = Vector2.zero;
            }
        }
        else
        {
            // Movimiento con lerp de velocidad para suavidad natural
            Vector2 targetVel = toTarget.normalized * moveSpeed;
            _smoothedVelocity = Vector2.Lerp(_smoothedVelocity, targetVel, Time.deltaTime * acceleration);
            currentSpeed = _smoothedVelocity.magnitude;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.MovePosition(rb.position + _smoothedVelocity * Time.deltaTime);
            else
                transform.position += (Vector3)(_smoothedVelocity * Time.deltaTime);
        }

        // Debug visual del camino
        for (int i = Mathf.Max(currentIndex - 1, 0); i < currentPath.Count - 1; i++)
            Debug.DrawLine(currentPath[i], currentPath[i + 1], Color.white, 0.1f);
    }

    public void StopAgent()
    {
        isMoving = false;
        currentPath = null;
        currentIndex = 0;
        _smoothedVelocity = Vector2.zero;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public bool IsMoving()
    {
        return isMoving;
    }


    public bool IsOnFinalPathSegment()
    {
        if (currentPath == null || currentPath.Count == 0)
            return false;

        if (currentPath.Count == 1)
            return true;

        // Estamos en el último tramo cuando el índice apunta al último waypoint (o más allá)
        return currentIndex >= currentPath.Count - 1;
    }

    public void LookAtTarget(Vector3 targetPosition)
    {
        if (graphicsRoot == null) return;

        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Calculamos ángulo para el plano XY
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRot = Quaternion.Euler(0, 0, angle);
        graphicsRoot.rotation = Quaternion.Slerp(graphicsRoot.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }













}