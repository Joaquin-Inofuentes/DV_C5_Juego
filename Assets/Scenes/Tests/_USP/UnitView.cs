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

    private void OnGUI()
    {
        if (model.IsDead) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0) return;

        float x = screenPos.x - (barWidth / 2);
        float y = Screen.height - screenPos.y + offset.y;

        // Color según equipo
        Color teamColor = (model.team == Game.Core.UnitTeam.PlayerTeam) ? Color.green : Color.red;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x, y, barWidth, 6), Texture2D.whiteTexture);

        GUI.color = teamColor;
        float healthPercent = model.healthActual / model.healthMax;
        GUI.DrawTexture(new Rect(x, y, barWidth * healthPercent, 6), Texture2D.whiteTexture);
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