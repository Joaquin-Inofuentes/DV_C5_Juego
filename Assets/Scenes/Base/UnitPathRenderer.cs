using USP.Entities;
using USP.Core;
using USP.Services;
using UnityEngine;
using UnityEngine.AI;
using Game.Squad;

[RequireComponent(typeof(LineRenderer))]
public class UnitPathRenderer : MonoBehaviour
{
    private LineRenderer line;
    private SoldierController controller;
    private NavMeshAgent navAgent;

    [Header("Configuración Visual")]
    public Color colorMovimiento = Color.white;
    public Color colorPreview = Color.yellow;
    public float anchoLinea = 0.15f;

    private Vector3? previewTarget = null;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        controller = GetComponent<SoldierController>();
        navAgent = GetComponent<NavMeshAgent>();

        line.useWorldSpace = true;
        line.startWidth = anchoLinea;
        line.endWidth = anchoLinea;
        line.positionCount = 0;

        if (line.material == null || line.material.name.Contains("Default"))
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    void LateUpdate()
    {
        if (controller == null) return;

        if (previewTarget.HasValue)
        {
            DibujarCamino(previewTarget.Value, colorPreview);
            previewTarget = null;
            return;
        }

        if (controller.currentState == SoldierController.State.IrAObjetivo ||
            controller.currentState == SoldierController.State.Interactuando ||
            controller.currentState == SoldierController.State.IrAAtacar)
        {
            Vector3 destinoActual = transform.position;

            if (controller.currentState == SoldierController.State.IrAObjetivo)
            {
                destinoActual = controller.destinoPos;
            }
            else if (navAgent != null && navAgent.enabled)
            {
                destinoActual = navAgent.destination;
            }

            DibujarCamino(destinoActual, colorMovimiento);
        }
        else
        {
            if (line.positionCount != 0) line.positionCount = 0;
        }
    }

    public void SetPreviewTarget(Vector3 target)
    {
        previewTarget = target;
    }

    void DibujarCamino(Vector3 destino, Color color)
    {
        line.startColor = color;
        line.endColor = color;

        if (navAgent != null && navAgent.enabled && navAgent.hasPath)
        {
            NavMeshPath path = navAgent.path;
            line.positionCount = path.corners.Length;
            for (int i = 0; i < path.corners.Length; i++)
            {
                line.SetPosition(i, path.corners[i] + Vector3.up * 0.1f);
            }
        }
        else
        {
            line.positionCount = 2;
            line.SetPosition(0, transform.position + Vector3.up * 0.1f);
            line.SetPosition(1, destino + Vector3.up * 0.1f);
        }
    }
}

