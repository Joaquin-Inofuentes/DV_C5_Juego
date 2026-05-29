using UnityEngine;
using Game.Squad;

public class ShotSensor : MonoBehaviour
{
    public SoldierController controller;

    void Awake()
    {
        if (controller == null) controller = GetComponentInParent<SoldierController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Bala bala = other.GetComponent<Bala>();
        if (bala != null)
        {
            if (bala.dueño != null && bala.dueño.layer != transform.root.gameObject.layer)
            {
                Debug.Log($"<color=orange>[SENTIDOS]</color> {transform.root.name} detectó un disparo cercano de {bala.dueño.name}");

                if (controller != null && controller.objetivo == null)
                {
                    controller.InvestigarPosicion(bala.dueño.transform.position);
                }
            }
        }
    }
}
