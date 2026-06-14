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

            // Ángulo fijo para cada slot para que nunca se peleen por la misma posición:
            // Slot 0: atrás-izquierda (-135 grados)
            // Slot 1: atrás-derecha (135 grados)
            // Otros slots: distribuir en otros ángulos fijos
            float angulo = -135f;
            if (i == 1) angulo = 135f;
            else if (i > 1) angulo = (i % 2 == 0) ? -90f - (i * 15f) : 90f + (i * 15f);

            Vector3 dir = Quaternion.Euler(0, 0, angulo) * Vector3.down;

            // Forzar la posición a estar a 'distanciaPreferida'
            Vector3 targetPos = posicionLider + dir * distanciaPreferida;

            // Evitar que cruce paredes usando un raycast simple
            RaycastHit2D hit = Physics2D.Raycast(posicionLider, dir, distanciaPreferida, mask);
            if (hit.collider != null)
            {
                targetPos = (Vector3)hit.point - dir * radioSeguridadSoldado;
            }

            // Asignar o interpolar
            puntosDeFormacion[i].position = Vector3.Lerp(puntosDeFormacion[i].position, targetPos, Time.deltaTime * 12f);
            Debug.DrawLine(posicionLider, puntosDeFormacion[i].position, Color.white);
        }
    }
}