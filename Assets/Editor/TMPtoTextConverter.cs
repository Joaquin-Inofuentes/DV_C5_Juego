using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.Reflection;

public class TMPtoTextConverter : EditorWindow
{
    [MenuItem("Tools/Convert TMP to Legacy Text if Unreferenced")]
    public static void Convert()
    {
        int convertedCount = 0;

        // Fuente legacy estándar actualizada
        Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var tmps = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);

        foreach (var tmp in tmps)
        {
            if (!IsReferenced(tmp))
            {
                string contenido = tmp.text;
                Color color = tmp.color;
                int fontSize = Mathf.RoundToInt(tmp.fontSize);
                TextAlignmentOptions alignTMP = tmp.alignment;

                // Crear nuevo GameObject hijo
                GameObject goText = new GameObject("LegacyText");
                goText.transform.SetParent(tmp.transform.parent, false);

                // Posicionar igual que el TMP
                var rectOld = tmp.rectTransform;
                var rectNew = goText.AddComponent<RectTransform>();
                rectNew.sizeDelta = rectOld.sizeDelta;
                rectNew.anchorMin = rectOld.anchorMin;
                rectNew.anchorMax = rectOld.anchorMax;
                rectNew.pivot = rectOld.pivot;
                rectNew.localPosition = rectOld.localPosition;
                rectNew.localRotation = rectOld.localRotation;
                rectNew.localScale = rectOld.localScale;

                // Agregar Text clásico
                var textUI = goText.AddComponent<Text>();
                
                textUI.text = contenido;
                textUI.color = color;
                textUI.fontSize = fontSize;
                textUI.font = legacyFont;
                textUI.alignment = ConvertAlignment(alignTMP);

                // Eliminar TMP original
                DestroyImmediate(tmp.gameObject);
            }
        }


        Debug.Log($"✅ Conversión terminada. {convertedCount} elementos reemplazados por Text legacy.");
    }

    private static bool IsReferenced(TextMeshProUGUI tmp)
    {
        var allComponents = GameObject.FindObjectsOfType<Component>(true);
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (typeof(Object).IsAssignableFrom(field.FieldType))
                {
                    var value = field.GetValue(comp) as Object;
                    if (value == tmp)
                        return true;
                }
            }
        }
        return false;
    }

    private static TextAnchor ConvertAlignment(TextAlignmentOptions tmpAlign)
    {
        switch (tmpAlign)
        {
            case TextAlignmentOptions.TopLeft: return TextAnchor.UpperLeft;
            case TextAlignmentOptions.Top: return TextAnchor.UpperCenter;
            case TextAlignmentOptions.TopRight: return TextAnchor.UpperRight;
            case TextAlignmentOptions.Left: return TextAnchor.MiddleLeft;
            case TextAlignmentOptions.Center: return TextAnchor.MiddleCenter;
            case TextAlignmentOptions.Right: return TextAnchor.MiddleRight;
            case TextAlignmentOptions.BottomLeft: return TextAnchor.LowerLeft;
            case TextAlignmentOptions.Bottom: return TextAnchor.LowerCenter;
            case TextAlignmentOptions.BottomRight: return TextAnchor.LowerRight;
            default: return TextAnchor.MiddleCenter;
        }
    }
}
