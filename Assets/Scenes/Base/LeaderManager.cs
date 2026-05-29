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

        SoldierController currentLeader = GlobalData.liderActual;
        if (currentLeader == null) return;

        float leaderX = currentLeader.transform.position.x;
        SoldierController bestTarget = null;

        if (forward) // E: Closest soldier to the right
        {
            float minDistance = float.MaxValue;
            foreach (var u in unidades)
            {
                if (u != null && u != currentLeader && u.model != null && !u.model.IsDead)
                {
                    if (u.transform.position.x > leaderX)
                    {
                        float dist = u.transform.position.x - leaderX;
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = u;
                        }
                    }
                }
            }

            // Wrap around to the leftmost soldier if none on the right
            if (bestTarget == null)
            {
                float minX = float.MaxValue;
                foreach (var u in unidades)
                {
                    if (u != null && u != currentLeader && u.model != null && !u.model.IsDead)
                    {
                        if (u.transform.position.x < minX)
                        {
                            minX = u.transform.position.x;
                            bestTarget = u;
                        }
                    }
                }
            }
        }
        else // Q: Closest soldier to the left
        {
            float minDistance = float.MaxValue;
            foreach (var u in unidades)
            {
                if (u != null && u != currentLeader && u.model != null && !u.model.IsDead)
                {
                    if (u.transform.position.x < leaderX)
                    {
                        float dist = leaderX - u.transform.position.x;
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = u;
                        }
                    }
                }
            }

            // Wrap around to the rightmost soldier if none on the left
            if (bestTarget == null)
            {
                float maxX = float.MinValue;
                foreach (var u in unidades)
                {
                    if (u != null && u != currentLeader && u.model != null && !u.model.IsDead)
                    {
                        if (u.transform.position.x > maxX)
                        {
                            maxX = u.transform.position.x;
                            bestTarget = u;
                        }
                    }
                }
            }
        }

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
        // 1. Verificar si toda la escuadra ha sido eliminada
        bool algunoVivo = false;
        SoldierController masCercano = null;
        float minDist = Mathf.Infinity;

        foreach (var u in unidades)
        {
            if (u != null && u != deadSoldier && u.model != null && !u.model.IsDead)
            {
                algunoVivo = true;
                float d = Vector3.Distance(deadSoldier.transform.position, u.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    masCercano = u;
                }
            }
        }

        if (!algunoVivo)
        {
            Debug.Log("<color=red>Derrota:</color> Todos los soldados han muerto. Cargando escena de derrota.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("EscenaPerdiste");
            return;
        }

        // 2. Si el que murió era el líder, auto-reasignar al vivo más cercano
        if (GlobalData.liderActual == deadSoldier)
        {
            int nuevoIndice = unidades.IndexOf(masCercano);
            if (nuevoIndice != -1)
            {
                Debug.Log($"<color=orange>[SQUAD]</color> El líder ha muerto. Reasignando control a {masCercano.name} (el más cercano).");
                liderActualIndex = nuevoIndice;
                CambiarLider(nuevoIndice);
            }
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
