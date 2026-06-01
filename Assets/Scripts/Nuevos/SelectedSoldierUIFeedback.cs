using UnityEngine;
using Game.Squad;

public class SelectedSoldierUIFeedback : MonoBehaviour
{
    public void OnLeaderChanged(UnitController newLeader)
    {
        // Tu lógica de feedback visual aquí
        if (newLeader == GetComponent<UnitController>())
        {
            // Mostrar efectos de selección
        }
    }
}