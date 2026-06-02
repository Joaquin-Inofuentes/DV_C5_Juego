using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Squad;
using Game.Core;

public class PositionManager : MonoBehaviour
{
    public static PositionManager Instance;
    public List<Transform> puntosFormacion;

    void Awake() => Instance = this;

    void Update()
    {
        OrganizarEscuadra();
    }

    void OrganizarEscuadra()
    {
        if (GlobalData.liderActual == null) return;

        // Filtrar: Solo aliados vivos que NO sean el l�der actual
        var seguidores = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.PlayerTeam && !u.model.IsLeader && !u.model.IsDead)
            .ToList();

        int puntoIndex = 0;
        foreach (var unidad in seguidores)
        {
            if (puntoIndex < puntosFormacion.Count)
            {
                unidad.currentSlot = puntosFormacion[puntoIndex];
                var estado = unidad.GetCurrentState();
                if (estado is EsperandoState || estado == null)
                    unidad.CambiarEstado(new SeguirFormacionState());
                puntoIndex++;
            }
        }
    }
}