using UnityEngine;
using System.Collections;
using Game.Sensors;
using System;

public class Bala : MonoBehaviour, IDetectable
{
    public float damage;
    public GameObject dueno;
    public float velocidad = 20f;

    // Implementacion de IDetectable
    public string GetName() => dueno != null ? $"Bala de {dueno.name}" : "Bala";
    public DetectableType GetDetectableType() => DetectableType.Proyectil;
    public Transform GetTransform() => transform;

    [Header("Visuales")]
    public Sprite spriteInicio;
    public Sprite spriteDurante;
    public Sprite spriteExplosion;

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private BoxCollider2D col; // Usando 2D
    [SerializeField] private bool explotando;

    // Cache estático del CursorManager para no hacer FindObjectOfType en cada impacto (hot path)
    private static CursorManager _cursorCache;

    void OnEnable()
    {
        if (col == null) col = GetComponent<BoxCollider2D>();

        explotando = false;

        if (col != null)
        {
            col.enabled = true;
        }
        else
        {
            Debug.LogError($"La Bala en {gameObject.name} NO tiene un BoxCollider2D. Agregalo en el inspector.");
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

    [Header("Efectos y Sonidos Asignados")]
    public string vfxName;
    public string impactSoundName;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        ProcesarImpacto(collider.gameObject, transform.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 contactPoint = transform.position;
        if (collision.contacts.Length > 0)
        {
            contactPoint = collision.contacts[0].point;
        }
        ProcesarImpacto(collision.gameObject, contactPoint);
    }

    private void ProcesarImpacto(GameObject hitGo, Vector3 contactPoint)
    {
        if (explotando) return;

        // Si colisiona con el dueno, o con objetos de la misma capa del dueno, o con otra bala, ignorar
        if (dueno != null && (hitGo == dueno || hitGo.layer == dueno.layer || hitGo.CompareTag("Bala")))
        {
            return;
        }

        IDaniable objetivo = hitGo.GetComponent<IDaniable>();
        if (objetivo != null)
        {
            Debug.Log($"<color=cyan>[COLISION BALA]</color> Bala de <b>{dueno?.name}</b> impacto en <b>{hitGo.name}</b> causandole {(int)damage} de daño.");
            objetivo.RecibirDano((int)damage, dueno);

            // Parpadear cursor si golpea con exito al enemigo
            if (dueno != null && (dueno.CompareTag("Player") || dueno.name.Contains("Soldado")))
            {
                if (_cursorCache == null) _cursorCache = FindObjectOfType<CursorManager>();
                CursorManager cursor = _cursorCache;
                if (cursor != null)
                {
                    cursor.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                    CoroutineHelper.Instance.StartCoroutine(RestaurarEscalaCursor(cursor));
                }
            }
        }
        else
        {
            Debug.Log($"<color=orange>[COLISION EN ESCENARIO]</color> Bala de {dueno?.name} impacto contra objeto no danable: {hitGo.name}");
        }

        // Debug visual de choque
        Debug.DrawLine(transform.position, contactPoint, Color.red, 2f);

        // Spawn VFX específico o por defecto
        if (Manager_VFX.Instance != null)
        {
            Manager_VFX.Instance.SpawnVFX(vfxName, contactPoint);
        }

        // Reproducir sonido de impacto
        if (!string.IsNullOrEmpty(impactSoundName))
        {
            BD_Audios.ReproducirConSolapamiento(impactSoundName);
        }

        Explosion();
    }

    private System.Collections.IEnumerator RestaurarEscalaCursor(CursorManager cursor)
    {
        yield return new WaitForSeconds(0.08f);
        if (cursor != null) cursor.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }

    public void Explosion()
    {
        try
        {
            explotando = true;
            if (col != null) col.enabled = false;
            sr.sprite = spriteExplosion;
            CancelInvoke("Desactivar");
        }
        catch (Exception e)
        {
            Debug.Log("Problema al explotar bala " + e, gameObject);
        }
        finally
        {
            Desactivar();
        }
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
