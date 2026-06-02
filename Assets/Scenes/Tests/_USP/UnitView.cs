using UnityEngine;
using System.Collections;

public class UnitView : MonoBehaviour
{
    public UnitModel model;
    public SpriteRenderer mainSprite;
    public GameObject selectionRing;

    [Header("UI")]
    public float barWidth = 60f;
    public Vector2 offset = new Vector2(0, 50);

    void Update()
    {
        if (selectionRing != null)
            selectionRing.SetActive(model.IsLeader);
    }

    public void TriggerFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        mainSprite.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        mainSprite.color = Color.white;
    }

    [Header("Colores Barra de Vida")]
    public float barHeight = 6f;
    private static readonly Color bgColor = new Color(0.1f, 0.6f, 0.1f, 1f);   // Verde fondo
    private static readonly Color fillColor = new Color(0.85f, 0.1f, 0.1f, 1f); // Rojo relleno
    private static readonly Color borderColor = new Color(0f, 0.4f, 0f, 1f);    // Verde borde oscuro

    private void OnGUI()
    {
        if (model == null || model.IsDead || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0) return;

        float x = screenPos.x - (barWidth / 2);
        float y = Screen.height - screenPos.y + offset.y;
        float border = 1f;

        // Borde verde oscuro
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(x - border, y - border, barWidth + border * 2, barHeight + border * 2), Texture2D.whiteTexture);

        // Fondo verde (estallido)
        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

        // Relleno rojo (vida actual)
        float healthPercent = model.healthActual / model.healthMax;
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, y, barWidth * healthPercent, barHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    // Dentro de UnitView.cs
    public void SetSelectionRing(bool isActive)
    {
        if (selectionRing != null)
        {
            selectionRing.SetActive(isActive);
        }
    }
}