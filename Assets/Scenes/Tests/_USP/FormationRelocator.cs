using UnityEngine;
using System.Collections.Generic;

public class FormationRelocator : MonoBehaviour
{
    [Header("CONFIGURACION")]
    public List<Transform> puntosDeFormacion;
    public LayerMask obstacleLayer;
    public float radioSeguridadSoldado = 0.5f;

    [Header("DISTANCIAS")]
    public float distanciaPreferida = 3.5f;
    public float distanciaMinima = 1.5f;

    // Dirección de movimiento del líder (suavizada) — los aliados se colocan detrás
    private Vector3 _leaderMoveDir = new Vector3(0f, -1f, 0f); // sur por defecto
    private Vector3 _leaderPrevPos;

    void Start()
    {
        if (GlobalData.liderActual != null)
            _leaderPrevPos = GlobalData.liderActual.transform.position;
    }

    void Update()
    {
        if (GlobalData.liderActual == null) return;

        Vector3 posicionLider = GlobalData.liderActual.transform.position;

        // Actualizar dirección de movimiento del líder (suavizado)
        Vector3 delta = posicionLider - _leaderPrevPos;
        if (delta.sqrMagnitude > 0.0004f)
            _leaderMoveDir = Vector3.Lerp(_leaderMoveDir, delta.normalized, Time.deltaTime * 4f);
        _leaderPrevPos = posicionLider;

        // Los aliados se despliegan DETRÁS del líder, alternando lados
        Vector3 behindDir = -_leaderMoveDir.normalized;
        Vector3 perpDir = new Vector3(-behindDir.y, behindDir.x, 0f).normalized;

        List<Vector3> posicionesCalculadas = new List<Vector3>();

        for (int i = 0; i < puntosDeFormacion.Count; i++)
        {
            if (puntosDeFormacion[i] == null) continue;

            // Alternar izq/der + profundidad escalonada
            float lado = (i % 2 == 0) ? 1f : -1f;
            float profundidad = 1f + (i / 2) * 0.4f;
            Vector3 posicionIdeal = posicionLider
                + behindDir * (distanciaPreferida * profundidad)
                + perpDir * (lado * distanciaPreferida * 0.65f);

            Vector3 posicionFinal = CalcularPosicionValida(posicionLider, posicionIdeal, behindDir, posicionesCalculadas);
            posicionesCalculadas.Add(posicionFinal);

            puntosDeFormacion[i].position = Vector3.Lerp(puntosDeFormacion[i].position, posicionFinal, Time.deltaTime * 8f);
            Debug.DrawLine(posicionLider, puntosDeFormacion[i].position, Color.white);
        }
    }

    Vector3 CalcularPosicionValida(Vector3 origenLider, Vector3 destinoIdeal, Vector3 direccionOriginal, List<Vector3> posicionesOcupadas)
    {
        // 1. Obtener la máscara de capa de obstáculos
        LayerMask mask = obstacleLayer;
        if (mask == 0)
        {
            var pathfindingModel = FindFirstObjectByType<IA_P2_PathfindingModel>();
            if (pathfindingModel != null)
            {
                mask = pathfindingModel.obstacleLayer;
            }
            else
            {
                mask = LayerMask.GetMask("Obstacles", "Obstaculos");
                if (mask == 0) mask = (1 << 6) | (1 << 14); // Fallback a capa 6 (antigua) y 14 (Obstacles)
            }
        }

        Vector3 dir = destinoIdeal - origenLider;
        float dist = dir.magnitude;
        Vector3 posFinal = destinoIdeal;

        // 2. Si el destino ideal está detrás de una pared, acercarlo suavemente
        if (dist > 0.01f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(origenLider, radioSeguridadSoldado, dir.normalized, dist, mask);
            if (hit.collider != null)
            {
                // Obstruido: Acercamos la posición justo antes del obstáculo para evitar saltos bruscos
                float safeDist = Mathf.Max(0f, hit.distance - 0.15f);
                posFinal = origenLider + dir.normalized * safeDist;
            }
        }

        // 3. Pequeña fuerza de repulsión entre aliados para no encimarse si se agrupan en una pared
        Vector3 repulsion = Vector3.zero;
        foreach (var pos in posicionesOcupadas)
        {
            float d = Vector3.Distance(posFinal, pos);
            if (d < 1.0f && d > 0.001f) // Si están muy cerca
            {
                Vector3 away = (posFinal - pos).normalized;
                repulsion += away * (1.0f - d) * 0.5f; // Fuerza suave de repulsión
            }
        }

        posFinal += repulsion;

        return posFinal;
    }
}