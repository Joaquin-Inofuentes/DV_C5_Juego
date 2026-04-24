using UnityEngine;

public class FSMController : MonoBehaviour
{
    public enum State { IrAFormacion, Atacar, IrAAtacar, IrAObjetivo, Liderando, Esperando, Idle }
    public State currentState = State.Idle;



    [Header("Órdenes Manuales")]
    public Vector3 destinoPos; // Guardamos la posición, no el objeto
    public bool tieneOrdenManual = false;


    public IA_P2_AgentIA agent;
    public Transform destino;
    public Transform objetivo;
    public Transform slotAsignado;
    public GameObject selectionIndicator;

    [Header("Tiempos")]
    public float waitTimer = 0f;
    public float waitDuration = 4f; // Tiempo que espera antes de volver

    void Update()
    {
        if (currentState == State.Liderando)
        {
            if (agent.enabled) agent.enabled = false;
            if (selectionIndicator != null) selectionIndicator.SetActive(true);
            return;
        }
        else
        {
            if (!agent.enabled) agent.enabled = true;
            if (selectionIndicator != null) selectionIndicator.SetActive(false);
        }

        DetermineState();
        ExecuteState();
    }

    void DetermineState()
    {
        if (currentState == State.Liderando) return;

        // PRIORIDAD 1: Ir a objetivo manual (Vector3)
        if (tieneOrdenManual)
        {
            currentState = State.IrAObjetivo;
        }
        // PRIORIDAD 2: Esperar en el sitio tras llegar
        else if (waitTimer > 0)
        {
            currentState = State.Esperando;
        }
        // PRIORIDAD 3: Atacar si hay enemigo
        else if (objetivo != null)
        {
            float dist = Vector2.Distance((Vector2)transform.position, (Vector2)objetivo.position);
            currentState = (dist <= 4f) ? State.Atacar : State.IrAAtacar;
        }
        else
        {
            currentState = State.IrAFormacion;
        }
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case State.IrAObjetivo:
                MoverA(destinoPos); // Usamos la posición guardada
                if (Vector2.Distance((Vector2)transform.position, (Vector2)destinoPos) < 0.6f)
                {
                    tieneOrdenManual = false; // Marcamos como completada
                    waitTimer = waitDuration;
                    agent.StopAgent();
                }
                break;

            case State.Esperando:
                agent.StopAgent();
                waitTimer -= Time.deltaTime;
                break;

            case State.Atacar:
                agent.StopAgent();
                if (objetivo != null)
                    Debug.DrawLine(transform.position, objetivo.position, Color.white);
                break;

            case State.IrAAtacar:
                if (objetivo != null) MoverA(objetivo.position);
                break;

            case State.IrAFormacion:
                if (slotAsignado != null) MoverA(slotAsignado.position);
                break;
        }
    }

    // Reemplaza el SetOrder anterior por este que recibe Vector3
    public void SetOrder(Vector3 newPos)
    {
        this.destinoPos = newPos;
        this.tieneOrdenManual = true;
        this.objetivo = null;
        this.waitTimer = 0;
    }

    public void RegresarAFormacion()
    {
        if (currentState == State.Liderando) return;

        tieneOrdenManual = false;
        waitTimer = 0;
        objetivo = null;
        currentState = State.IrAFormacion;
        Debug.Log(gameObject.name + " regresando a formación.");
    }

    void MoverA(Vector3 pos)
    {
        if (agent.enabled)
        {
            agent.GoTo(pos);
        }
    }

}