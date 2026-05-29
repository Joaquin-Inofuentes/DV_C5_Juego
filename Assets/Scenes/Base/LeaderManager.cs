using UnityEngine;
using System.Collections.Generic;
using Game.Squad;

/// <summary>
/// Gestiona la selección y asignación del líder de la escuadra utilizando el EventBus.
/// </summary>
public class LeaderManager : MonoBehaviour
{
    [Header("Pelotón de Soldados")]
    public List<SoldierController> unidades;

    [Header("Configuración")]
    [Tooltip("Panel UI o texto de advertencia a activar cuando el jugador intenta cambiar a un soldado muerto.")]
    public GameObject MensajeDeQueEstaMuerto;

    [Tooltip("Índice de inicio para el líder de la escuadra.")]
    public int indiceLiderInicial = 0;

    public static LeaderManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private int liderActualIndex = -1;

    private void OnEnable()
    {
        // Suscribirse a eventos
        SquadEventBus.OnSoldierSwitchRequested += CambiarLider;
        SquadEventBus.OnSoldierDied += HandleSoldierDied;

        if (GEN_Inputs.Instance != null)
        {
            GEN_Inputs.Instance.OnCycleLeader += HandleCycleLeaderFromInputs;
        }

        if (unidades != null && unidades.Count > indiceLiderInicial)
        {
            StartCoroutine(InicializarLiderTarde());
        }
    }

    private void OnDisable()
    {
        // Cancelar suscripción
        SquadEventBus.OnSoldierSwitchRequested -= CambiarLider;
        SquadEventBus.OnSoldierDied -= HandleSoldierDied;

        if (GEN_Inputs.Instance != null)
        {
            GEN_Inputs.Instance.OnCycleLeader -= HandleCycleLeaderFromInputs;
        }
    }

    private System.Collections.IEnumerator InicializarLiderTarde()
    {
        yield return new WaitForEndOfFrame();
        if (GEN_Inputs.Instance != null)
        {
            GEN_Inputs.Instance.OnCycleLeader -= HandleCycleLeaderFromInputs;
            GEN_Inputs.Instance.OnCycleLeader += HandleCycleLeaderFromInputs;
        }
        liderActualIndex = indiceLiderInicial;
        CambiarLider(indiceLiderInicial);
    }

    private void Update()
    {
        if (GEN_Inputs.Instance != null)
        {
            GEN_Inputs.Instance.OnCycleLeader -= HandleCycleLeaderFromInputs;
            GEN_Inputs.Instance.OnCycleLeader += HandleCycleLeaderFromInputs;
        }

        // Si el líder actual existe, posicionar este objeto en su posición (para pivots de formación)
        if (GlobalData.liderActual != null)
        {
            transform.position = GlobalData.liderActual.transform.position;
        }
    }

    private void HandleCycleLeaderFromInputs(bool forward)
    {
        if (unidades == null || unidades.Count == 0) return;

        int step = forward ? 1 : -1;
        int startIndex = (liderActualIndex == -1) ? 0 : liderActualIndex;
        int nextIndex = startIndex;

        for (int i = 0; i < unidades.Count; i++)
        {
            nextIndex = (nextIndex + step + unidades.Count) % unidades.Count;
            if (unidades[nextIndex] != null && unidades[nextIndex].model != null && !unidades[nextIndex].model.IsDead)
            {
                liderActualIndex = nextIndex;
                SquadEventBus.TriggerSoldierSwitchRequested(nextIndex);
                return;
            }
        }
    }

    public void CambiarLider(int index)
    {
        if (unidades == null || index < 0 || index >= unidades.Count) return;

        // Si el soldado está muerto (nulo)
        if (unidades[index] == null || (unidades[index].model != null && unidades[index].model.IsDead))
        {
            Debug.LogError($"<color=red>ACCESO DENEGADO:</color> El soldado en el slot {index + 1} ha muerto y no puede liderar.");
            if (MensajeDeQueEstaMuerto != null)
            {
                MensajeDeQueEstaMuerto.SetActive(true);
            }
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
        // Verificar si toda la escuadra ha sido eliminada
        bool algunoVivo = false;
        foreach (var u in unidades)
        {
            if (u != null && u.model != null && !u.model.IsDead)
            {
                algunoVivo = true;
                break;
            }
        }

        if (!algunoVivo)
        {
            Debug.Log("<color=red>Derrota:</color> Todos los soldados han muerto.");
        }
    }

    public void DesactivarMensaje()
    {
        if (MensajeDeQueEstaMuerto != null)
        {
            MensajeDeQueEstaMuerto.SetActive(false);
        }
    }
}
