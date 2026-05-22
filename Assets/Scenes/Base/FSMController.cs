using UnityEngine;

public class FSMController : MonoBehaviour
{
    public enum State { IrAFormacion, Atacar, IrAAtacar, Investigar, IrAObjetivo, Liderando, Esperando, Interactuando }
    public State currentState = State.Esperando;

    public IA_P2_AgentIA agent;
    public Transform objetivo;
    public Vector3? investigarPos = null; // Posición de un disparo escuchado
    public Transform slotAsignado;

    [Header("Configuración Inteligente")]
    public float distanciaFuego = 6f; // A qué distancia empieza a disparar
    public float distanciaPersecucion = 15f; // Hasta qué distancia persigue

    [Header("Órdenes Manuales")]
    public Vector3 destinoPos;
    public bool tieneOrdenManual = false;
    private IInteractable objetoAInteractuar;

    [Header("Tiempos y Combate")]
    public float waitTimer = 0f;
    public float returnCooldown;
    public Disparador Dispara;
    public float fireRate = 0.5f;
    private float nextFireTime;

    void Update()
    {
        if (returnCooldown > 0) returnCooldown -= Time.deltaTime;

        if (currentState == State.Liderando)
        {
            if (agent.enabled) agent.enabled = false;
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                Disparar();
                nextFireTime = Time.time + fireRate;
            }
            return;
        }

        if (!agent.enabled) agent.enabled = true;
        DetermineState();
        ExecuteState();
    }

    void DetermineState()
    {
        if (currentState == State.Liderando) return;

        // 1. COMBATE (Prioridad Máxima)
        if (objetivo != null && returnCooldown <= 0)
        {
            float dist = Vector3.Distance(transform.position, objetivo.position);
            if (dist <= distanciaFuego) currentState = State.Atacar;
            else if (dist <= distanciaPersecucion) currentState = State.IrAAtacar;
            else { objetivo = null; currentState = State.IrAFormacion; } // Perdió rastro
        }
        // 2. INVESTIGAR DISPARO
        else if (investigarPos.HasValue)
        {
            currentState = State.Investigar;
        }
        // 3. INTERACCION / ORDENES
        else if (objetoAInteractuar != null) currentState = State.Interactuando;
        else if (tieneOrdenManual) currentState = State.IrAObjetivo;
        else if (waitTimer > 0) currentState = State.Esperando;
        else currentState = State.IrAFormacion;
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case State.Atacar:
                agent.StopAgent();
                if (objetivo != null)
                {
                    LookAtTarget2D(objetivo.position);
                    if (Time.time >= nextFireTime) { Disparar(); nextFireTime = Time.time + fireRate; }
                }
                break;

            case State.IrAAtacar:
                if (objetivo != null) MoverA(objetivo.position);
                break;

            case State.Investigar:
                if (investigarPos.HasValue)
                {
                    MoverA(investigarPos.Value);
                    if (Vector3.Distance(transform.position, investigarPos.Value) < 1.5f)
                    {
                        investigarPos = null; // Llegó a investigar y no encontró nada
                        waitTimer = 2f; // Se queda mirando un poco
                    }
                }
                break;

            // Dentro de FSMController.cs, en el switch(currentState)
            case State.Interactuando:
                // VERIFICACIÓN: Comprobamos si el objeto es nulo O si ha sido destruido
                if (objetoAInteractuar == null || (objetoAInteractuar is MonoBehaviour mb && mb == null))
                {
                    objetoAInteractuar = null;
                    currentState = State.IrAFormacion; // Volvemos al estado normal
                    return;
                }

                MoverA(objetoAInteractuar.GetTransform().position);

                if (Vector3.Distance(transform.position, objetoAInteractuar.GetTransform().position) < 1.2f)
                {
                    objetoAInteractuar.Interact(gameObject);
                    objetoAInteractuar = null; // Se limpia la referencia después de interactuar
                }
                break;

            case State.IrAObjetivo:
                MoverA(destinoPos);
                if (Vector3.Distance(transform.position, destinoPos) < 0.8f) { tieneOrdenManual = false; waitTimer = 3f; }
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

    public void InvestigarPosicion(Vector3 pos)
    {
        if (currentState == State.Liderando || objetivo != null) return;
        investigarPos = pos;
    }

    public void SetInteractionOrder(IInteractable interactuable)
    {
        this.objetoAInteractuar = interactuable;
        this.tieneOrdenManual = false;
        this.investigarPos = null;
        this.objetivo = null;
    }

    public void SetOrder(Vector3 newPos)
    {
        this.destinoPos = newPos;
        this.tieneOrdenManual = true;
        this.objetoAInteractuar = null;
        this.investigarPos = null;
        this.objetivo = null;
        this.waitTimer = 0;
    }

    public void RegresarAFormacion()
    {
        if (currentState == State.Liderando) return;
        tieneOrdenManual = false;
        objetoAInteractuar = null;
        investigarPos = null;
        objetivo = null;
        returnCooldown = 2.5f;
        currentState = State.IrAFormacion;
    }

    void MoverA(Vector3 pos) { if (agent.enabled) agent.GoTo(pos); }
    void Disparar() => Dispara.Disparar();

    void LookAtTarget2D(Vector3 pos)
    {
        Vector3 diferencia = pos - transform.position;
        float anguloZ = Mathf.Atan2(diferencia.y, diferencia.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, anguloZ), Time.deltaTime * 10f);
    }
}