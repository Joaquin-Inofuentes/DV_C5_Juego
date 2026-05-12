using UnityEngine;

public class Destruible : MonoBehaviour, IDaniable
{
    [Header("Estadísticas")]
    public float vida = 100f;
    public float maxVida = 100f;

    [Header("Regeneración")]
    public float healRate = 5f;      // Cuánta vida recupera por segundo
    public float healDelay = 2f;     // Tiempo de espera tras recibir daño
    private float lastDamageTime;    // Cuándo fue la última vez que recibió daño

    void Update()
    {
        // 1. Verificar si ha pasado el tiempo de espera (2 segundos)
        // 2. Verificar si le falta vida
        if (Time.time - lastDamageTime >= healDelay && vida < maxVida)
        {
            // Curación suave usando MoveTowards (más preciso para rate constante)
            // o Lerp si quieres que cure más rápido al principio y lento al final
            vida = Mathf.MoveTowards(vida, maxVida, healRate * Time.deltaTime);
        }
    }

    public void RecibirDano(int cantidad, GameObject atacante)
    {
        vida -= cantidad;
        lastDamageTime = Time.time; // Activamos el cooldown de regeneración

        if (atacante != null)
        {
            FSMController fsm = GetComponent<FSMController>();

            if (fsm != null && fsm.currentState != FSMController.State.Liderando)
            {
                // PRIORIDAD: Si ya tiene un objetivo, no lo cambiamos (Enemigo Único)
                // Si no tiene ninguno, le asignamos el que nos acaba de disparar
                if (fsm.objetivo == null)
                {
                    fsm.objetivo = atacante.transform;

                    // IMPORTANTE: Reseteamos órdenes manuales para que la FSM 
                    // pase a estado IrAAtacar/Atacar inmediatamente
                    fsm.tieneOrdenManual = false;
                    fsm.waitTimer = 0;
                }
            }
        }

        if (vida <= 0) Morir();
    }

    void Morir()
    {
        // Si el que muere es el líder actual, limpiar la referencia
        if (GlobalData.liderActual != null && GlobalData.liderActual.gameObject == gameObject)
        {
            GlobalData.liderActual = null;
        }

        Destroy(gameObject);
    }
}