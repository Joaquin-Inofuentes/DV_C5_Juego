using UnityEngine;
using System.Collections;

public class Bala : MonoBehaviour
{
    public float daño;
    public GameObject dueño;
    public float velocidad = 20f; // Aumentamos velocidad base

    [Header("Visuales")]
    public Sprite spriteInicio;
    public Sprite spriteDurante;
    public Sprite spriteExplosion;
    public float tiempoExplosion = 0.5f;

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private bool explotando;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    void OnEnable()
    {
        explotando = false;
        col.enabled = true;
        sr.sprite = spriteInicio;
        Invoke("CambiarADurante", 0.05f);
        Invoke("Desactivar", 5f);
    }

    void Update()
    {
        if (!explotando)
        {
            // Movimiento constante tipo "impulso" sin física
            transform.position += transform.right * velocidad * Time.deltaTime;
        }
    }

    void CambiarADurante() => sr.sprite = spriteDurante;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Ignorar dueño o balas
        if (other.gameObject == dueño || other.CompareTag("Bala")) return;

        // 2. Intentar dañar si el objeto tiene la interfaz
        IDaniable objetivo = other.GetComponent<IDaniable>();
        if (objetivo != null)
        {
            objetivo.RecibirDano((int)daño);
            Debug.Log($"<color=red>Daño aplicado a:</color> {other.name}");
        }
        else
        {
            return;
        }

        // 3. Debug de colisión y explosión
        Debug.Log($"<color=yellow>Colisionó con:</color> {other.name} (Tag: {other.tag})");
        StartCoroutine(Explosion());
    }

    IEnumerator Explosion()
    {
        explotando = true;
        col.enabled = false;
        sr.sprite = spriteExplosion;
        CancelInvoke("Desactivar");
        yield return new WaitForSeconds(tiempoExplosion);
        Desactivar();
    }

    void Desactivar()
    {
        CancelInvoke();
        BalaPool.Instance.ReturnBala(this);
    }
}

public interface IDaniable
{
    void RecibirDano(int cantidad); // Interface
}