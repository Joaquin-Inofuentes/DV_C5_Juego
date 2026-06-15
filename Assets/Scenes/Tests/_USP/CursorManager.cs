using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorNormal;
    public Texture2D cursorInteractuar;
    public Texture2D cursorDisparar;
    public Texture2D cursorImpacto;

    public static CursorManager Instance { get; private set; }

    private float scaleBlinkTimer = 0f;
    private float _shootFeedbackTimer = 0f;
    private float _hitFeedbackTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    public void TriggerShootFeedback()
    {
        _shootFeedbackTimer = 0.15f;
    }

    public void TriggerHitFeedback()
    {
        _hitFeedbackTimer = 0.25f;
    }

    private Texture2D lastCursorTexture;
    private SpriteRenderer _sr;
    private UnityEngine.UI.Image _img;
    private Color _lastColor;

    private void SetVisualColor(Color c)
    {
        if (_sr == null && _img == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            _img = GetComponent<UnityEngine.UI.Image>();
            if (_img == null) _img = GetComponentInChildren<UnityEngine.UI.Image>();
        }

        if (c != _lastColor)
        {
            _lastColor = c;
            Debug.Log($"[UI_DEBUG] Color del cursor cambiado a: {c}");
        }

        if (_sr != null) _sr.color = c;
        if (_img != null) _img.color = c;
    }

    void Update()
    {
        // 1. Detectar si el mouse esta FUERA de la ventana del juego o gameplay
        Vector3 mPos = Input.mousePosition;
        bool estaFuera = mPos.x < 0 || mPos.y < 0 || mPos.x > Screen.width || mPos.y > Screen.height;

        if (estaFuera)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            transform.localScale = Vector3.zero;
            return;
        }

        Color targetColor = Color.white;

        // Si tenemos feedback de impacto, usar cursor de impacto
        if (_hitFeedbackTimer > 0f)
        {
            _hitFeedbackTimer -= Time.deltaTime;
            CambiarCursor(cursorImpacto != null ? cursorImpacto : cursorNormal);
            scaleBlinkTimer = 0.3f;
            targetColor = Color.red;
            SetVisualColor(targetColor);
            return;
        }

        // Si tenemos feedback de disparo, usar cursor de disparo
        if (_shootFeedbackTimer > 0f)
        {
            _shootFeedbackTimer -= Time.deltaTime;
            CambiarCursor(cursorDisparar != null ? cursorDisparar : cursorNormal);
            targetColor = Color.yellow;
            SetVisualColor(targetColor);
            return;
        }

        // 2. Configurar Raycast para ignorar todo excepto Capa 11
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        int layerMask = 1 << 11;

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            IInteractable interactuable = hit.collider.GetComponent<IInteractable>();
            if (interactuable != null)
            {
                CambiarCursor(cursorInteractuar);
                SetVisualColor(targetColor);

                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    scaleBlinkTimer = 0.5f;
                }
                return;
            }
        }

        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.CompareTag("Enemy") || hit.collider.name.Contains("Enemigo") || hit.collider.GetComponent<IDaniable>() != null)
            {
                scaleBlinkTimer = 0.5f;
            }
        }

        CambiarCursor(cursorNormal);
        SetVisualColor(targetColor);
    }

    private void FixedUpdate()
    {
        Vector3 mPos = Input.mousePosition;
        bool estaFuera = mPos.x < 0 || mPos.y < 0 || mPos.x > Screen.width || mPos.y > Screen.height;
        if (estaFuera)
        {
            transform.localScale = Vector3.zero;
            return;
        }

        if (scaleBlinkTimer > 0f)
        {
            scaleBlinkTimer -= Time.fixedDeltaTime;
            float scaleFactor = 0.5f + Mathf.Abs(Mathf.Sin(scaleBlinkTimer * 20f)) * 0.2f;
            transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
        else
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
    }

    void CambiarCursor(Texture2D tex)
    {
        if (tex == null) return;
        if (tex != lastCursorTexture)
        {
            lastCursorTexture = tex;
            Debug.Log($"[UI_DEBUG] Textura de cursor cambiada a: {tex.name}");
        }
        Vector2 centroReal = new Vector2(tex.width / 2f, tex.height / 2f);
        Cursor.SetCursor(tex, centroReal, CursorMode.Auto);
    }
}
