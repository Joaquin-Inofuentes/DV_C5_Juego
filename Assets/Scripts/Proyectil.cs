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
    public Vector3 PuntoDeOrigen;
    public void OnEnable()
    {
        PuntoDeOrigen = transform.position;
    }

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
            ContactPoint2D contacto = col.contacts[0];

            // 1. Calculamos la dirección desde el impacto hacia el PuntoDeOrigen
            Vector2 direccion = (Vector2)PuntoDeOrigen - contacto.point;

            // 2. Calculamos el ángulo en grados para el eje Z (2D)
            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

            // Nota: Si tu sprite por defecto mira hacia arriba (eje Y), resta 90 al ángulo:
            // float angulo = (Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg) - 90f;

            // 3. Creamos la rotación final en el eje Z
            Quaternion rotacionHaciaOrigen = Quaternion.Euler(0, 0, angulo);

            // 4. Instanciamos directamente con la rotación correcta
            GameObject bala = Instantiate(prefab, contacto.point, rotacionHaciaOrigen);

            bala.transform.localScale = Vector3.one;
        }
    }

    void DestruirBala()
    {
        gameObject.GetComponent<Collider2D>().enabled = false;
        //gameObject.transform.localScale = esBomba ? new Vector3(3, 3, 3) : new Vector3(2, 2, 2);
        Invoke("DesaparecerBala", 0.1f);
    }

    void DesaparecerBala() => Destroy(gameObject);
}