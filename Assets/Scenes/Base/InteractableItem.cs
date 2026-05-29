using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Squad;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public string nombreItem = "Botiquín";
    public float curacion = 50f;

    public string GetInteractName() => nombreItem;
    
    public Transform GetTransform()
    {
        if (this == null) return null;
        return transform;
    }

    public void Interact(GameObject interactuante)
    {
        // Lógica si es Botiquín
        if (nombreItem == "Botiquín")
        {
            LeaderManager lm = LeaderManager.Instance;

            if (lm != null)
            {
                // Buscar soldado vivo, no líder, con menos vida
                var objetivo = lm.unidades
                    .Where(u => u != null && u != GlobalData.liderActual)
                    .Select(u => u.model)
                    .Where(m => m != null && m.vidaActual < m.vidaMaxima)
                    .OrderBy(m => m.vidaActual)
                    .FirstOrDefault();

                if (objetivo != null)
                {
                    Debug.Log($"<color=green>[BOTIQUÍN]</color> Curando a: {objetivo.name}");
                    objetivo.Curar(curacion);
                    Destroy(gameObject);
                    return;
                }
            }
            Debug.Log("No hay aliados heridos que requieran el botiquín.");
        }
        else
        {
            Debug.Log($"<color=blue>[INTERACCION]</color> Interactuando con {nombreItem} sin destruir.");
        }
    }
}
