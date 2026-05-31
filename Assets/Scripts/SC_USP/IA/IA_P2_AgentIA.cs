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
        gameObject.GetComponent<Renderer>().material.color = color;
    }

    public void GoToGameobject(GameObject target)
    {
        GoTo(target.transform.position);
    }

    public void GoTo(Vector3 targetPosition, float Offset = 0)
    {
        // 1. VALIDACIÓN DE REPETICIÓN: Si ya vamos a ese destino, no hacer nada.
        // Esto evita que el currentIndex se resetee a 0 en cada frame.
        if (isMoving && currentPath != null && currentPath.Count > 0)
        {
            float distAlDestinoFinal = Vector3.Distance(currentPath[currentPath.Count - 1], targetPosition);
            if (distAlDestinoFinal < 0.1f) return; // Ya estamos yendo ahí
        }

        int Estado = GetStateActual(targetPosition);
        if (Estado == 1) // Visible
        {
            currentPath = new List<Vector3> { targetPosition };
            currentIndex = 0;
            isMoving = true;
            return;
        }

        Vector3 Origen = transform.position;
        var model = IA_P2_PathfindingModel.Instance;
        if (model == null) return;

        LayerMask obstacleLayer = model.obstacleLayer;

        // Pedir camino al manager
        List<Vector3> RecorridoAStar = IA_P2_PathfindingManager.RequestPath(Origen, targetPosition, Offset);

        // Optimizar con Theta (asegúrate que el script Theta también use Vector3 sin aplastar Y)
        currentPath = IA_F_PathFinding_Theta.OptimizarConTheta(RecorridoAStar, obstacleLayer);

        currentIndex = 0;
        isMoving = currentPath != null && currentPath.Count > 0;
    }

    public int GetStateActual(Vector3 targetPosition)
    {
        var model = IA_P2_PathfindingModel.Instance;

        if (model == null)
        {
            model = FindObjectOfType<IA_P2_PathfindingModel>();
            if (model == null) return 0;
        }

        Vector3 PosAAnalizar = transform.position;
        LayerMask obstacleLayer = model.obstacleLayer;

        // 1. Si desde mi posición actual veo al objetivo (en el plano YX)
        if (!Physics.Linecast(PosAAnalizar, targetPosition, obstacleLayer))
        {
            return 1; // Camino directo visible
        }

        // 2. Si el último waypoint del camino actual ya ve al nuevo objetivo
        if (currentPath != null && currentPath.Count >= 2)
        {
            Vector3 UltimoWaypoint = currentPath[currentPath.Count - 1];

            if (!Physics.Linecast(UltimoWaypoint, targetPosition, obstacleLayer))
            {
                return 2; // Camino visible desde el último waypoint
            }
        }

        return 0; // El camino no sirve, requiere A* completo
    }

    void Update()
    {
        if (!isMoving || currentPath == null || currentPath.Count == 0)
            return;

        Vector3 target = currentPath[currentIndex];
        // Calculamos dirección en 2D (ignorando Z)
        Vector2 toTarget = (Vector2)target - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        // Rotación en Eje Z
        if (!debug_BlockRotation && distance > 0.05f)
        {
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            // Si tu sprite mira hacia la derecha, usa 'angle'. 
            // Si mira hacia arriba, usa 'angle - 90'.
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
            transform.position += (Vector3)toTarget.normalized * step;
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

        if (currentPath == null || currentPath.Count < 2)
            return false;

        return currentIndex == currentPath.Count;
    }

    public void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Calculamos ángulo para el plano XY
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Aplicamos solo a Z
        Quaternion targetRot = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }













    static List<Vector3> OptimizarConTheta(List<Vector3> RecorridoAStar)
    {
        // Primero revisa si 

        return null;
    }


    /* Usa esto
     

IA_P2_LineOfSight3D. public static bool Check(Vector3 from, Vector3 to,LayerMask obstacleLayer)
    {
        from.y = 0;
        to.y = 0;
        Vector3 dir = to - from;
        float dist = dir.magnitude;

        return !Physics.Raycast(from, dir.normalized, dist, obstacleLayer);
    }


     */

}