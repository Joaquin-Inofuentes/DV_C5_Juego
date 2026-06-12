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
        // PRUEBA 1: ¿La posición ideal está despejada?
        if (EsPosicionValida(origenLider, destinoIdeal, posicionesOcupadas)) return destinoIdeal;

        // PRUEBA 2: Intentar rotando la dirección original 90 grados (Derecha absoluta)
        Vector3 dirDerecha = Quaternion.Euler(0, 0, -90) * direccionOriginal;
        Vector3 posDerecha = origenLider + (dirDerecha * distanciaPreferida);
        if (EsPosicionValida(origenLider, posDerecha, posicionesOcupadas)) return posDerecha;

        // PRUEBA 3: Intentar rotando 180 grados (Opuesto absoluto)
        Vector3 dirOpuesta = Quaternion.Euler(0, 0, 180) * direccionOriginal;
        Vector3 posOpuesta = origenLider + (dirOpuesta * distanciaPreferida);
        if (EsPosicionValida(origenLider, posOpuesta, posicionesOcupadas)) return posOpuesta;

        // PRUEBA 4: Intentar rotando 90 grados a la izquierda (Izquierda absoluta)
        Vector3 dirIzquierda = Quaternion.Euler(0, 0, 90) * direccionOriginal;
        Vector3 posIzquierda = origenLider + (dirIzquierda * distanciaPreferida);
        if (EsPosicionValida(origenLider, posIzquierda, posicionesOcupadas)) return posIzquierda;

        // PRUEBA 5: Intentar a distancia mínima en la dirección original
        Vector3 posCercana = origenLider + (direccionOriginal * distanciaMinima);
        if (EsPosicionValida(origenLider, posCercana, posicionesOcupadas)) return posCercana;

        // FALLBACK: Si todo está completamente bloqueado, los esparcimos en abanico alrededor del líder
        float angulo = posicionesOcupadas.Count * (360f / puntosDeFormacion.Count) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angulo) * 0.8f, Mathf.Sin(angulo) * 0.8f, 0f);
        return origenLider + offset;
    }

    bool EsPosicionValida(Vector3 origen, Vector3 destino, List<Vector3> posicionesOcupadas)
    {
        // 1. Evitar encimar slots en la misma posición (distancia mínima de 0.8 unidades)
        foreach (var pos in posicionesOcupadas)
        {
            if (Vector3.Distance(destino, pos) < 0.8f)
            {
                return false;
            }
        }

        // 2. Obtener la máscara de capa de obstáculos
        LayerMask mask = obstacleLayer;
        if (mask == 0)
        {
            var model = FindObjectOfType<IA_P2_PathfindingModel>();
            if (model != null)
            {
                mask = model.obstacleLayer;
            }
            else
            {
                mask = LayerMask.GetMask("Obstacles", "Obstaculos");
                if (mask == 0) mask = (1 << 6) | (1 << 14); // Fallback a capa 6 (antigua) y 14 (Obstacles)
            }
        }

        // 3. Comprobar si el punto final está superpuesto directamente con un obstáculo
        Collider2D col = Physics2D.OverlapCircle(destino, radioSeguridadSoldado, mask);
        if (col != null)
        {
            return false;
        }

        // 4. Comprobar mediante CircleCast si hay camino libre desde el origen al destino
        Vector3 dir = destino - origen;
        float dist = dir.magnitude;
        if (dist > 0.01f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(origen, radioSeguridadSoldado, dir.normalized, dist, mask);
            if (hit.collider != null)
            {
                return false;
            }
        }

        return true;
    }
}