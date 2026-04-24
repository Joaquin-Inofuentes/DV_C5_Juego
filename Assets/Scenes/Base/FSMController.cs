using UnityEngine;

public class FSMController : MonoBehaviour
{
    public enum State { IrAFormacion, Atacar, IrAAtacar, IrAObjetivo, Liderando, Esperando, Idle }
    public State currentState = State.Idle;

    public float returnCooldown;

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
        if (returnCooldown > 0)
        {
            returnCooldown -= Time.deltaTime;
            if(currentState == State.IrAObjetivo
                || currentState == State.Esperando)
            {
                returnCooldown = 0;
            }
        }

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

        // PRIORIDAD 1: Orden manual
        if (tieneOrdenManual)
        {
            currentState = State.IrAObjetivo;
        }
        // PRIORIDAD 2: Atacar (Solo si no hay cooldown de "Z")
        else if (objetivo != null && returnCooldown <= 0)
        {
            float dist = Vector2.Distance(transform.position, objetivo.position);
            currentState = (dist <= 4f) ? State.Atacar : State.IrAAtacar;
        }
        // PRIORIDAD 3: Espera temporal tras llegar a un destino manual
        else if (waitTimer > 0)
        {
            currentState = State.Esperando;
        }
        // PRIORIDAD 4: Por defecto, siempre intentar volver a la formación
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
                {
                    LookAtTarget2D(objetivo); // Llamada al método
                    Debug.DrawLine(transform.position, objetivo.position, Color.white);
                }
                break;

            case State.IrAAtacar:
                if (objetivo != null) MoverA(objetivo.position);
                break;

            case State.IrAFormacion:
                if (slotAsignado != null) MoverA(slotAsignado.position);
                break;
        }
    }

    void LookAtTarget2D(Transform target)
    {
        if (target == null) return;

        Vector3 diferencia = target.position - transform.position;
        // Atan2 devuelve el ángulo en radianes, lo pasamos a grados

        float anguloZ = Mathf.Atan2(diferencia.y, diferencia.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, anguloZ);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
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
        returnCooldown = 2f; // Bloqueo de 2 segundos
        currentState = State.IrAFormacion;
    }

    void MoverA(Vector3 pos)
    {
        if (agent.enabled)
        {
            agent.GoTo(pos);
        }
    }

}