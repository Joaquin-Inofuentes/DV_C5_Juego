using Game.Core;
using Game.Squad;
using System.Linq;
using UnityEngine;

public class UnitCommander : MonoBehaviour
{
    private bool _suscrito = false;
    private Vector3 _ultimaPosOrden;
    private bool _hayOrdenPendiente = false;

    void Start()
    {
        if (GEN_Inputs.Instance == null)
            Debug.LogError("[UnitCommander] No se encontró GEN_Inputs en la escena.");

        if (LeaderManager.Instance == null)
            Debug.LogWarning("[UnitCommander] No se encontró LeaderManager. Las órdenes 1/2/3 no funcionarán.");
    }

    void Update()
    {
        SuscribirseAInputs();

        if (GEN_Inputs.Instance == null) return;

        // Z: todas las unidades con orden vuelven a formación
        if (GEN_Inputs.Instance.RegresarAFormacion)
        {
            var enEspera = FindObjectsOfType<UnitController>()
                .Where(u => u.isWaitingOrder && !u.model.IsDead);
            foreach (var u in enEspera)
            {
                u.isWaitingOrder = false;
                u.CambiarEstado(new SeguirFormacionState());
            }
        }

        // Click derecho: mandar al aliado más cercano a esa posición
        if (GEN_Inputs.Instance.OrdenPresionada)
        {
            Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
            _ultimaPosOrden = mousePos;
            _hayOrdenPendiente = true;

            Debug.Log($"<color=cyan>[UnitCommander]</color> Click derecho en posición {mousePos}. Buscando aliado más cercano...");
            MandarMasCercano(mousePos);
        }
    }

    private void SuscribirseAInputs()
    {
        if (_suscrito || GEN_Inputs.Instance == null) return;
        GEN_Inputs.Instance.OnOrdenDirecta += OnOrdenDirecta;
        _suscrito = true;
        Debug.Log("[UnitCommander] Suscrito a GEN_Inputs.OnOrdenDirecta (teclas 1/2/3).");
    }

    void OnDestroy()
    {
        if (GEN_Inputs.Instance != null)
            GEN_Inputs.Instance.OnOrdenDirecta -= OnOrdenDirecta;
    }

    private void OnOrdenDirecta(int index)
    {
        if (LeaderManager.Instance == null)
        {
            Debug.LogWarning("[UnitCommander] LeaderManager no encontrado. No se puede dar orden.");
            return;
        }

        var unidades = LeaderManager.Instance.unidades;
        if (index < 0 || index >= unidades.Count)
        {
            Debug.LogWarning($"[UnitCommander] Índice {index} fuera de rango. Hay {unidades.Count} unidades.");
            return;
        }

        var unidad = unidades[index];
        if (unidad == null || unidad.model.IsDead)
        {
            Debug.LogWarning($"[UnitCommander] Unidad {index + 1} está muerta o no existe.");
            return;
        }

        Vector3 destino = GEN_Inputs.Instance.MouseWorldPosition;

        if (unidad.model.IsLeader)
        {
            Debug.Log($"<color=cyan>[UnitCommander]</color> Tecla {index + 1} es el líder. Enviando al más cercano al destino.");
            MandarMasCercano(destino);
            return;
        }

        Debug.Log($"<color=cyan>[UnitCommander]</color> Orden directa: {unidad.name} (tecla {index + 1}) → ir a {destino}");
        unidad.MoveToPoint(destino);
        unidad.CambiarEstado(new IrADestinoState());
    }

    void MandarMasCercano(Vector3 destino)
    {
        UnitController mejorCandidato = null;
        float minDist = Mathf.Infinity;

        var aliados = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.PlayerTeam && !u.model.IsLeader && !u.model.IsDead);

        int count = 0;
        foreach (var a in aliados)
        {
            count++;
            float d = Vector3.Distance(a.transform.position, destino);
            if (d < minDist)
            {
                minDist = d;
                mejorCandidato = a;
            }
        }

        if (count == 0)
        {
            Debug.LogWarning("[UnitCommander] No hay aliados disponibles (no-líder, vivos) para enviar.");
            return;
        }

        if (mejorCandidato != null)
        {
            Debug.Log($"<color=cyan>[UnitCommander]</color> Enviando a {mejorCandidato.name} (dist: {minDist:F1}) → {destino}");
            mejorCandidato.MoveToPoint(destino);
            mejorCandidato.CambiarEstado(new IrADestinoState());
        }
    }
}
