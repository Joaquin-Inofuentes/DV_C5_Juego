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

    // Guardamos los offsets como vectores de dirección fijos
    private List<Vector3> direccionesFijasMundo = new List<Vector3>();

    void Start()
    {
        // Al inicio, guardamos la dirección relativa original basándonos en la jerarquía
        // pero la trataremos como una dirección absoluta en el mundo
        foreach (var p in puntosDeFormacion)
        {
            // Tomamos la posición local y la normalizamos para tener solo la dirección
            if (p.localPosition.sqrMagnitude > 0.01f)
                direccionesFijasMundo.Add(p.localPosition.normalized);
            else
                direccionesFijasMundo.Add(Vector3.back); // Por defecto atrás si está en el centro
        }
    }

    void Update()
    {
        if (GlobalData.liderActual == null) return;

        // Tomamos solo la POSICION del líder, ignoramos su rotación por completo
        Vector3 posicionLider = GlobalData.liderActual.transform.position;

        for (int i = 0; i < puntosDeFormacion.Count; i++)
        {
            if (puntosDeFormacion[i] == null) continue;

            // 1. Calcular la posición ideal usando la dirección FIJA del mundo
            // Ya no usamos lider.TransformDirection, así que el slot no "gira" con el líder
            Vector3 direccionMundo = direccionesFijasMundo[i];
            Vector3 posicionIdeal = posicionLider + (direccionMundo * distanciaPreferida);

            // 2. Ejecutar Lógica de Reubicación si hay obstáculos
            Vector3 posicionFinal = CalcularPosicionValida(posicionLider, posicionIdeal, direccionMundo);

            // 3. Mover el punto de formación suavemente a la posición calculada
            puntosDeFormacion[i].position = Vector3.Lerp(puntosDeFormacion[i].position, posicionFinal, Time.deltaTime * 8f);

            // DEBUG: Línea blanca que siempre mantiene la misma orientación cardinal
            Debug.DrawLine(posicionLider, puntosDeFormacion[i].position, Color.white);
        }
    }

    Vector3 CalcularPosicionValida(Vector3 origenLider, Vector3 destinoIdeal, Vector3 direccionOriginal)
    {
        // PRUEBA 1: ¿La posición ideal está despejada?
        if (EsPosicionValida(origenLider, destinoIdeal)) return destinoIdeal;

        // PRUEBA 2: Intentar rotando la dirección original 90 grados (Derecha absoluta)
        Vector3 dirDerecha = Quaternion.Euler(0, 0, -90) * direccionOriginal;
        Vector3 posDerecha = origenLider + (dirDerecha * distanciaPreferida);
        if (EsPosicionValida(origenLider, posDerecha)) return posDerecha;

        // PRUEBA 3: Intentar rotando 180 grados (Opuesto absoluto)
        Vector3 dirOpuesta = Quaternion.Euler(0, 0, 180) * direccionOriginal;
        Vector3 posOpuesta = origenLider + (dirOpuesta * distanciaPreferida);
        if (EsPosicionValida(origenLider, posOpuesta)) return posOpuesta;

        // PRUEBA 4: Intentar rotando 90 grados a la izquierda (Izquierda absoluta)
        Vector3 dirIzquierda = Quaternion.Euler(0, 0, 90) * direccionOriginal;
        Vector3 posIzquierda = origenLider + (dirIzquierda * distanciaPreferida);
        if (EsPosicionValida(origenLider, posIzquierda)) return posIzquierda;

        // FALLBACK: Si todo está rodeado de paredes, pegarse al líder en la dirección original
        return origenLider + (direccionOriginal * distanciaMinima);
    }

    bool EsPosicionValida(Vector3 origen, Vector3 destino)
    {
        Vector3 dir = destino - origen;
        float dist = dir.magnitude;

        // Raycast circular (SphereCast) para asegurar que el soldado quepa por el camino
        RaycastHit hit;
        if (Physics.SphereCast(origen, radioSeguridadSoldado, dir.normalized, out hit, dist, obstacleLayer))
        {
            return false;
        }
        return true;
    }
}