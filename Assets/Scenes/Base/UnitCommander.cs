using UnityEngine;
using System.Collections.Generic;
using Game.Squad;

public class UnitCommander : MonoBehaviour
{
    public List<SoldierController> units = new List<SoldierController>();
    public Transform targetMarker;

    private void Start()
    {
        if (GEN_Inputs.Instance != null)
        {
            GEN_Inputs.Instance.OnOrdenDirecta += DarOrdenDirecta;
        }
    }

    private void OnDestroy()
    {
        if (GEN_Inputs.Instance != null)
        {
            GEN_Inputs.Instance.OnOrdenDirecta -= DarOrdenDirecta;
        }
    }

    void Update()
    {
        units.RemoveAll(u => u == null);

        // Si el manager de inputs no está disponible, intentar reconectarse
        if (GEN_Inputs.Instance == null) return;

        // Suscripción tardía si no se logró en Start por orden de ejecución
        GEN_Inputs.Instance.OnOrdenDirecta -= DarOrdenDirecta;
        GEN_Inputs.Instance.OnOrdenDirecta += DarOrdenDirecta;

        // 1. Lógica de HOVER
        ProcesarHoverInteraccion();

        // 2. Lógica de CLICKS
        if (GEN_Inputs.Instance.OrdenPresionada) ProcesarOrden();

        if (GEN_Inputs.Instance.RegresarAFormacion)
        {
            foreach (var unit in units)
            {
                if (unit != null) unit.RegresarAFormacion();
            }
        }
    }

    void ProcesarHoverInteraccion()
    {
        if (GEN_Inputs.Instance == null || Camera.main == null) return;
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
        if (GEN_Inputs.Instance == null || Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;

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
        if (GEN_Inputs.Instance == null) return;
        if (indice >= units.Count || units[indice] == null || units[indice].currentState == SoldierController.State.Liderando) return;
        Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
        MoverMarcador(mousePos);
        units[indice].SetOrder(mousePos);
    }

    void MoverMarcador(Vector3 pos)
    {
        if (targetMarker != null)
        {
            targetMarker.position = pos;
            MarkerAnim anim = targetMarker.GetComponent<MarkerAnim>();
            if (anim != null) anim.IniciarAnimacion(pos);
        }
    }
}
