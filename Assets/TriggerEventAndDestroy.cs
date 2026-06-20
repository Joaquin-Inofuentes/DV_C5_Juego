using UnityEngine;
using UnityEngine.Events;

public class TriggerEventAndDestroy : MonoBehaviour
{
    [Header("Eventos de Unity")]
    [Tooltip("Coloca aquí las funciones que quieres que se ejecuten al entrar al trigger.")]
    public UnityEvent onTriggerEnterEvent;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Debuguea con quién colisionó
        Debug.Log($"[Trigger] Colisión detectada con: {collision.gameObject.name}");
        if (collision.gameObject.name.Contains("J_"))
        {
            // 2. Invoca el Unity Event si no es nulo
            if (onTriggerEnterEvent != null)
            {
                onTriggerEnterEvent.Invoke();
                Debug.Log($"[Trigger] ¡Unity Event invocado con éxito por {gameObject.name}!");
            }

            // 3. Destruye este GameObject
            Destroy(gameObject);
        }
    }
}