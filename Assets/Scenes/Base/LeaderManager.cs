using USP.Entities;
using USP.Core;
using USP.Services;
using UnityEngine;
using System.Collections.Generic;
using Game.Squad;

/// <summary>
/// Gestiona la selección y asignación del líder de la escuadra.
/// </summary>
public class LeaderManager : MonoBehaviour
{
    [Header("Pelotón de Soldados")]
    public List<SoldierController> unidades;

    [Header("Configuración")]
    [Tooltip("Panel UI a activar cuando se intenta cambiar a un soldado muerto.")]
    public GameObject MensajeDeQueEstaMuerto;

    [Tooltip("Índice del líder inicial.")]
    public int indiceLiderInicial = 0;

    public static LeaderManager Instance { get; private set; }

    private int liderActualIndex = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SquadEventBus.OnSoldierSwitchRequested += CambiarLider;
        SquadEventBus.OnSoldierDied            += HandleSoldierDied;
        SuscribirseACycleLeader();

        if (unidades != null && unidades.Count > indiceLiderInicial)
            StartCoroutine(InicializarLiderTarde());
        else
            Debug.LogWarning($"[LeaderManager] Lista 'unidades' vacía o indiceLiderInicial ({indiceLiderInicial}) fuera de rango. Asigná J1/J2/J3 en el Inspector.");
    }

    private void OnDisable()
    {
        SquadEventBus.OnSoldierSwitchRequested -= CambiarLider;
        SquadEventBus.OnSoldierDied            -= HandleSoldierDied;

        if (GEN_Inputs.Instance != null)
            GEN_Inputs.Instance.OnCycleLeader -= HandleCycleLeaderFromInputs;
    }

    private void SuscribirseACycleLeader()
    {
        if (GEN_Inputs.Instance == null)
        {
            Debug.LogWarning("[LeaderManager] GEN_Inputs.Instance es null en OnEnable. Se reintentará al final del frame.");
            return;
        }
        // Evitar doble suscripción
        GEN_Inputs.Instance.OnCycleLeader -= HandleCycleLeaderFromInputs;
        GEN_Inputs.Instance.OnCycleLeader += HandleCycleLeaderFromInputs;
    }

    private System.Collections.IEnumerator InicializarLiderTarde()
    {
        yield return new WaitForEndOfFrame();
        SuscribirseACycleLeader();
        liderActualIndex = indiceLiderInicial;
        CambiarLider(indiceLiderInicial);
    }

    private void Update()
    {
        // Re-suscribir si GEN_Inputs tardó en inicializarse
        if (GEN_Inputs.Instance != null)
        {
            GEN_Inputs.Instance.OnCycleLeader -= HandleCycleLeaderFromInputs;
            GEN_Inputs.Instance.OnCycleLeader += HandleCycleLeaderFromInputs;
        }

        if (GlobalData.liderActual != null)
            transform.position = GlobalData.liderActual.transform.position;
    }

    private void HandleCycleLeaderFromInputs(bool forward)
    {
        if (unidades == null || unidades.Count == 0)
        {
            Debug.LogWarning("[LeaderManager] HandleCycleLeader → lista de unidades vacía.");
            return;
        }

        SoldierController currentLeader = GlobalData.liderActual;
        if (currentLeader == null)
        {
            Debug.LogWarning("[LeaderManager] HandleCycleLeader → GlobalData.liderActual es null.");
            return;
        }

        SoldierController bestTarget = BuscarSiguienteUnidad(forward, currentLeader);

        if (bestTarget != null)
        {
            int index = unidades.IndexOf(bestTarget);
            if (index != -1)
            {
                liderActualIndex = index;
                CambiarLider(index);
            }
        }
    }

    /// <summary>Busca la siguiente unidad viva según la dirección de ciclado.</summary>
    private SoldierController BuscarSiguienteUnidad(bool forward, SoldierController liderActual)
    {
        float leaderX = liderActual.transform.position.x;
        SoldierController bestTarget = null;
        float bestValue = forward ? float.MaxValue : float.MinValue;

        foreach (var u in unidades)
        {
            if (u == null || u == liderActual || u.model == null || u.model.IsDead) continue;

            float ux = u.transform.position.x;
            if (forward && ux > leaderX)
            {
                float dist = ux - leaderX;
                if (dist < bestValue) { bestValue = dist; bestTarget = u; }
            }
            else if (!forward && ux < leaderX)
            {
                float dist = leaderX - ux;
                if (dist < bestValue) { bestValue = dist; bestTarget = u; }
            }
        }

        // Wrap-around: si no hay nadie en esa dirección, tomar el extremo opuesto
        if (bestTarget == null)
        {
            float extremeValue = forward ? float.MaxValue : float.MinValue;
            foreach (var u in unidades)
            {
                if (u == null || u == liderActual || u.model == null || u.model.IsDead) continue;
                float ux = u.transform.position.x;
                if (forward  && ux < extremeValue) { extremeValue = ux; bestTarget = u; }
                if (!forward && ux > extremeValue) { extremeValue = ux; bestTarget = u; }
            }
        }

        return bestTarget;
    }

    public void CambiarLider(int index)
    {
        if (unidades == null || index < 0 || index >= unidades.Count) return;

        if (unidades[index] == null || (unidades[index].model != null && unidades[index].model.IsDead))
        {
            Debug.LogError($"<color=red>[LeaderManager] Acceso denegado:</color> Soldado en slot {index + 1} está muerto.");
            if (MensajeDeQueEstaMuerto != null) MensajeDeQueEstaMuerto.SetActive(true);
            return;
        }

        for (int i = 0; i < unidades.Count; i++)
        {
            if (unidades[i] == null) continue;

            bool isSelected = (i == index);
            unidades[i].model.IsLeader = isSelected;

            if (isSelected)
            {
                unidades[i].currentState = SoldierController.State.Liderando;
                unidades[i].CambiarEstado(new LiderandoState());
                unidades[i].view?.SetSelectionActive(true);
                GlobalData.liderActual = unidades[i];
                SquadEventBus.TriggerLeaderChanged(unidades[i]);
            }
            else
            {
                unidades[i].view?.SetSelectionActive(false);
                if (unidades[i].currentState == SoldierController.State.Liderando)
                {
                    unidades[i].currentState = SoldierController.State.IrAFormacion;
                    unidades[i].CambiarEstado(new IrAFormacionState());
                }
            }
        }
    }

    private void HandleSoldierDied(SoldierController deadSoldier)
    {
        bool algunoVivo = false;
        SoldierController masCercano = null;
        float minDist = Mathf.Infinity;

        foreach (var u in unidades)
        {
            if (u == null || u == deadSoldier || u.model == null || u.model.IsDead) continue;
            algunoVivo = true;
            float d = Vector3.Distance(deadSoldier.transform.position, u.transform.position);
            if (d < minDist) { minDist = d; masCercano = u; }
        }

        if (!algunoVivo)
        {
            Debug.Log("<color=red>[LeaderManager] Derrota: todos los soldados han muerto.</color>");
            UnityEngine.SceneManagement.SceneManager.LoadScene("EscenaPerdiste");
            return;
        }

        if (GlobalData.liderActual == deadSoldier && masCercano != null)
        {
            int nuevoIndice = unidades.IndexOf(masCercano);
            if (nuevoIndice != -1)
            {
                liderActualIndex = nuevoIndice;
                CambiarLider(nuevoIndice);
            }
        }
    }

    public void DesactivarMensaje()
    {
        if (MensajeDeQueEstaMuerto != null) MensajeDeQueEstaMuerto.SetActive(false);
    }
}
