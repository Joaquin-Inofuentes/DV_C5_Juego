using UnityEngine;
using Game.Squad;

public class ShotSensor : MonoBehaviour
{
    public UnitController miController;

    void OnTriggerEnter2D(Collider2D other)
    {
        Bala bala = other.GetComponent<Bala>();
        if (bala != null && bala.dueno != null)
        {
            UnitController atacante = bala.dueno.GetComponent<UnitController>();

            // Si la bala es de alguien de OTRO equipo
            if (atacante != null && atacante.model.team != miController.model.team)
            {
                // Reaccionar al disparo: investigar o atacar
                miController.target = atacante.transform;
            }
        }
    }
}