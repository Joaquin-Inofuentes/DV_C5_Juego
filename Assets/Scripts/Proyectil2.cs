// Clase que controla el proyectil disparado desde el tanque
// Se encarga de la trayectoria y la explosión al impactar
using UnityEngine;

public class Proyectil2 : MonoBehaviour
{
    public float velocidad = 20f;
    public float tiempoDeVida = 5f;
    public GameObject explosionPrefab;

    void Start()
    {
        Destroy(gameObject, tiempoDeVida); // El proyectil se destruye después de cierto tiempo
        
    }

    void FixedUpdate()
    {
        // Movimiento hacia adelante en el eje Z
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);

        // Mantener Z fijo en su valor inicial (en este caso, 1)
        Vector3 nuevaPosicion = transform.localPosition;
        nuevaPosicion.z = 1;  // Fija el valor de Z
        transform.localPosition = nuevaPosicion;
    }


    // Detectar colisión con otros objetos
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        // Crear la explosión en la posición de impacto
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(gameObject); // Destruir el proyectil
    }
}
