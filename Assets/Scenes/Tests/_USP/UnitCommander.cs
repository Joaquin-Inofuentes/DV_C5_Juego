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

        // Click derecho: si el click es sobre un aliado caído, mandar revividor; si no, mandar al destino
        if (GEN_Inputs.Instance.OrdenPresionada)
        {
            Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
            _ultimaPosOrden = mousePos;
            _hayOrdenPendiente = true;

            // Detectar si hay un aliado caído cerca del click
            UnitController clickedDown = null;
            float minClickDist = 1.5f;
            foreach (var u in FindObjectsOfType<UnitController>())
            {
                if (!u.IsDown() || u.model.team != UnitTeam.PlayerTeam) continue;
                float d = Vector3.Distance(mousePos, u.transform.position);
                if (d < minClickDist) { minClickDist = d; clickedDown = u; }
            }

            if (clickedDown != null)
            {
                Debug.Log($"<color=lime>[UnitCommander]</color> Click en aliado caído: {clickedDown.name}. Enviando revividor.");
                MandarRevivir(clickedDown);
                return;
            }

            Debug.Log($"<color=cyan>[UnitCommander]</color> Click derecho en {mousePos}. Buscando aliado...");
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
        _destinosActivos[unidad] = destino;
        unidad.MoveToPoint(destino);
        unidad.CambiarEstado(new IrADestinoState());
    }

    // Diccionario para registrar el destino actual asignado a cada unidad
    private static readonly System.Collections.Generic.Dictionary<UnitController, Vector3> _destinosActivos = 
        new System.Collections.Generic.Dictionary<UnitController, Vector3>();

    private void LimpiarDestinosInactivos()
    {
        var inactivos = _destinosActivos.Keys
            .Where(u => u == null || u.model.IsDead || !u.isWaitingOrder)
            .ToList();
        foreach (var u in inactivos)
        {
            _destinosActivos.Remove(u);
        }
    }

    private Vector3 ObtenerDestinoAjustado(Vector3 destinoOriginal)
    {
        LimpiarDestinosInactivos();
        Vector3 destinoAjustado = destinoOriginal;
        bool posicionOcupada = true;
        int intentos = 0;

        // Si la posición ya está ocupada por otra unidad (distancia menor a 1.2 unidades), la desplazamos en espiral
        while (posicionOcupada && intentos < 8)
        {
            posicionOcupada = false;
            foreach (var pos in _destinosActivos.Values)
            {
                if (Vector3.Distance(pos, destinoAjustado) < 1.2f)
                {
                    posicionOcupada = true;
                    // Desplazamiento en espiral simple
                    float angulo = intentos * 45f * Mathf.Deg2Rad;
                    float radio = 1.2f;
                    destinoAjustado = destinoOriginal + new Vector3(Mathf.Cos(angulo) * radio, Mathf.Sin(angulo) * radio, 0f);
                    break;
                }
            }
            intentos++;
        }
        return destinoAjustado;
    }

    void MandarRevivir(UnitController downed)
    {
        var candidatos = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.PlayerTeam
                && !u.model.IsLeader && !u.model.IsDown && !u.isWaitingOrder)
            .OrderBy(u => Vector3.Distance(u.transform.position, downed.transform.position))
            .ToList();

        if (candidatos.Count == 0)
        {
            Debug.LogWarning("[UnitCommander] No hay aliados libres para revivir.");
            return;
        }

        var revividor = candidatos[0];
        revividor.MoveToPoint(downed.transform.position);
        revividor.CambiarEstado(new IrADestinoState());
        revividor.view.ShowSpeech("¡Voy a rescatarte!", 2.5f);
        downed.view.ShowSpeech("¡Por fin! ¡Aguantando!", 2.5f);

        Debug.Log($"<color=lime>[UnitCommander]</color> {revividor.name} enviado a rescatar a {downed.name}.");
    }

    void MandarMasCercano(Vector3 destino)
    {
        LimpiarDestinosInactivos();

        // Filtrar aliados: vivos, que no sean líderes, y que NO estén actualmente realizando una orden (libres)
        var aliadosLibres = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.PlayerTeam && !u.model.IsLeader && !u.model.IsDead && !u.isWaitingOrder)
            .ToList();

        if (aliadosLibres.Count == 0)
        {
            Debug.LogWarning("[UnitCommander] Todos los aliados están ocupados o no hay disponibles.");
            return;
        }

        UnitController mejorCandidato = null;
        float minDist = Mathf.Infinity;

        foreach (var a in aliadosLibres)
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
            Vector3 destinoFinal = ObtenerDestinoAjustado(destino);
            Debug.Log($"<color=cyan>[UnitCommander]</color> Enviando a {mejorCandidato.name} (libre, dist: {minDist:F1}) → {destinoFinal}");
            
            _destinosActivos[mejorCandidato] = destinoFinal;
            mejorCandidato.MoveToPoint(destinoFinal);
            mejorCandidato.CambiarEstado(new IrADestinoState());
        }
    }
}
