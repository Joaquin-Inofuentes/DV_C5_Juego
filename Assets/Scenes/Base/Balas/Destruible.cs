using UnityEngine;

public class Destruible : MonoBehaviour, IDaniable
{
    [Header("Estadísticas")]
    public float vida = 100f;
    public float maxVida = 100f;

    [Header("Regeneración")]
    public float healRate = 5f;
    public float healDelay = 2f;
    private float lastDamageTime;

    void Update()
    {
        if (Time.time - lastDamageTime >= healDelay && vida < maxVida)
        {
            vida = Mathf.MoveTowards(vida, maxVida, healRate * Time.deltaTime);
        }
    }

    public void RecibirDano(int cantidad, GameObject atacante)
    {
        vida -= cantidad;
        lastDamageTime = Time.time;

        if (atacante != null)
        {
            FSMController fsm = GetComponent<FSMController>();

            // Si el que recibe daño no es el líder controlado por el jugador...
            if (fsm != null && fsm.currentState != FSMController.State.Liderando)
            {
                // REACCIÓN AGRESIVA:
                // Si me disparan, mi prioridad es sobrevivir. Cancelo órdenes de caminar o recoger items.
                fsm.objetivo = atacante.transform;
                fsm.tieneOrdenManual = false;
                fsm.LimpiarOrdenDeInteraccion(); // Nueva función en FSM para limpiar botiquines
                fsm.waitTimer = 0;

                Debug.Log($"<color=red>[ALERTA]</color> {name} herido por {atacante.name}. Contraatacando!");
            }
        }

        if (vida <= 0) Morir();
    }

    void Morir()
    {
        if (GlobalData.liderActual != null && GlobalData.liderActual.gameObject == gameObject)
        {
            GlobalData.liderActual = null;
        }
        Destroy(gameObject);
    }
}