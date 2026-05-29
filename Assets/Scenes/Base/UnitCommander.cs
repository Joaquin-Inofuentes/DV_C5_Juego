using UnityEngine;
using System.Collections.Generic;
using Game.Squad;

public class UnitCommander : MonoBehaviour
{
    public List<SoldierController> units = new List<SoldierController>();
    public Transform targetMarker;

    void Update()
    {
        units.RemoveAll(u => u == null);

        // 1. Lógica de HOVER
        ProcesarHoverInteraccion();

        // 2. Lógica de CLICKS
        if (Input.GetMouseButtonDown(1)) ProcesarOrden();

        if (Input.GetKeyDown(KeyCode.F1)) DarOrdenDirecta(0);
        if (Input.GetKeyDown(KeyCode.F2)) DarOrdenDirecta(1);
        if (Input.GetKeyDown(KeyCode.F3)) DarOrdenDirecta(2);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            foreach (var unit in units)
            {
                if (unit != null) unit.RegresarAFormacion();
            }
        }
    }

    void ProcesarHoverInteraccion()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = 1 << 11; // Solo Capa 11: Interactuables

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            IInteractable interactuable = hit.collider.GetComponent<IInteractable>();
            if (interactuable != null)
            {
                SoldierController cercano = GetSoldadoMasCercano(hit.point);
                if (cercano != null)
                {
                    UnitPathRenderer pathVisual = cercano.GetComponent<UnitPathRenderer>();
                    if (pathVisual != null)
                    {
                        pathVisual.SetPreviewTarget(hit.collider.transform.position);
                    }
                }
            }
        }
    }

    void ProcesarOrden()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        SoldierController soldado = GetSoldadoMasCercano(mousePos);
        if (soldado == null) return;

        if (Physics.Raycast(ray, out hit, 100f, 1 << 11))
        {
            IInteractable interactuable = hit.collider.GetComponent<IInteractable>();
            if (interactuable != null)
            {
                Debug.DrawLine(soldado.transform.position, hit.collider.transform.position, Color.white, 5f);
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
        if (indice >= units.Count || units[indice] == null || units[indice].currentState == SoldierController.State.Liderando) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        MoverMarcador(mousePos);
        units[indice].SetOrder(mousePos);
    }

    void MoverMarcador(Vector3 pos)
    {
        if (targetMarker != null)
        {
            targetMarker.position = pos;
            MarkerAnim anim = targetMarker.GetComponent<MarkerAnim>();
            //if (anim != null) anim.Play();
        }
    }
}
