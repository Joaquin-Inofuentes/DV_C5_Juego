using USP.Entities;
using USP.Core;
using USP.Services;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Squad;

public class PositionManager : MonoBehaviour
{
    public static PositionManager Instance;

    public List<Transform> puntosDeFormacion = new List<Transform>();
    public List<SoldierController> todosLosSoldados = new List<SoldierController>();

    void OnEnable()
    {
        Instance = this;
    }

    public void Awake()
    {
        OnEnable();
    }

    void Update()
    {
        LimpiarSoldadosMuertos();
        OrganizarFormacion();
    }

    void LimpiarSoldadosMuertos()
    {
        todosLosSoldados.RemoveAll(s => s == null);
    }

    void OrganizarFormacion()
    {
        foreach (var s in todosLosSoldados)
        {
            if (s != null) s.slotAsignado = null;
        }

        List<SoldierController> seguidores = todosLosSoldados
            .Where(s => s != null && s.currentState != SoldierController.State.Liderando)
            .ToList();

        List<Transform> puntosDisponibles = puntosDeFormacion
            .Where(p => p != null)
            .ToList();

        foreach (var soldado in seguidores)
        {
            Transform mejorPunto = null;
            float minDist = Mathf.Infinity;

            foreach (var punto in puntosDisponibles)
            {
                if (punto == null || soldado == null) continue;

                float d = Vector2.Distance(soldado.transform.position, punto.position);
                if (d < minDist)
                {
                    minDist = d;
                    mejorPunto = punto;
                }
            }

            if (mejorPunto != null && soldado != null)
            {
                soldado.slotAsignado = mejorPunto;
                puntosDisponibles.Remove(mejorPunto);
            }
        }
    }
}

