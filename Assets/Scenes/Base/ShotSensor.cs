using UnityEngine;

public class ShotSensor : MonoBehaviour
{
    public FSMController fsm;

    void Awake()
    {
        if (fsm == null) fsm = GetComponentInParent<FSMController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si lo que entra es una Bala
        Bala bala = other.GetComponent<Bala>();
        if (bala != null)
        {
            // Si el dueño de la bala no es mi propio equipo
            if (bala.dueño != null && bala.dueño.layer != transform.root.gameObject.layer)
            {
                Debug.Log($"<color=orange>[SENTIDOS]</color> {transform.root.name} detectó un disparo cercano de {bala.dueño.name}");

                // Si no tiene un objetivo visual, va a investigar de donde vino la bala
                if (fsm.objetivo == null)
                {
                    fsm.InvestigarPosicion(bala.dueño.transform.position);
                }
            }
        }
    }
}