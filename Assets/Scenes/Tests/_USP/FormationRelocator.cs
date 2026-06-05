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

    // Guardamos los offsets como vectores de direcci�n fijos
    private List<Vector3> direccionesFijasMundo = new List<Vector3>();

    void Start()
    {
        // Al inicio, guardamos la direcci�n relativa original bas�ndonos en la jerarqu�a
        // pero la trataremos como una direcci�n absoluta en el mundo
        foreach (var p in puntosDeFormacion)
        {
            // Tomamos la posicin local y la normalizamos para tener solo la direccin
            if (p.localPosition.sqrMagnitude > 0.01f)
                direccionesFijasMundo.Add(p.localPosition.normalized);
            else
                direccionesFijasMundo.Add(Vector3.back); // Por defecto atrs si est en el centro
        }
    }

    void Update()
    {
        if (GlobalData.liderActual == null) return;

        // Tomamos solo la POSICION del líder, ignoramos su rotación por completo
        Vector3 posicionLider = GlobalData.liderActual.transform.position;

        List<Vector3> posicionesCalculadas = new List<Vector3>();

        for (int i = 0; i < puntosDeFormacion.Count; i++)
        {
            if (puntosDeFormacion[i] == null) continue;

            // 1. Calcular la posición ideal usando la dirección FIJA del mundo
            Vector3 direccionMundo = direccionesFijasMundo[i];
            Vector3 posicionIdeal = posicionLider + (direccionMundo * distanciaPreferida);

            // 2. Ejecutar Lógica de Reubicación si hay obstáculos o conflicto con otros slots
            Vector3 posicionFinal = CalcularPosicionValida(posicionLider, posicionIdeal, direccionMundo, posicionesCalculadas);

            // Registrar posición calculada para que los siguientes slots no se encimen aquí
            posicionesCalculadas.Add(posicionFinal);

            // 3. Mover el punto de formación suavemente a la posición calculada
            puntosDeFormacion[i].position = Vector3.Lerp(puntosDeFormacion[i].position, posicionFinal, Time.deltaTime * 8f);

            // DEBUG: Línea blanca que siempre mantiene la misma orientación cardinal
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