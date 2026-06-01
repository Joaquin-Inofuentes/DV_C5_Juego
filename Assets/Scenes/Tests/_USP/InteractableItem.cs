using UnityEngine;
using System.Linq;
using Game.Squad;
using Game.Core;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public string nombreItem = "Botiquín";
    public float curacion = 50f;

    public string GetInteractName() => nombreItem;
    public Transform GetTransform() => this == null ? null : transform;

    public void Interact(GameObject interactuante)
    {
        if (nombreItem == "Botiquín")
        {
            // Buscar la unidad del equipo Player con menos vida que no sea el líder (o incluirlo si quieres)
            var unidadesAliadas = FindObjectsOfType<UnitController>()
                .Where(u => u.model.team == UnitTeam.PlayerTeam && !u.model.IsDead)
                .Select(u => u.model)
                .Where(m => m.healthActual < m.healthMax)
                .OrderBy(m => m.healthActual)
                .FirstOrDefault();

            if (unidadesAliadas != null)
            {
                Debug.Log($"<color=green>[CURACIÓN]</color> Aplicada a {unidadesAliadas.name}");
                unidadesAliadas.AddHealth(curacion);
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("No hay heridos que necesiten el botiquín.");
            }
        }
    }
}