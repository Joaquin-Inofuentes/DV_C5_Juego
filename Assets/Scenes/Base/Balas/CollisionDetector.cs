using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [Header("Ajustes de Visualización")]
    public float radioEsfera = 0.5f;
    public float duracionLinea = 1.0f;

    // --- COLISIONES FÍSICAS (Sólidas) ---

    private void OnCollisionEnter(Collision collision)
    {
        // Log directo
        Debug.Log($"<color=green>[ENTER SÓLIDO]</color> Objeto: <b>{gameObject.name}</b> chocó con <b>{collision.gameObject.name}</b>");

        // Esfera verde en el punto de impacto
        Debug.DrawLine(transform.position, collision.contacts[0].point, Color.green, duracionLinea);
    }

    private void OnCollisionStay(Collision collision)
    {
        // Log directo (Cuidado: esto inundará la consola si hay muchos objetos)
        Debug.Log($"<color=yellow>[STAY SÓLIDO]</color> <b>{gameObject.name}</b> sigue tocando a <b>{collision.gameObject.name}</b>");

        // Línea amarilla constante al punto de contacto
        Debug.DrawLine(transform.position, collision.contacts[0].point, Color.yellow);
    }

    private void OnCollisionExit(Collision collision)
    {
        // Log directo
        Debug.Log($"<color=red>[EXIT SÓLIDO]</color> <b>{gameObject.name}</b> se separó de <b>{collision.gameObject.name}</b>");
    }

    // --- TRIGGERS (Zonas/Atravesables) ---

    private void OnTriggerEnter(Collider other)
    {
        // Log directo
        Debug.Log($"<color=blue>[ENTER TRIGGER]</color> <b>{gameObject.name}</b> entró en la zona de <b>{other.gameObject.name}</b>");

        // Línea azul al centro del trigger
        Debug.DrawLine(transform.position, other.transform.position, Color.blue, duracionLinea);
    }

    private void OnTriggerStay(Collider other)
    {
        // Log directo
        Debug.Log($"<color=cyan>[STAY TRIGGER]</color> <b>{gameObject.name}</b> dentro de <b>{other.gameObject.name}</b>");

        // Línea cian constante
        Debug.DrawLine(transform.position, other.transform.position, Color.cyan);
    }

    private void OnTriggerExit(Collider other)
    {
        // Log directo
        Debug.Log($"<color=magenta>[EXIT TRIGGER]</color> <b>{gameObject.name}</b> salió de la zona de <b>{other.gameObject.name}</b>");
    }

    // --- GIZMOS (Siempre visibles en la Scene) ---

    private void OnDrawGizmos()
    {
        // Dibujamos una esfera de alambre alrededor del objeto para saber que tiene el script
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radioEsfera);
    }
}