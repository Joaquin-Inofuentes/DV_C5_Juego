using USP.Entities;
using USP.Core;
using USP.Services;
using UnityEngine;
using System.Collections.Generic;
using Game.Squad;

public class UnitCommander : MonoBehaviour
{
    public List<SoldierController> units = new List<SoldierController>();
    public Transform targetMarker;

    private void Start()
    {
        SuscribirseAOrdenDirecta();

        if (units == null || units.Count == 0)
            Debug.LogWarning("[UnitCommander] Lista 'units' vacía. Asigná J1/J2/J3 en el Inspector: Formacion → UnitCommander → Units.");
    }

    private void OnDestroy()
    {
        if (GEN_Inputs.Instance != null)
            GEN_Inputs.Instance.OnOrdenDirecta -= DarOrdenDirecta;
    }

    private void SuscribirseAOrdenDirecta()
    {
        if (GEN_Inputs.Instance == null)
        {
            Debug.LogWarning("[UnitCommander] GEN_Inputs.Instance es null en Start. Se reintentará en Update.");
            return;
        }
        GEN_Inputs.Instance.OnOrdenDirecta -= DarOrdenDirecta;
        GEN_Inputs.Instance.OnOrdenDirecta += DarOrdenDirecta;
    }

    void Update()
    {
        units.RemoveAll(u => u == null);

        if (GEN_Inputs.Instance == null) return;

        // Garantizar suscripción si GEN_Inputs tardó en inicializarse
        GEN_Inputs.Instance.OnOrdenDirecta -= DarOrdenDirecta;
        GEN_Inputs.Instance.OnOrdenDirecta += DarOrdenDirecta;

        ProcesarHoverInteraccion();

        if (GEN_Inputs.Instance.OrdenPresionada) ProcesarOrden();

        if (GEN_Inputs.Instance.RegresarAFormacion)
        {
            foreach (var unit in units)
                unit?.RegresarAFormacion();
        }
    }

    void ProcesarHoverInteraccion()
    {
        Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
        Collider2D col2D = Physics2D.OverlapPoint(mousePos, 1 << 11);
        if (col2D == null) return;

        IInteractable interactuable = col2D.GetComponent<IInteractable>();
        if (interactuable == null) return;

        SoldierController cercano = GetSoldadoMasCercano(mousePos);
        cercano?.GetComponent<UnitPathRenderer>()?.SetPreviewTarget(col2D.transform.position);
    }

    void ProcesarOrden()
    {
        Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;

        SoldierController soldado = GetSoldadoMasCercano(mousePos);
        if (soldado == null) return;

        // Detección 2D de ítems interactuables (capa 11)
        Collider2D col2D = Physics2D.OverlapPoint(mousePos, 1 << 11);
        if (col2D != null)
        {
            IInteractable interactuable = col2D.GetComponent<IInteractable>();
            if (interactuable != null)
            {
                Debug.DrawLine(soldado.transform.position, col2D.transform.position, Color.white, 5f);
                soldado.SetInteractionOrder(interactuable);
                return;
            }
        }

        MoverMarcador(mousePos);
        soldado.SetOrder(mousePos);
    }

    SoldierController GetSoldadoMasCercano(Vector3 pos)
    {
        SoldierController mejor = null;
        float minDist = Mathf.Infinity;
        foreach (var u in units)
        {
            if (u == null || u.currentState == SoldierController.State.Liderando) continue;
            float d = Vector3.Distance(u.transform.position, pos);
            if (d < minDist) { minDist = d; mejor = u; }
        }
        return mejor;
    }

    void DarOrdenDirecta(int indice)
    {
        if (GEN_Inputs.Instance == null) return;
        if (indice >= units.Count || units[indice] == null) return;
        if (units[indice].currentState == SoldierController.State.Liderando) return;

        Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
        MoverMarcador(mousePos);
        units[indice].SetOrder(mousePos);
    }

    void MoverMarcador(Vector3 pos)
    {
        if (targetMarker == null) return;
        targetMarker.position = pos;
        targetMarker.GetComponent<MarkerAnim>()?.IniciarAnimacion(pos);
    }
}
