using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public string nombreItem = "Botiquín";
    public float curacion = 50f;

    public string GetInteractName() => nombreItem;
    // En InteractableItem.cs
    public Transform GetTransform()
    {
        // Si el objeto está destruido, devolvemos null para que el FSM se entere
        if (this == null) return null;
        return transform;
    }

    public void Interact(GameObject interactuante)
    {
        // 1. LÓGICA SI ES BOTIQUÍN
        if (nombreItem == "Botiquín")
        {
            LeaderManager lm = LeaderManager.Instance;

            if (lm != null)
            {
                // Buscar soldado vivo, no líder, con menos vida
                var objetivo = lm.unidades
                    .Where(u => u != null && u != GlobalData.liderActual)
                    .Select(u => u.GetComponent<Destruible>())
                    .Where(d => d != null && d.vida < d.maxVida)
                    .OrderBy(d => d.vida)
                    .FirstOrDefault();

                if (objetivo != null)
                {
                    Debug.Log($"<color=green>[BOTIQUÍN]</color> Curando a: {objetivo.name}");
                    objetivo.vida = Mathf.Min(objetivo.vida + curacion, objetivo.maxVida);
                    Destroy(gameObject); // Solo se destruye si se usa
                    return;
                }
            }
            // Si el código llega aquí, no había nadie herido (o solo queda el líder)
            Debug.Log("No hay aliados heridos que requieran el botiquín.");
        }
        // 2. LÓGICA SI NO ES BOTIQUÍN (Ej: Enemigos u otros objetos)
        else
        {
            Debug.Log($"<color=blue>[INTERACCION]</color> Interactuando con {nombreItem} sin destruir.");

            // Aquí puedes llamar a métodos específicos del enemigo o ítem si los tienes
            // Ejemplo: interactuante.GetComponent<FSMController>().InteractuarCon(this);

            // Al NO llamar a Destroy(gameObject), el objeto persiste en la escena
        }
    }
}