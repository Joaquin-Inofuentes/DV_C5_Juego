using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [Header("Ajustes de Visualización")]
    public float radioEsfera = 0.5f;
    public float duracionLinea = 1.0f;

    // --- COLISIONES FÍSICAS (Sólidas) ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Log directo
        Debug.Log($"<color=green>[ENTER SÓLIDO 2D]</color> Objeto: <b>{gameObject.name}</b> chocó con <b>{collision.gameObject.name}</b>");

        // Esfera verde en el punto de impacto
        if (collision.contactCount > 0)
        {
            Debug.DrawLine(transform.position, collision.contacts[0].point, Color.green, duracionLinea);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Log directo
        Debug.Log($"<color=yellow>[STAY SÓLIDO 2D]</color> <b>{gameObject.name}</b> sigue tocando a <b>{collision.gameObject.name}</b>");

        // Línea amarilla constante al punto de contacto
        if (collision.contactCount > 0)
        {
            Debug.DrawLine(transform.position, collision.contacts[0].point, Color.yellow);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Log directo
        Debug.Log($"<color=red>[EXIT SÓLIDO 2D]</color> <b>{gameObject.name}</b> se separó de <b>{collision.gameObject.name}</b>");
    }

    // --- TRIGGERS (Zonas/Atravesables) ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Log directo
        Debug.Log($"<color=blue>[ENTER TRIGGER 2D]</color> <b>{gameObject.name}</b> entró en la zona de <b>{other.gameObject.name}</b>");

        // Línea azul al centro del trigger
        Debug.DrawLine(transform.position, other.transform.position, Color.blue, duracionLinea);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Log directo
        Debug.Log($"<color=cyan>[STAY TRIGGER 2D]</color> <b>{gameObject.name}</b> dentro de <b>{other.gameObject.name}</b>");

        // Línea cian constante
        Debug.DrawLine(transform.position, other.transform.position, Color.cyan);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Log directo
        Debug.Log($"<color=magenta>[EXIT TRIGGER 2D]</color> <b>{gameObject.name}</b> salió de la zona de <b>{other.gameObject.name}</b>");
    }

    // --- GIZMOS (Siempre visibles en la Scene) ---

    private void OnDrawGizmos()
    {
        // Dibujamos una esfera de alambre alrededor del objeto para saber que tiene el script
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radioEsfera);
    }
}
