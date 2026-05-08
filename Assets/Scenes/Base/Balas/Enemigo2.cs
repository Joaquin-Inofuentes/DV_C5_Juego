using UnityEngine;

public class Enemigo2 : MonoBehaviour, IDaniable
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
        // lastDamageTime = Time.time; // Si usas regeneración

        // REACCIÓN: Ir hacia el atacante
        if (atacante != null)
        {
            FSMController fsm = GetComponent<FSMController>();

            // Solo le damos la orden si NO es el líder actual (para no quitarle el control al jugador)
            if (fsm != null && fsm.currentState != FSMController.State.Liderando)
            {
                // Le damos la orden de ir a la posición donde estaba el atacante
                fsm.SetOrder(atacante.transform.position);
                //Debug.Log(gameObject.name + " va tras " + atacante.name);
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