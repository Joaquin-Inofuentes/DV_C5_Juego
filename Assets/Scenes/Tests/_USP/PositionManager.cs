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

        // Filtrar: Solo aliados vivos que NO sean el lder actual
        var seguidores = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.BandoA && !u.model.IsLeader && !u.model.IsDead)
            .ToList();

        foreach (var unidad in seguidores)
        {
            unidad.currentSlot = null; // Quitar slots por completo
            var estado = unidad.GetCurrentState();
            if (!unidad.isWaitingOrder && (estado is EsperandoState || estado == null))
                unidad.CambiarEstado(new SeguirFormacionState());
        }
    }
}