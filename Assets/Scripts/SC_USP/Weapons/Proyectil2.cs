namespace USP.Weapons {
using UnityEngine;

public class Proyectil2 : MonoBehaviour
{
    public float velocidad = 20f;
    public float tiempoDeVida = 5f;
    public GameObject explosionPrefab;

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    void FixedUpdate()
    {
        transform.Translate(Vector3.right * velocidad * Time.fixedDeltaTime);

        Vector3 nuevaPosicion = transform.localPosition;
        nuevaPosicion.z = 0;
        transform.localPosition = nuevaPosicion;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.gameObject.name);
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }
}
}
