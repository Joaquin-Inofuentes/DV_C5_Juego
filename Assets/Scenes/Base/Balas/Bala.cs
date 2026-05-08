using UnityEngine;
using System.Collections;

public class Bala : MonoBehaviour
{
    public float daño;
    public GameObject dueño;
    public float velocidad = 20f;

    [Header("Visuales")]
    public Sprite spriteInicio;
    public Sprite spriteDurante;
    public Sprite spriteExplosion;
    public float tiempoExplosion = 0.5f;

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private BoxCollider col; // Usando 3D
    [SerializeField] private bool explotando;


    void OnEnable()
    {
        // SEGURIDAD: Si col sigue siendo null, intentamos buscarlo una última vez
        if (col == null) col = GetComponent<BoxCollider>();

        explotando = false;

        // Solo activamos si la referencia existe para evitar el UnassignedReferenceException
        if (col != null)
        {
            col.enabled = true;
        }
        else
        {
            Debug.LogError($"La Bala en {gameObject.name} NO tiene un BoxCollider (3D). Agrégalo en el inspector.");
        }

        sr.sprite = spriteInicio;
        Invoke("CambiarADurante", 0.05f);
        Invoke("Desactivar", 5f);
    }

    void Update()
    {
        if (!explotando)
        {
            // En 3D, transform.right funciona bien si el sprite mira a la derecha
            transform.position += transform.right * velocidad * Time.deltaTime;
        }
    }

    void CambiarADurante() => sr.sprite = spriteDurante;

    // Colisión 3D
    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject.name, collision.gameObject); // Log para ver qué colisiona
        //Debug.DrawLine(Vector3.zero, collision.contacts[0].point, Color.red, 1f); // Línea de depuración para ver colisiones
        if (explotando) return;
        if (collision.gameObject.layer == dueño.layer || collision.gameObject.CompareTag("Bala")) return;

        IDaniable objetivo = collision.gameObject.GetComponent<IDaniable>();
        if (objetivo != null)
        {
            objetivo.RecibirDano((int)daño);
        }

        StartCoroutine(Explosion());
    }

    IEnumerator Explosion()
    {
        explotando = true;
        if (col != null) col.enabled = false;
        sr.sprite = spriteExplosion;
        CancelInvoke("Desactivar");
        yield return new WaitForSeconds(tiempoExplosion);
        Desactivar();
    }

    void Desactivar()
    {
        CancelInvoke();
        if (BalaPool.Instance != null)
            BalaPool.Instance.ReturnBala(this);
        else
            gameObject.SetActive(false);
    }
}

public interface IDaniable
{
    void RecibirDano(int cantidad); // Interface
}