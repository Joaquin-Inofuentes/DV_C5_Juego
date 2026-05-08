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
        if (explotando) return;
        if (dueño != null && (collision.gameObject.layer == dueño.layer || collision.gameObject.CompareTag("Bala"))) return;

        IDaniable objetivo = collision.gameObject.GetComponent<IDaniable>();
        if (objetivo != null)
        {
            // PASAMOS EL DUEÑO AQUÍ
            objetivo.RecibirDano((int)daño, dueño);
        }

        Explosion();
    }

    public void Explosion()
    {
        explotando = true;
        if (col != null) col.enabled = false;
        sr.sprite = spriteExplosion;
        CancelInvoke("Desactivar");
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
    // Ahora pedimos la cantidad y quién disparó
    void RecibirDano(int cantidad, GameObject atacante);
}