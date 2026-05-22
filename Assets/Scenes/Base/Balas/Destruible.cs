using UnityEngine;

public class Destruible : MonoBehaviour, IDaniable
{
    public float vida = 100f;
    public float maxVida = 100f;
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
            if (fsm != null && fsm.currentState != FSMController.State.Liderando)
            {
                // Si el enemigo es visible, lo marcamos como objetivo
                fsm.objetivo = atacante.transform;

                // Si está muy lejos para verlo, al menos sabemos de donde vino el daño
                fsm.InvestigarPosicion(atacante.transform.position);

                fsm.tieneOrdenManual = false;
                fsm.waitTimer = 0;
            }
        }

        if (vida <= 0) Morir();
    }

    void Morir()
    {
        if (GlobalData.liderActual != null && GlobalData.liderActual.gameObject == gameObject)
            GlobalData.liderActual = null;

        Destroy(gameObject);
    }
}