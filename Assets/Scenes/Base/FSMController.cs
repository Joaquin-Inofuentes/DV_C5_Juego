using UnityEngine;

public class FSMController : MonoBehaviour
{
    public enum State { IrAFormacion, Atacar, IrAAtacar, IrAObjetivo, Liderando, Esperando, Idle, Interactuando }
    public State currentState = State.Idle;

    public IA_P2_AgentIA agent;
    public Transform objetivo; // El enemigo actual
    public Transform slotAsignado;
    public GameObject selectionIndicator;

    [Header("Órdenes Manuales")]
    public Vector3 destinoPos;
    public bool tieneOrdenManual = false;
    private IInteractable objetoAInteractuar;

    [Header("Tiempos")]
    public float waitTimer = 0f;
    public float waitDuration = 4f;
    public float returnCooldown;

    [Header("Combate")]
    public Disparador Dispara;
    public float fireRate = 0.5f;
    private float nextFireTime;

    void Update()
    {
        if (returnCooldown > 0) returnCooldown -= Time.deltaTime;

        if (currentState == State.Liderando)
        {
            if (agent.enabled) agent.enabled = false;
            if (selectionIndicator != null) selectionIndicator.SetActive(true);
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                Disparar();
                nextFireTime = Time.time + fireRate;
            }
            return;
        }

        if (!agent.enabled) agent.enabled = true;
        if (selectionIndicator != null) selectionIndicator.SetActive(false);

        DetermineState();
        ExecuteState();
    }

    void DetermineState()
    {
        if (currentState == State.Liderando) return;

        // PRIORIDAD 1: Combatir (Si tiene un objetivo y no está en cooldown de retirada)
        if (objetivo != null && returnCooldown <= 0)
        {
            float dist = Vector3.Distance(transform.position, objetivo.position);
            currentState = (dist <= 6f) ? State.Atacar : State.IrAAtacar;
        }
        // PRIORIDAD 2: Interactuar con item (Botiquines)
        else if (objetoAInteractuar != null)
        {
            currentState = State.Interactuando;
        }
        // PRIORIDAD 3: Ir a posición marcada por click derecho
        else if (tieneOrdenManual)
        {
            currentState = State.IrAObjetivo;
        }
        // PRIORIDAD 4: Esperar tras llegar a destino
        else if (waitTimer > 0)
        {
            currentState = State.Esperando;
        }
        // PRIORIDAD 5: Volver a la formación
        else
        {
            currentState = State.IrAFormacion;
        }
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case State.Atacar:
                agent.StopAgent();
                if (objetivo != null)
                {
                    LookAtTarget2D(objetivo);
                    if (Time.time >= nextFireTime) { Disparar(); nextFireTime = Time.time + fireRate; }
                }
                break;

            case State.IrAAtacar:
                if (objetivo != null) MoverA(objetivo.position);
                break;

            case State.Interactuando:
                if (objetoAInteractuar == null || objetoAInteractuar.GetTransform() == null) { objetoAInteractuar = null; return; }
                MoverA(objetoAInteractuar.GetTransform().position);
                if (Vector3.Distance(transform.position, objetoAInteractuar.GetTransform().position) < 1.2f)
                {
                    objetoAInteractuar.Interact(gameObject);
                    objetoAInteractuar = null;
                    agent.StopAgent();
                }
                break;

            case State.IrAObjetivo:
                MoverA(destinoPos);
                if (Vector3.Distance(transform.position, destinoPos) < 0.8f)
                {
                    tieneOrdenManual = false;
                    waitTimer = waitDuration;
                    agent.StopAgent();
                }
                break;

            case State.IrAFormacion:
                if (slotAsignado != null) MoverA(slotAsignado.position);
                break;

            case State.Esperando:
                agent.StopAgent();
                waitTimer -= Time.deltaTime;
                break;
        }
    }

    public void SetInteractionOrder(IInteractable interactuable)
    {
        this.objetoAInteractuar = interactuable;
        this.tieneOrdenManual = false;
        this.objetivo = null;
    }

    public void LimpiarOrdenDeInteraccion() => objetoAInteractuar = null;

    public void SetOrder(Vector3 newPos)
    {
        this.destinoPos = newPos;
        this.tieneOrdenManual = true;
        this.objetoAInteractuar = null;
        this.objetivo = null;
        this.waitTimer = 0;
    }

    public void RegresarAFormacion()
    {
        if (currentState == State.Liderando) return;
        tieneOrdenManual = false;
        objetoAInteractuar = null;
        waitTimer = 0;
        objetivo = null;
        returnCooldown = 2.5f; // Bloqueo de combate breve para poder huir
        currentState = State.IrAFormacion;
    }

    void MoverA(Vector3 pos) { if (agent.enabled) agent.GoTo(pos); }
    void Disparar() => Dispara.Disparar();

    void LookAtTarget2D(Transform target)
    {
        if (target == null) return;
        Vector3 diferencia = target.position - transform.position;
        float anguloZ = Mathf.Atan2(diferencia.y, diferencia.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, anguloZ), Time.deltaTime * 10f);
    }
}