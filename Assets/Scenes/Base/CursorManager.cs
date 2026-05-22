using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorNormal;
    public Texture2D cursorInteractuar;

    void Update()
    {
        // 1. Detectar si el mouse está FUERA de la ventana del juego
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

        // Creamos la mascara: 1 desplazado 11 veces (Capa 11)
        int layerMask = 1 << 11;

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            // Solo entrará aquí si el objeto está en la Capa 11
            IInteractable interactuable = hit.collider.GetComponent<IInteractable>();
            if (interactuable != null)
            {
                CambiarCursor(cursorInteractuar);
                return;
            }
        }

        // 3. Si no detecta nada en la Capa 11, cursor normal
        CambiarCursor(cursorNormal);
    }

    void CambiarCursor(Texture2D tex)
    {
        if (tex == null) return;

        // Hotspot en el centro exacto de la textura
        Vector2 centroReal = new Vector2(tex.width / 2f, tex.height / 2f);

        Cursor.SetCursor(tex, centroReal, CursorMode.Auto);
    }
}