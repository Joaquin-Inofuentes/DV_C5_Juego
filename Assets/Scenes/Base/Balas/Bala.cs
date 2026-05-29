using UnityEngine;
using System.Collections;

public class Bala : MonoBehaviour
{
    public float damage;
    public GameObject dueno;
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
        if (col == null) col = GetComponent<BoxCollider>();

        explotando = false;

        if (col != null)
        {
            col.enabled = true;
        }
        else
        {
            Debug.LogError($"La Bala en {gameObject.name} NO tiene un BoxCollider (3D). Agregalo en el inspector.");
        }

        sr.sprite = spriteInicio;
        Invoke("CambiarADurante", 0.05f);
        Invoke("Desactivar", 5f);
    }

    void Update()
    {
        if (!explotando)
        {
            transform.position += transform.right * velocidad * Time.deltaTime;
        }
    }

    void CambiarADurante() => sr.sprite = spriteDurante;

    private void OnCollisionEnter(Collision collision)
    {
        if (explotando) return;

        // Si colisiona con el dueno, o con objetos de la misma capa del dueno, o con otra bala, ignorar
        if (dueno != null && (collision.gameObject == dueno || collision.gameObject.layer == dueno.layer || collision.gameObject.CompareTag("Bala")))
        {
            return;
        }

        IDaniable objetivo = collision.gameObject.GetComponent<IDaniable>();
        if (objetivo != null)
        {
            Debug.Log($"<color=cyan>[COLISION BALA]</color> Bala de <b>{dueno?.name}</b> impacto en <b>{collision.gameObject.name}</b> causandole {(int)damage} de daño.");
            objetivo.RecibirDano((int)damage, dueno);

            // Parpadear cursor si golpea con exito al enemigo
            if (dueno != null && (dueno.CompareTag("Player") || dueno.name.Contains("Soldado")))
            {
                CursorManager cursor = FindObjectOfType<CursorManager>();
                if (cursor != null)
                {
                    cursor.transform.localScale = Vector3.one * 1.5f;
                    CoroutineHelper.Instance.StartCoroutine(RestaurarEscalaCursor(cursor));
                }
            }
        }
        else
        {
            Debug.Log($"<color=orange>[COLISION EN ESCENARIO]</color> Bala de {dueno?.name} impacto contra objeto no danable: {collision.gameObject.name}");
        }

        // Linea roja indicando punto exacto de choque/destello de impacto
        if (collision.contacts.Length > 0)
        {
            Debug.DrawLine(transform.position, collision.contacts[0].point, Color.red, 2f);
        }

        Explosion();
    }

    private IEnumerator RestaurarEscalaCursor(CursorManager cursor)
    {
        yield return new WaitForSeconds(0.08f);
        if (cursor != null) cursor.transform.localScale = Vector3.one;
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
    void RecibirDano(int cantidad, GameObject atacante);
}
