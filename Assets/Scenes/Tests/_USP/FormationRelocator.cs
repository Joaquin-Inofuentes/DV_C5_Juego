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

        // Obtener la máscara de capa de obstáculos
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
                mask = (1 << 6) | (1 << 14);
            }
        }

        for (int i = 0; i < puntosDeFormacion.Count; i++)
        {
            if (puntosDeFormacion[i] == null) continue;

            // Dirección actual del líder al punto de formación
            Vector3 dir = puntosDeFormacion[i].position - posicionLider;
            dir.z = 0f;

            if (dir.sqrMagnitude < 0.01f)
            {
                // Si está encima o muy cerca del líder, asignar una dirección por defecto según el índice
                float angulo = (i % 2 == 0) ? -135f : 135f;
                dir = Quaternion.Euler(0, 0, angulo) * Vector3.down;
            }

            // Forzar la posición a estar exactamente a 'distanciaPreferida'
            Vector3 targetPos = posicionLider + dir.normalized * distanciaPreferida;

            // Evitar que cruce paredes usando un raycast simple
            RaycastHit2D hit = Physics2D.Raycast(posicionLider, dir.normalized, distanciaPreferida, mask);
            if (hit.collider != null)
            {
                targetPos = (Vector3)hit.point - dir.normalized * radioSeguridadSoldado;
            }

            // Mover el punto de formación de forma suave hacia la posición objetivo
            puntosDeFormacion[i].position = Vector3.Lerp(puntosDeFormacion[i].position, targetPos, Time.deltaTime * 10f);
            Debug.DrawLine(posicionLider, puntosDeFormacion[i].position, Color.white);
        }
    }
}