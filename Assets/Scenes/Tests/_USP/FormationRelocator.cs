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

    private Vector3 _formationForward = Vector3.down;

    void Update()
    {
        if (GlobalData.liderActual == null) return;

        Vector3 posicionLider = GlobalData.liderActual.transform.position;

        // Determinar dirección deseada del movimiento/vista del líder
        Vector3 targetForward = _formationForward;
        Vector3 moveInput = Vector3.zero;

        if (GEN_Inputs.Instance != null)
        {
            moveInput = new Vector3(GEN_Inputs.Instance.MovimientoInput.x, GEN_Inputs.Instance.MovimientoInput.y, 0f);
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            targetForward = moveInput.normalized;
        }
        else if (GEN_Inputs.Instance != null)
        {
            Vector3 mouseDir = (GEN_Inputs.Instance.MouseWorldPosition - posicionLider).normalized;
            mouseDir.z = 0f;
            if (mouseDir.sqrMagnitude > 0.01f)
            {
                targetForward = mouseDir.normalized;
            }
        }

        // Suavizar la dirección hacia adelante para evitar giros instantáneos o erráticos
        _formationForward = Vector3.Slerp(_formationForward, targetForward, Time.deltaTime * 6f).normalized;

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

            // Ángulo fijo relativo al frente de formación para que nunca se peleen por la misma posición:
            // Slot 0: atrás-izquierda (-135 grados)
            // Slot 1: atrás-derecha (135 grados)
            float angulo = -135f;
            if (i == 1) angulo = 135f;
            else if (i > 1) angulo = (i % 2 == 0) ? -90f - (i * 15f) : 90f + (i * 15f);

            Vector3 dir = Quaternion.Euler(0, 0, angulo) * _formationForward;

            // Forzar la posición a estar a 'distanciaPreferida'
            Vector3 targetPos = posicionLider + dir * distanciaPreferida;

            // Evitar que cruce paredes usando un raycast simple
            RaycastHit2D hit = Physics2D.Raycast(posicionLider, dir, distanciaPreferida, mask);
            if (hit.collider != null)
            {
                targetPos = (Vector3)hit.point - dir * radioSeguridadSoldado;
            }

            // Asignar o interpolar el punto de formación
            puntosDeFormacion[i].position = Vector3.Lerp(puntosDeFormacion[i].position, targetPos, Time.deltaTime * 12f);
            
            // Dibujar debugs visuales en la escena: Rojo hasta el objetivo real, Verde hasta la posición sin obstáculos
            Vector3 idealPos = posicionLider + dir * distanciaPreferida;
            Debug.DrawLine(posicionLider, idealPos, Color.green);
            Debug.DrawLine(posicionLider, puntosDeFormacion[i].position, Color.red);

            // Debug en consola para comparar destinos y la posición del jugador principal
            Debug.Log($"[DEBUG FORMACION] Slot {i} Destino Final: {puntosDeFormacion[i].position} | Target Pos: {targetPos} | Lider: {posicionLider} | Distancia: {Vector3.Distance(posicionLider, puntosDeFormacion[i].position):F2}m");
        }
    }
}