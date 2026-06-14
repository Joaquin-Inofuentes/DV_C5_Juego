using UnityEngine;
using Game.Squad;

public class PickUp : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitController unit = other.GetComponent<UnitController>();
        if (unit != null && unit.model.team == Game.Core.UnitTeam.BandoA)
        {
            // Lógica de recolectar item
            unit.model.AddHealth(20);
            Destroy(gameObject);
        }
    }
}