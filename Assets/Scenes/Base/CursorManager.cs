using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorNormal;
    public Texture2D cursorInteractuar;

    private float scaleBlinkTimer = 0f;

    void Update()
    {
        // 1. Detectar si el mouse esta FUERA de la ventana del juego
        Vector3 mPos = Input.mousePosition;
        bool estaFuera = mPos.x < 0 || mPos.y < 0 || mPos.x > Screen.width || mPos.y > Screen.height;

        if (estaFuera)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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
    }

    private void FixedUpdate()
    {
        if (scaleBlinkTimer > 0f)
        {
            scaleBlinkTimer -= Time.fixedDeltaTime;
            float scaleFactor = 1f + Mathf.Abs(Mathf.Sin(scaleBlinkTimer * 20f)) * 0.4f;
            transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }

    void CambiarCursor(Texture2D tex)
    {
        if (tex == null) return;
        Vector2 centroReal = new Vector2(tex.width / 2f, tex.height / 2f);
        Cursor.SetCursor(tex, centroReal, CursorMode.Auto);
    }
}
