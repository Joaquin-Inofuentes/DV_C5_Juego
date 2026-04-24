using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PositionManager : MonoBehaviour
{
    public static PositionManager Instance; // Singleton para acceso rápido

    public List<Transform> puntosDeFormacion = new List<Transform>();
    public List<FSMController> todosLosSoldados = new List<FSMController>();

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
        OrganizarFormacion();
    }

    void OrganizarFormacion()
    {
        // 1. Limpiar slots asignados
        foreach (var s in todosLosSoldados) s.slotAsignado = null;

        // 2. Filtrar solo los que están en estado de formación
        List<FSMController> seguidores = todosLosSoldados
            .Where(s => s.currentState == FSMController.State.IrAFormacion)
            .ToList();

        List<Transform> puntosDisponibles = new List<Transform>(puntosDeFormacion);

        foreach (var soldado in seguidores)
        {
            Transform mejorPunto = null;
            float minDist = Mathf.Infinity;

            foreach (var punto in puntosDisponibles)
            {
                if (punto == null) continue;
                float d = Vector2.Distance(soldado.transform.position, punto.position);
                if (d < minDist)
                {
                    minDist = d;
                    mejorPunto = punto;
                }
            }

            if (mejorPunto != null)
            {
                soldado.slotAsignado = mejorPunto;
                puntosDisponibles.Remove(mejorPunto);
            }
        }
    }
}