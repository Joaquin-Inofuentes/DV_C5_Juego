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

    // Estado de revivimiento — se actualiza desde UnitController_Revival y RevivingState
    [HideInInspector] public float revivalProgress = 0f; // 0=sin revivir, 1=completo
    private float _revivalCompleteTimer = 0f;
    private float _healTimer = 0f;
    private static Texture2D _circleTexture;

    // Burbujas de diálogo
    private string _speechText = "";
    private float _speechTimer = 0f;
    private float _speechDuration = 3f;
    private GUIStyle _bubbleStyle;

    [Header("Indicadores de Estado")]
    public IndicatorEntry healIndicator = new IndicatorEntry { name = "Heal", onTime = 0.15f, offTime = 0.15f };
    public IndicatorEntry combatIndicator = new IndicatorEntry { name = "Combat", onTime = 0.12f, offTime = 0.12f };
    public IndicatorEntry movingIndicator = new IndicatorEntry { name = "Moving", onTime = 0.3f, offTime = 0.3f };
    public IndicatorEntry revivingIndicator = new IndicatorEntry { name = "Reviving", onTime = 0.25f, offTime = 0.25f };

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
        if (_revivalCompleteTimer > 0f) _revivalCompleteTimer -= Time.deltaTime;
        if (_healTimer > 0f) _healTimer -= Time.deltaTime;
        if (_speechTimer > 0f) _speechTimer -= Time.deltaTime;
        if (selectionRing != null)
            selectionRing.SetActive(model != null && model.IsLeader);
    }

    public void TriggerHealEffect() => _healTimer = 0.9f;

    public void ShowSpeech(string msg, float duration = 3f)
    {
        _speechText = msg;
        _speechDuration = Mathf.Max(duration, 0.1f);
        _speechTimer = _speechDuration;
    }

    /// <summary>Llamado cuando el revivimiento se completa — dispara animación de exhalación.</summary>
    public void OnRevivalComplete()
    {
        _revivalCompleteTimer = 1.4f;
        revivalProgress = 0f;
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
        StopBlink(IndicatorType.Reviving);
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
            case IndicatorType.Reviving: return revivingIndicator;
            default: return null;
        }
    }

    void HideAllIndicators()
    {
        if (healIndicator.indicator != null) healIndicator.indicator.SetActive(false);
        if (combatIndicator.indicator != null) combatIndicator.indicator.SetActive(false);
        if (movingIndicator.indicator != null) movingIndicator.indicator.SetActive(false);
        if (revivingIndicator.indicator != null) revivingIndicator.indicator.SetActive(false);
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
        if (model == null || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0) return;

        if (model.IsDown)
        {
            DrawHeartbeatCircle(screenPos);
            if (revivalProgress > 0.02f) DrawRevivalBar(screenPos);
        }
        else if (_revivalCompleteTimer > 0f)
        {
            DrawExhaleCircle(screenPos);
            DrawHealthBar(screenPos);
        }
        else
        {
            DrawHealthBar(screenPos);
            if (_healTimer > 0f) DrawHealCircle(screenPos);
        }

        DrawSpecLabel(screenPos);
        DrawSpeechBubble(screenPos);
    }

    private void DrawHealthBar(Vector3 screenPos)
    {
        float x = screenPos.x - (barWidth / 2);
        float y = Screen.height - screenPos.y + offset.y;
        float border = 1f;

        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(x - border, y - border, barWidth + border * 2, barHeight + border * 2), Texture2D.whiteTexture);

        GUI.color = new Color(0f, 0f, 0f, 1f); // fondo negro
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

        float healthPercent = model.healthActual / model.healthMax;
        GUI.color = fillColor; // rojo
        GUI.DrawTexture(new Rect(x, y, barWidth * healthPercent, barHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void DrawRevivalBar(Vector3 screenPos)
    {
        float w = barWidth * 2f;
        float h = barHeight * 1.5f;
        float x = screenPos.x - w * 0.5f;
        float y = Screen.height - screenPos.y + offset.y + barHeight + 3f;
        float border = 1f;

        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(x - border, y - border, w + border * 2, h + border * 2), Texture2D.whiteTexture);
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.yellow;
        GUI.DrawTexture(new Rect(x, y, w * revivalProgress, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawHealCircle(Vector3 screenPos)
    {
        float t = _healTimer / 0.9f; // 1→0
        float size = barWidth * (1.0f + (1f - t) * 1.2f);
        float alpha = t * 0.8f;
        float guiY = Screen.height - screenPos.y;
        GUI.color = new Color(0.2f, 1f, 0.3f, alpha);
        GUI.DrawTexture(new Rect(screenPos.x - size * 0.5f, guiY - size * 0.5f, size, size), GetCircleTexture());
        GUI.color = Color.white;
    }

    private GUIStyle _specStyle;

    private void DrawSpecLabel(Vector3 screenPos)
    {
        if (model == null) return;
        string label;
        Color labelColor;
        switch (model.specialization)
        {
            case UnitSpecialization.Flancotirador: label = "FLANC"; labelColor = new Color(0.4f, 0.8f, 1f); break;
            case UnitSpecialization.Apoyo:         label = "APOYO"; labelColor = new Color(1f, 0.8f, 0.2f); break;
            case UnitSpecialization.Medico:        label = "MED";   labelColor = new Color(0.3f, 1f, 0.4f); break;
            default: return;
        }

        if (_specStyle == null)
        {
            _specStyle = new GUIStyle(GUI.skin.label);
            _specStyle.fontSize = 10;
            _specStyle.fontStyle = FontStyle.Bold;
            _specStyle.alignment = TextAnchor.MiddleCenter;
        }

        float w = 60f;
        float guiY = Screen.height - screenPos.y + offset.y + barHeight + 3f;
        _specStyle.normal.textColor = labelColor;
        GUI.Label(new Rect(screenPos.x - w * 0.5f, guiY, w, 14f), label, _specStyle);
    }

    private void DrawHeartbeatCircle(Vector3 screenPos)
    {
        float t = Time.time;
        float baseSize = barWidth * 2.2f;

        float pulse;
        if (revivalProgress > 0.02f)
        {
            // Siendo revivido: calmar progresivamente
            float stress = 1f - revivalProgress;
            float bpm = Mathf.Lerp(1.0f, 2.4f, stress);
            float phase = (t * bpm) % 1f;
            float lub   = Mathf.Exp(-Mathf.Pow((phase - 0.08f) / 0.06f, 2f));
            float dub   = 0.5f * Mathf.Exp(-Mathf.Pow((phase - 0.22f) / 0.07f, 2f));
            float erratic = Mathf.Clamp01(lub + dub);
            float breathing = Mathf.Sin(t * Mathf.PI * 1.1f) * 0.4f + 0.5f;
            pulse = Mathf.Lerp(breathing, erratic, stress);
        }
        else
        {
            // Caído sin ser revivido: latido estresado rápido
            float bpm = 2.4f + Mathf.Sin(t * 0.7f) * 0.25f;
            float phase = (t * bpm) % 1f;
            float lub   = Mathf.Exp(-Mathf.Pow((phase - 0.08f) / 0.055f, 2f));
            float dub   = 0.45f * Mathf.Exp(-Mathf.Pow((phase - 0.21f) / 0.065f, 2f));
            float echo  = 0.2f * Mathf.Exp(-Mathf.Pow((phase - 0.38f) / 0.05f, 2f));
            pulse = Mathf.Clamp01(lub + dub + echo);
        }

        float size = baseSize * (0.7f + pulse * 0.7f);
        float guiY = Screen.height - screenPos.y;

        GUI.color = new Color(0.9f, 0.08f, 0.08f, 0.88f);
        GUI.DrawTexture(new Rect(screenPos.x - size * 0.5f, guiY - size * 0.5f, size, size), GetCircleTexture());
        GUI.color = Color.white;
    }

    private void DrawExhaleCircle(Vector3 screenPos)
    {
        // t va de 1 a 0 durante la animación
        float t = _revivalCompleteTimer / 1.4f;
        // Sube rápido y baja suave (inhalar y exhalar)
        float inflate = Mathf.Sin(t * Mathf.PI);
        float size = barWidth * (0.8f + inflate * 0.6f);
        float alpha = t * 0.75f;
        float guiY = Screen.height - screenPos.y;

        GUI.color = new Color(0.2f, 0.9f, 0.3f, alpha);
        GUI.DrawTexture(new Rect(screenPos.x - size * 0.5f, guiY - size * 0.5f, size, size), GetCircleTexture());
        GUI.color = Color.white;
    }

    private static Texture2D GetCircleTexture()
    {
        if (_circleTexture != null) return _circleTexture;
        const int sz = 64;
        _circleTexture = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float c = (sz - 1) * 0.5f;
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(1f - (dist - (c - 1.5f)));
                _circleTexture.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        _circleTexture.Apply();
        return _circleTexture;
    }

    private void DrawSpeechBubble(Vector3 screenPos)
    {
        if (_speechTimer <= 0f || string.IsNullOrEmpty(_speechText)) return;

        float lifeRatio = _speechTimer / _speechDuration;
        float alpha = Mathf.Sin(lifeRatio * Mathf.PI); // fade suave in/out

        if (_bubbleStyle == null)
        {
            _bubbleStyle = new GUIStyle(GUI.skin.label);
            _bubbleStyle.fontSize = 11;
            _bubbleStyle.fontStyle = FontStyle.Bold;
            _bubbleStyle.alignment = TextAnchor.MiddleCenter;
            _bubbleStyle.wordWrap = false;
        }

        float bubbleW = 130f;
        float bubbleH = 26f;
        float bx = screenPos.x - bubbleW * 0.5f;
        float by = Screen.height - screenPos.y - 70f - bubbleH;

        // Borde oscuro exterior
        GUI.color = new Color(0f, 0f, 0f, 0.9f * alpha);
        GUI.DrawTexture(new Rect(bx - 2, by - 2, bubbleW + 4, bubbleH + 4), Texture2D.whiteTexture);

        // Fondo semitransparente
        GUI.color = new Color(0.12f, 0.12f, 0.18f, 0.92f * alpha);
        GUI.DrawTexture(new Rect(bx, by, bubbleW, bubbleH), Texture2D.whiteTexture);

        _bubbleStyle.normal.textColor = new Color(1f, 0.95f, 0.8f, alpha);
        GUI.Label(new Rect(bx, by, bubbleW, bubbleH), _speechText, _bubbleStyle);

        GUI.color = Color.white;
    }

    public void SetSelectionRing(bool isActive)
    {
        if (selectionRing != null)
            selectionRing.SetActive(isActive);
    }
}
