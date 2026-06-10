using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Squad;

public class UnitView : MonoBehaviour
{
    public UnitModel model;
    public SpriteRenderer mainSprite;
    public GameObject selectionRing;

    [Header("Rotación Gráfica")]
    [Tooltip("Transform que rotará visualmente. El root nunca rota.")]
    public Transform graphicsRoot;

    [Header("LineRenderer")]
    public LineRenderer lineRenderer;

    [Header("Indicadores de Estado")]
    public IndicatorEntry healIndicator = new IndicatorEntry { name = "Heal", onTime = 0.15f, offTime = 0.15f };
    public IndicatorEntry combatIndicator = new IndicatorEntry { name = "Combat", onTime = 0.12f, offTime = 0.12f };
    public IndicatorEntry movingIndicator = new IndicatorEntry { name = "Moving", onTime = 0.3f, offTime = 0.3f };

    [Header("UI")]
    public float barWidth = 60f;
    public Vector2 offset = new Vector2(0, 50);

    private Dictionary<IndicatorType, Coroutine> _activeBlinkRoutines = new Dictionary<IndicatorType, Coroutine>();

    void Awake()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        HideAllIndicators();
    }

    void Update()
    {
        if (selectionRing != null)
            selectionRing.SetActive(model != null && model.IsLeader);
    }

    // === ROTACIÓN GRÁFICA ===

    public void RotateGraphics(float angle)
    {
        if (graphicsRoot != null)
            graphicsRoot.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void RotateGraphicsSmooth(float angle, float speed)
    {
        if (graphicsRoot != null)
        {
            Quaternion target = Quaternion.Euler(0, 0, angle);
            graphicsRoot.rotation = Quaternion.Slerp(graphicsRoot.rotation, target, speed * Time.deltaTime);
        }
    }

    // === LINE RENDERER ===

    public void ShowLineToTarget(Vector3 from, Vector3 targetPos)
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, targetPos);
    }

    public void ShowLineToDestination(Vector3 from, Vector3 destination)
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, destination);
    }

    public void ShowLinePath(List<Vector3> path, Color color)
    {
        if (lineRenderer == null || path == null || path.Count < 2) { HideLine(); return; }
        lineRenderer.enabled = true;
        lineRenderer.positionCount = path.Count;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        for (int i = 0; i < path.Count; i++)
            lineRenderer.SetPosition(i, path[i]);
    }

    public void HideLine()
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    // === INDICADORES DE TITILEO ===

    public void StartBlink(IndicatorType type)
    {
        StopBlink(type);
        IndicatorEntry entry = GetEntry(type);
        if (entry == null || entry.indicator == null) return;
        Coroutine c = StartCoroutine(BlinkRoutine(entry));
        _activeBlinkRoutines[type] = c;
    }

    public void StopBlink(IndicatorType type)
    {
        if (_activeBlinkRoutines.TryGetValue(type, out Coroutine c) && c != null)
            StopCoroutine(c);
        _activeBlinkRoutines.Remove(type);

        IndicatorEntry entry = GetEntry(type);
        if (entry != null && entry.indicator != null)
            entry.indicator.SetActive(false);
    }

    public void StopAllBlinks()
    {
        StopBlink(IndicatorType.Heal);
        StopBlink(IndicatorType.Combat);
        StopBlink(IndicatorType.Moving);
    }

    IEnumerator BlinkRoutine(IndicatorEntry entry)
    {
        float elapsed = 0f;
        while (entry.duration < 0 || elapsed < entry.duration)
        {
            entry.indicator.SetActive(true);
            yield return new WaitForSeconds(entry.onTime);
            elapsed += entry.onTime;

            entry.indicator.SetActive(false);
            yield return new WaitForSeconds(entry.offTime);
            elapsed += entry.offTime;
        }
        entry.indicator.SetActive(false);
    }

    IndicatorEntry GetEntry(IndicatorType type)
    {
        switch (type)
        {
            case IndicatorType.Heal: return healIndicator;
            case IndicatorType.Combat: return combatIndicator;
            case IndicatorType.Moving: return movingIndicator;
            default: return null;
        }
    }

    void HideAllIndicators()
    {
        if (healIndicator.indicator != null) healIndicator.indicator.SetActive(false);
        if (combatIndicator.indicator != null) combatIndicator.indicator.SetActive(false);
        if (movingIndicator.indicator != null) movingIndicator.indicator.SetActive(false);
    }

    // === FLASH DE DAÑO ===

    public void TriggerFlash()
    {
        StopAllCoroutines();
        _activeBlinkRoutines.Clear();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        mainSprite.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        mainSprite.color = Color.white;
    }

    // === BARRA DE VIDA ===

    [Header("Colores Barra de Vida")]
    public float barHeight = 6f;
    public Color bgColor = new Color(0.1f, 0.6f, 0.1f, 1f);
    public Color fillColor = new Color(0.85f, 0.1f, 0.1f, 1f);
    public Color borderColor = new Color(0f, 0.4f, 0f, 1f);

    private void OnGUI()
    {
        if (model == null || model.IsDead || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0) return;

        float x = screenPos.x - (barWidth / 2);
        float y = Screen.height - screenPos.y + offset.y;
        float border = 1f;

        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(x - border, y - border, barWidth + border * 2, barHeight + border * 2), Texture2D.whiteTexture);

        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

        float healthPercent = model.healthActual / model.healthMax;
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, y, barWidth * healthPercent, barHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    public void SetSelectionRing(bool isActive)
    {
        if (selectionRing != null)
            selectionRing.SetActive(isActive);
    }
}
