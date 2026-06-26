using Game.Core;
using Game.Squad;
using System.Linq;
using UnityEngine;

public class UnitCommander : MonoBehaviour
{
    private bool _suscrito = false;
    private Vector3 _ultimaPosOrden;

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

        // Click derecho: si el click es sobre un aliado caído, mandar revividor; si es sobre un enemigo, atacar; si no, mandar al destino
        if (GEN_Inputs.Instance.OrdenPresionada)
        {
            Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
            _ultimaPosOrden = mousePos;

            // Detectar si hay un aliado caído cerca del click
            UnitController clickedDown = null;
            float minClickDist = 1.5f;
            foreach (var u in FindObjectsOfType<UnitController>())
            {
                if (!u.IsDown() || u.model.team != UnitTeam.BandoA) continue;
                float d = Vector3.Distance(mousePos, u.transform.position);
                if (d < minClickDist) { minClickDist = d; clickedDown = u; }
            }

            if (clickedDown != null)
            {
                Debug.Log($"<color=lime>[UnitCommander]</color> Click en aliado caído: {clickedDown.name}. Enviando revividor.");
                MandarRevivir(clickedDown);
                return;
            }

            // Detectar si hay un enemigo cerca del click
            UnitController clickedEnemy = null;
            float minEnemyClickDist = 1.5f;
            foreach (var u in FindObjectsOfType<UnitController>())
            {
                if (u.model.team == UnitTeam.BandoA || u.model.IsDead) continue;
                float d = Vector3.Distance(mousePos, u.transform.position);
                if (d < minEnemyClickDist) { minEnemyClickDist = d; clickedEnemy = u; }
            }

            if (clickedEnemy != null)
            {
                Debug.Log($"<color=red>[UnitCommander]</color> Click en enemigo: {clickedEnemy.name}. Enviando aliado a atacar.");
                MandarAtacarEnemigo(clickedEnemy);
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
            .Where(u => u.model.team == UnitTeam.BandoA
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

        // Filtrar aliados: vivos y que no sean líderes (ignorar si ya están moviéndose, para que agarre el más cercano siempre)
        var aliadosLibres = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.BandoA && !u.model.IsLeader && !u.model.IsDead)
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

    void MandarAtacarEnemigo(UnitController enemy)
    {
        // Encontrar aliados: vivos, que no sean líderes y que no estén caídos
        var aliados = FindObjectsOfType<UnitController>()
            .Where(u => u.model.team == UnitTeam.BandoA && !u.model.IsLeader && !u.model.IsDead)
            .ToList();

        if (aliados.Count == 0) return;

        // Si ambos están "sin movimiento" (ej: SeguirFormacion o EsperandoState), mandar a los dos.
        // Si no, mandar solo al más cercano.
        var aliadosSinMovimiento = aliados.Where(u => !u.isWaitingOrder && u.GetCurrentState() is SeguirFormacionState || u.GetCurrentState() is EsperandoState).ToList();

        if (aliadosSinMovimiento.Count >= 2)
        {
            Debug.Log($"<color=red>[UnitCommander]</color> {aliadosSinMovimiento.Count} aliados libres, enviando a todos a atacar a {enemy.name}.");
            foreach (var a in aliadosSinMovimiento)
            {
                a.target = enemy.transform;
                a.isWaitingOrder = true;
                a.CambiarEstado(new PerseguirState());
                a.view.ShowSpeech("¡Atacando enemigo!", 2.5f);
            }
        }
        else
        {
            // Mandar al más cercano
            UnitController mejorCandidato = null;
            float minDist = Mathf.Infinity;
            foreach (var a in aliados)
            {
                float d = Vector3.Distance(a.transform.position, enemy.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    mejorCandidato = a;
                }
            }

            if (mejorCandidato != null)
            {
                Debug.Log($"<color=red>[UnitCommander]</color> {mejorCandidato.name} enviado a atacar a {enemy.name}.");
                mejorCandidato.target = enemy.transform;
                mejorCandidato.isWaitingOrder = true;
                mejorCandidato.CambiarEstado(new PerseguirState());
                mejorCandidato.view.ShowSpeech("¡Atacando enemigo!", 2.5f);
            }
        }
    }
}
