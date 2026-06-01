using UnityEngine;
using Game.Squad;

[RequireComponent(typeof(LineRenderer))]
public class UnitPathRenderer : MonoBehaviour
{
    private LineRenderer line;
    private UnitController controller;
    private UnitFSM fsm;

    public Color colorMovimiento = Color.white;
    public float anchoLinea = 0.15f;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        controller = GetComponent<UnitController>();
        fsm = GetComponent<UnitFSM>();

        line.startWidth = anchoLinea;
        line.endWidth = anchoLinea;
        line.positionCount = 0;
    }

    void LateUpdate()
    {
        if (controller == null || fsm == null) return;

        // Dibujar si está en un estado de movimiento
        if (fsm.currentState == UnitFSM.State.IrADestino || fsm.currentState == UnitFSM.State.SeguirLider)
        {
            DibujarCamino();
        }
        else
        {
            line.positionCount = 0;
        }
    }

    void DibujarCamino()
    {
        line.startColor = colorMovimiento;
        line.endColor = colorMovimiento;
        line.positionCount = 2;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, controller.agent.isMoving ? controller.agent.targetObject.transform.position : transform.position);
    }
}