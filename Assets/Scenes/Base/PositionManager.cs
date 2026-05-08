using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PositionManager : MonoBehaviour
{
    public static PositionManager Instance;

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
        LimpiarSoldadosMuertos(); // Limpiamos antes de organizar
        OrganizarFormacion();
    }

    void LimpiarSoldadosMuertos()
    {
        // Elimina de la lista cualquier soldado que haya sido destruido
        todosLosSoldados.RemoveAll(s => s == null);
    }

    void OrganizarFormacion()
    {
        // 1. Limpiar slots asignados (solo a los que aún existen)
        foreach (var s in todosLosSoldados)
        {
            if (s != null) s.slotAsignado = null;
        }

        // 2. Filtrar: Solo seguidores que NO sean líderes y que NO sean null
        List<FSMController> seguidores = todosLosSoldados
            .Where(s => s != null && s.currentState != FSMController.State.Liderando)
            .ToList();

        // 3. Filtrar puntos de formación que no sean null (por si borraste alguno en el editor)
        List<Transform> puntosDisponibles = puntosDeFormacion
            .Where(p => p != null)
            .ToList();

        foreach (var soldado in seguidores)
        {
            Transform mejorPunto = null;
            float minDist = Mathf.Infinity;

            foreach (var punto in puntosDisponibles)
            {
                // Doble validación de seguridad
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