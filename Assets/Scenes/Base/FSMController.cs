using UnityEngine;
using Game.Squad;

/// <summary>
/// Clase puente para compatibilidad hacia atrás en escenas y prefabs que aún hacen referencia a FSMController.
/// </summary>
public class FSMController : MonoBehaviour
{
    public enum State { IrAFormacion, Atacar, IrAAtacar, Investigar, IrAObjetivo, Liderando, Esperando, Interactuando }
    
    private SoldierController soldierController;

    private void Awake()
    {
        soldardControllerCheck();
    }

    private void Start()
    {
        soldardControllerCheck();
    }

    private void soldardControllerCheck()
    {
        if (soldierController == null)
        {
            soldierController = GetComponent<SoldierController>();
        }
    }

    public State currentState
    {
        get
        {
            soldardControllerCheck();
            if (soldierController != null)
            {
                return (State)soldierController.currentState;
            }
            return State.Esperando;
        }
        set
        {
            soldardControllerCheck();
            if (soldierController != null)
            {
                soldierController.currentState = (SoldierController.State)value;
            }
        }
    }

    public Transform objetivo
    {
        get
        {
            soldardControllerCheck();
            return soldierController != null ? soldierController.objetivo : null;
        }
        set
        {
            soldardControllerCheck();
            if (soldierController != null) soldierController.objetivo = value;
        }
    }

    public Transform slotAsignado
    {
        get
        {
            soldardControllerCheck();
            return soldierController != null ? soldierController.slotAsignado : null;
        }
        set
        {
            soldardControllerCheck();
            if (soldierController != null) soldierController.slotAsignado = value;
        }
    }

    public Vector3 destinoPos
    {
        get
        {
            soldardControllerCheck();
            return soldierController != null ? soldierController.destinoPos : Vector3.zero;
        }
        set
        {
            soldardControllerCheck();
            if (soldierController != null) soldierController.destinoPos = value;
        }
    }

    public bool tieneOrdenManual
    {
        get
        {
            soldardControllerCheck();
            return soldierController != null ? soldierController.tieneOrdenManual : false;
        }
        set
        {
            soldardControllerCheck();
            if (soldierController != null) soldierController.tieneOrdenManual = value;
        }
    }

    public float waitTimer
    {
        get
        {
            soldardControllerCheck();
            return soldierController != null ? soldierController.waitTimer : 0f;
        }
        set
        {
            soldardControllerCheck();
            if (soldierController != null) soldierController.waitTimer = value;
        }
    }

    public void InvestigarPosicion(Vector3 pos)
    {
        soldardControllerCheck();
        if (soldierController != null) soldierController.InvestigarPosicion(pos);
    }

    public void SetInteractionOrder(IInteractable interactuable)
    {
        soldardControllerCheck();
        if (soldierController != null) soldierController.SetInteractionOrder(interactuable);
    }

    public void SetOrder(Vector3 newPos)
    {
        soldardControllerCheck();
        if (soldierController != null) soldierController.SetOrder(newPos);
    }

    public void RegresarAFormacion()
    {
        soldardControllerCheck();
        if (soldierController != null) soldierController.RegresarAFormacion();
    }
}
