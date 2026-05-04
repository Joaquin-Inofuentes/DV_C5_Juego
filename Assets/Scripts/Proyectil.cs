using UnityEngine;

public class Proyectil : MonoBehaviour
{
    public float velocidadInicial = 20f;
    public float dano;
    private Rigidbody2D rb2D;
    public bool esBomba;
    public float tiempoVida;

    [Header("Efectos de Impacto")]
    public GameObject efectoSangrePrefab;   // Asignar prefab de sangre
    public GameObject efectoImpactoPrefab;  // Asignar prefab de chispas/polvo

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.velocity = transform.right * velocidadInicial;
        }
        Invoke("DestruirBala", tiempoVida);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Determinar qué efecto spawnear según el Tag
        if (collision.gameObject.CompareTag("Jugadores") || collision.gameObject.CompareTag("Soldado_Enemigo"))
        {
            SpawnearEfecto(efectoSangrePrefab, collision);

            // Lógica de daño
            InformacionPersonaje info = collision.gameObject.GetComponent<InformacionPersonaje>();
            if (info != null) info.RecibirDanio(dano);

            Enemigo enemigo = collision.gameObject.GetComponent<Enemigo>();
            if (enemigo != null) enemigo.RecibirDanio(dano);

            DestruirBala();
        }
        else if (collision.gameObject.CompareTag("Obstaculo"))
        {
            SpawnearEfecto(efectoImpactoPrefab, collision);
            DestruirBala();
        }
    }

    void SpawnearEfecto(GameObject prefab, Collision2D col)
    {
        if (prefab != null)
        {
            // Creamos el efecto en el punto de contacto
            ContactPoint2D contacto = col.contacts[0];
            Instantiate(prefab, contacto.point, Quaternion.identity);
        }
    }

    void DestruirBala()
    {
        gameObject.GetComponent<Collider2D>().enabled = false;
        gameObject.transform.localScale = esBomba ? new Vector3(3, 3, 3) : new Vector3(2, 2, 2);
        Invoke("DesaparecerBala", 0.1f);
    }

    void DesaparecerBala() => Destroy(gameObject);
}