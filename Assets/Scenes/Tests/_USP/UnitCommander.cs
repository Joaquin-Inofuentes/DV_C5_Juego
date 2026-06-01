using Game.Core;
using Game.Squad;
using System.Linq;
using UnityEngine;

public class UnitCommander : MonoBehaviour
{
    void Update()
    {
        // Si presionas click derecho
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            MandarOrdenAAlguien(mousePos);
        }
    }

    void MandarOrdenAAlguien(Vector3 destino)
    {
        // Buscar el aliado más cercano al click (que no sea el líder)
        UnitController mejorCandidato = null;
        float minDist = Mathf.Infinity;

        var aliados = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.PlayerTeam && !u.model.IsLeader);

        foreach (var a in aliados)
        {
            float d = Vector3.Distance(a.transform.position, destino);
            if (d < minDist)
            {
                minDist = d;
                mejorCandidato = a;
            }
        }

        if (mejorCandidato != null)
        {
            // Accedemos a la FSM para cambiar el estado
            UnitFSM fsm = mejorCandidato.GetComponent<UnitFSM>();
            if (fsm != null) fsm.SetDestination(destino);
        }
    }
}