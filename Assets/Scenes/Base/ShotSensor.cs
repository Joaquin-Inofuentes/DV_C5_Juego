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
            if (bala.dueno != null && bala.dueno.layer != transform.root.gameObject.layer)
            {
                Debug.Log($"<color=orange>[SENTIDOS]</color> {transform.root.name} detectó un disparo cercano de {bala.dueno.name}");

                if (controller != null && controller.objetivo == null)
                {
                    controller.InvestigarPosicion(bala.dueno.transform.position);
                }
            }
        }
    }
}
