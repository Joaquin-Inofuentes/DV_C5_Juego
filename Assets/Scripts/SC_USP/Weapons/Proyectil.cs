using USP.Services;
using USP.Core;
using USP.Entities;
namespace USP.Weapons {
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

    [Header("Referencias de Equipo")]
    [Tooltip("El dueño de este proyectil.")]
    public GameObject owner;

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Debug básico ante cualquier colisión
        Debug.Log($"<color=white>[COLISION PROYECTIL]</color> Proyectil de <b>{owner?.name}</b> chocó contra <b>{collision.gameObject.name}</b>");

        // Si choca con el dueño, o con otro proyectil del mismo dueño, ignorar
        if (owner != null && (collision.gameObject == owner || collision.gameObject.layer == owner.layer))
        {
            return;
        }

        // Determinar si es rival u aliado respecto al origen
        bool esRival = false;
        if (owner != null)
        {
            // Comparar capas o tags
            if (owner.CompareTag("Player") || owner.name.Contains("Soldado"))
            {
                if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.name.Contains("Enemigo"))
                {
                    esRival = true;
                }
            }
            else if (owner.CompareTag("Enemy") || owner.name.Contains("Enemigo"))
            {
                if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Soldado"))
                {
                    esRival = true;
                }
            }
        }

        // Escribir log con color según rivalidad
        string rivalColor = esRival ? "red" : "green";
        string rivalText = esRival ? "¡RIVAL!" : "AMISTOSO / NEUTRAL";
        Debug.Log($"<color={rivalColor}>[ALINEACION: {rivalText}]</color> Proyectil de <b>{owner?.name}</b> impactó en <b>{collision.gameObject.name}</b>");

        Vector3 contactPoint = transform.position;
        if (collision.contacts.Length > 0)
        {
            contactPoint = collision.contacts[0].point;
        }

        // Spawn a random VFX from Manager_VFX
        if (Manager_VFX.Instance != null && Manager_VFX.Instance.vfxPrefabs != null && Manager_VFX.Instance.vfxPrefabs.Count > 0)
        {
            int rnd = Random.Range(0, Manager_VFX.Instance.vfxPrefabs.Count);
            string fxName = Manager_VFX.Instance.vfxPrefabs[rnd].name;
            Manager_VFX.Instance.SpawnVFX(fxName, contactPoint);
        }

        // 1. Determinar qué efecto spawnear según el Tag
        if (collision.gameObject.CompareTag("Jugadores") || collision.gameObject.CompareTag("Soldado_Enemigo") || collision.gameObject.GetComponent<IDaniable>() != null)
        {
            // Lógica de daño
            InformacionPersonaje info = collision.gameObject.GetComponent<InformacionPersonaje>();
            if (info != null) info.RecibirDanio(dano);

            Enemigo enemigo = collision.gameObject.GetComponent<Enemigo>();
            if (enemigo != null) enemigo.RecibirDanio(dano);

            IDaniable objetivo = collision.gameObject.GetComponent<IDaniable>();
            if (objetivo != null) objetivo.RecibirDano((int)dano, owner);

            DestruirBala();
        }
        else if (collision.gameObject.CompareTag("Obstaculo") || collision.gameObject.layer == 0) // Default / Obstáculo
        {
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
}
