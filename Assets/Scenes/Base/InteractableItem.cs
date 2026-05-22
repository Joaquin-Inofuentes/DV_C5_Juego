using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public string nombreItem = "Botiquín";
    public float curacion = 50f;

    public string GetInteractName() => nombreItem;
    public Transform GetTransform() => transform;

    public void Interact(GameObject interactuante)
    {
        Debug.Log($"<color=green>[INTERACCION]</color> {interactuante.name} usó {nombreItem}");

        Destruible d = interactuante.GetComponent<Destruible>();
        if (d != null)
        {
            d.vida = Mathf.Min(d.vida + curacion, d.maxVida);
        }
        Destroy(gameObject);
    }
}