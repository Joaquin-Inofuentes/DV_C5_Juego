using UnityEngine;

public interface IInteractable
{
    string GetInteractName();
    void Interact(GameObject interactuante);
    Transform GetTransform();
}