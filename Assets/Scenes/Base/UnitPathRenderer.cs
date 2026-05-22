using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class UnitPathRenderer : MonoBehaviour
{
    private LineRenderer line;
    private FSMController fsm;
    private NavMeshAgent navAgent;

    [Header("Configuración Visual")]
    public Color colorMovimiento = Color.white;
    public Color colorPreview = Color.yellow;
    public float anchoLinea = 0.15f;

    private Vector3? previewTarget = null; // Vector3 que puede ser nulo

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        fsm = GetComponent<FSMController>();
        navAgent = GetComponent<NavMeshAgent>();

        // Configuración básica del LineRenderer
        line.useWorldSpace = true;
        line.startWidth = anchoLinea;
        line.endWidth = anchoLinea;
        line.positionCount = 0;

        // Creamos un material simple si no tiene uno
        if (line.material == null || line.material.name.Contains("Default"))
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    void LateUpdate()
    {
        // 1. PRIORIDAD: Si hay un target de "Preview" (Mouse encima de interactuable)
        // Corregido: HasValue (con H mayúscula)
        if (previewTarget.HasValue)
        {
            DibujarCamino(previewTarget.Value, colorPreview);
            previewTarget = null; // Se limpia cada frame, el Commander lo re-asigna en su Update
            return;
        }

        // 2. Si el soldado se está moviendo activamente (Blanco)
        if (fsm.currentState == FSMController.State.IrAObjetivo ||
            fsm.currentState == FSMController.State.Interactuando ||
            fsm.currentState == FSMController.State.IrAAtacar)
        {
            Vector3 destinoActual = transform.position; // Fallback

            if (fsm.currentState == FSMController.State.IrAObjetivo)
            {
                destinoActual = fsm.destinoPos;
            }
            else if (navAgent != null && navAgent.enabled)
            {
                destinoActual = navAgent.destination;
            }

            DibujarCamino(destinoActual, colorMovimiento);
        }
        else
        {
            // Ocultar línea si no hay orden ni preview
            if (line.positionCount != 0) line.positionCount = 0;
        }
    }

    // El UnitCommander llama a esto en su Update si el mouse está sobre un Botiquín
    public void SetPreviewTarget(Vector3 target)
    {
        previewTarget = target;
    }

    void DibujarCamino(Vector3 destino, Color color)
    {
        line.startColor = color;
        line.endColor = color;

        // Si tenemos NavMeshAgent, intentamos dibujar el camino inteligente
        if (navAgent != null && navAgent.enabled && navAgent.hasPath)
        {
            NavMeshPath path = navAgent.path;
            line.positionCount = path.corners.Length;
            for (int i = 0; i < path.corners.Length; i++)
            {
                // Elevamos la línea 0.1 unidades para que no parpadee contra el suelo (Z-fighting)
                line.SetPosition(i, path.corners[i] + Vector3.up * 0.1f);
            }
        }
        else
        {
            // Fallback: Línea recta simple desde el soldado al destino
            line.positionCount = 2;
            line.SetPosition(0, transform.position + Vector3.up * 0.1f);
            line.SetPosition(1, destino + Vector3.up * 0.1f);
        }
    }
}