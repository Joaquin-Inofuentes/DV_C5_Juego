using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IA_F_ControllerSeguidor : MonoBehaviour
{
    public GameObject Posicion;
    public GameObject target; // El objetivo a seguir
    public IA_P2_AgentIA agent; // Referencia al agente para moverlo
    public List<GameObject> Enemigos; // Lista de enemigos para detectar
    public LayerMask obstacleLayer; // Capa de obstculos para el raycast
    public IA_F_EnemyCercanos Cercanos;
    void Update()
    {
        if (agent == null || Cercanos == null) return;

        target = ObtenerEnemigo();

        if (Posicion == null) return;
        if (target != null )
        {
            agent.GoTo(target.transform.position);
        }
        else
        {
            if (Posicion != null)
            {
                agent.GoTo(Posicion.transform.position);
            }
        }

    }

    private GameObject ObtenerEnemigo()
    {
        Enemigos = Cercanos.Colisionados;

        List<GameObject> enemigosVisibles = new List<GameObject>();
        foreach (var enemigo in Enemigos)
        {
            if (enemigo == null)
                continue; // Si el enemigo ha sido destruido, lo saltamos
            Transform PosibleObjetivo = enemigo.transform;
            if (EsVisible(PosibleObjetivo))
            {
                enemigosVisibles.Add(enemigo);
                Debug.DrawLine(enemigo.transform.position, agent.transform.position,Color.blue);
            }
        }
        // Eliminamos los enemigos destruidos de la lista (recorriendo al revs para no corromper ndices)
        for (int i = Enemigos.Count - 1; i >= 0; i--)
        {
            if (Enemigos[i] == null) Enemigos.RemoveAt(i);
        }
        // Ordenamos de mas cercano a mas lejos respecto a este agente
        Vector3 PosicionPropia = agent.transform.position;
        enemigosVisibles.Sort((a, b) =>
            Vector3.Distance(PosicionPropia, a.transform.position).CompareTo(
                Vector3.Distance(PosicionPropia, b.transform.position)));
        // Seleccionamos el visible mas cercano (no la lista sin filtrar)
        GameObject enemigoMasCercano = enemigosVisibles.Count > 0 ? enemigosVisibles[0] : null;

        return enemigoMasCercano;
    }

    public bool EsVisible(Transform PosibleEnemigo)
    {
        Transform PosicionAgente = agent.transform;

        // Direccin desde el agente hacia el posible enemigo
        Vector3 direccion = PosibleEnemigo.position - PosicionAgente.position;

        // Distancia al posible enemigo
        float distancia = Vector3.Distance(PosicionAgente.position, PosibleEnemigo.position);

        // Realizamos un Raycast2D para verificar si hay obstculos entre el agente y el posible enemigo
        RaycastHit2D hit = Physics2D.Raycast(PosicionAgente.position, direccion.normalized, distancia, obstacleLayer);
        if (hit.collider == null)
        {
            // Si no hay colisin con un obstculo, el enemigo es visible
            return true;
        }

        // Si hay colisin con un obstculo, el enemigo no es visible
        return false;
    }
}
