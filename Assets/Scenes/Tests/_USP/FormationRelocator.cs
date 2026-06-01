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
            // Tomamos la posici�n local y la normalizamos para tener solo la direcci�n
            if (p.localPosition.sqrMagnitude > 0.01f)
                direccionesFijasMundo.Add(p.localPosition.normalized);
            else
                direccionesFijasMundo.Add(Vector3.back); // Por defecto atr�s si est� en el centro
        }
    }

    void Update()
    {
        if (GlobalData.liderActual == null) return;

        // Tomamos solo la POSICION del l�der, ignoramos su rotaci�n por completo
        Vector3 posicionLider = GlobalData.liderActual.transform.position;

        for (int i = 0; i < puntosDeFormacion.Count; i++)
        {
            if (puntosDeFormacion[i] == null) continue;

            // 1. Calcular la posici�n ideal usando la direcci�n FIJA del mundo
            // Ya no usamos lider.TransformDirection, as� que el slot no "gira" con el l�der
            Vector3 direccionMundo = direccionesFijasMundo[i];
            Vector3 posicionIdeal = posicionLider + (direccionMundo * distanciaPreferida);

            // 2. Ejecutar L�gica de Reubicaci�n si hay obst�culos
            Vector3 posicionFinal = CalcularPosicionValida(posicionLider, posicionIdeal, direccionMundo);

            // 3. Mover el punto de formaci�n suavemente a la posici�n calculada
            puntosDeFormacion[i].position = Vector3.Lerp(puntosDeFormacion[i].position, posicionFinal, Time.deltaTime * 8f);

            // DEBUG: L�nea blanca que siempre mantiene la misma orientaci�n cardinal
            Debug.DrawLine(posicionLider, puntosDeFormacion[i].position, Color.white);
        }
    }

    Vector3 CalcularPosicionValida(Vector3 origenLider, Vector3 destinoIdeal, Vector3 direccionOriginal)
    {
        // PRUEBA 1: �La posici�n ideal est� despejada?
        if (EsPosicionValida(origenLider, destinoIdeal)) return destinoIdeal;

        // PRUEBA 2: Intentar rotando la direcci�n original 90 grados (Derecha absoluta)
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

        // FALLBACK: Si todo est� rodeado de paredes, pegarse al l�der en la direcci�n original
        return origenLider + (direccionOriginal * distanciaMinima);
    }

    bool EsPosicionValida(Vector3 origen, Vector3 destino)
    {
        Vector3 dir = destino - origen;
        float dist = dir.magnitude;

        // CircleCast 2D (el juego es 2D) para asegurar que el soldado quepa por el camino
        RaycastHit2D hit = Physics2D.CircleCast(origen, radioSeguridadSoldado, dir.normalized, dist, obstacleLayer);
        if (hit.collider != null)
        {
            return false;
        }
        return true;
    }
}