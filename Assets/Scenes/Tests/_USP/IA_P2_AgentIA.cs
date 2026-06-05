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

    public void OnDisable()
    {
        isMoving = false;
        currentPath = null;
        currentIndex = 0;
        currentSpeed = 0f;
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

        // Comprobar si hay línea de visión directa
        var hit = Physics2D.Linecast(PosAAnalizar, targetPosition, obstacleLayer);
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

        List<Vector3> RecorridoAStar = IA_P2_PathfindingManager.RequestPath(Origen, targetPosition, Offset);
        currentPath = IA_F_PathFinding_Theta.OptimizarConTheta(RecorridoAStar, obstacleL);

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

        // Rotación en Eje Z — aplica al graphicsRoot si existe, sino al root
        if (!debug_BlockRotation && distance > 0.05f)
        {
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            Transform rotTarget = graphicsRoot != null ? graphicsRoot : transform;
            rotTarget.rotation = Quaternion.Slerp(rotTarget.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (debug_BlockMovement) return;

        // Avance de nodos
        if (distance <= nodeReachDistance)
        {
            currentIndex++;
            if (currentIndex >= currentPath.Count)
            {
                isMoving = false;
                // No hacemos snap de posición para evitar saltos bruscos en 2D
            }
        }
        else
        {
            float step = moveSpeed * Time.deltaTime;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.MovePosition(rb.position + toTarget.normalized * step);
            }
            else
            {
                transform.position += (Vector3)toTarget.normalized * step;
            }
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
        // Si tienes un componente NavMeshAgent, aquí deberías poner navMesh.isStopped = true;
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
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Calculamos ángulo para el plano XY
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRot = Quaternion.Euler(0, 0, angle);
        Transform rotTarget = graphicsRoot != null ? graphicsRoot : transform;
        rotTarget.rotation = Quaternion.Slerp(rotTarget.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }













}