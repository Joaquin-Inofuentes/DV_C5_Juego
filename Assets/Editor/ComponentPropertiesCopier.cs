using System.Text;
using UnityEditor;
using UnityEngine;

public static class ComponentPropertiesCopier
{
    [MenuItem("CONTEXT/Component/Copy Properties to Clipboard", false, 150)]
    public static void CopyComponentProperties(MenuCommand command)
    {
        Component component = command.context as Component;
        if (component == null) return;

        string result = FormatComponentProperties(component);
        GUIUtility.systemCopyBuffer = result;
        Debug.Log($"Copied properties of component '{component.GetType().Name}' on '{component.gameObject.name}' to clipboard!");
    }

    [MenuItem("CONTEXT/Component/Copy All Components' Properties", false, 151)]
    public static void CopyAllComponentsPropertiesFromComponent(MenuCommand command)
    {
        Component component = command.context as Component;
        if (component == null) return;
        CopyGameObjectPropertiesInternal(component.gameObject);
    }

    [MenuItem("CONTEXT/GameObject/Copy All Components' Properties", false, 150)]
    public static void CopyGameObjectPropertiesFromGameObjectContext(MenuCommand command)
    {
        GameObject go = command.context as GameObject;
        if (go == null) return;
        CopyGameObjectPropertiesInternal(go);
    }

    [MenuItem("GameObject/Copy All Components' Properties", false, 30)]
    public static void CopyGameObjectProperties(MenuCommand command)
    {
        GameObject go = command.context as GameObject;
        if (go == null)
        {
            go = Selection.activeGameObject;
        }
        if (go == null) return;

        CopyGameObjectPropertiesInternal(go);
    }

    [MenuItem("CONTEXT/Component/Paste Properties from Clipboard", false, 200)]
    public static void PastePropertiesFromComponentContext(MenuCommand command)
    {
        Component component = command.context as Component;
        if (component == null) return;
        PastePropertiesFromClipboard(component.gameObject);
    }

    [MenuItem("CONTEXT/GameObject/Paste Properties from Clipboard", false, 200)]
    public static void PastePropertiesFromGameObjectContext(MenuCommand command)
    {
        GameObject go = command.context as GameObject;
        if (go == null) return;
        PastePropertiesFromClipboard(go);
    }

    [MenuItem("GameObject/Paste Properties from Clipboard", false, 31)]
    public static void PastePropertiesFromGameObjectMenu(MenuCommand command)
    {
        GameObject go = command.context as GameObject;
        if (go == null)
        {
            go = Selection.activeGameObject;
        }
        if (go == null) return;

        PastePropertiesFromClipboard(go);
    }

    private static void CopyGameObjectPropertiesInternal(GameObject go)
    {
        string result = FormatGameObjectProperties(go);
        GUIUtility.systemCopyBuffer = result;
        Debug.Log($"Copied properties of GameObject '{go.name}' and its components to clipboard!");
    }

    public static string FormatGameObjectProperties(GameObject go)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"GameObject: {go.name}");
        sb.AppendLine($"Tag: {go.tag}");
        sb.AppendLine($"Layer: {LayerMask.LayerToName(go.layer)}");
        sb.AppendLine($"Active: {go.activeSelf}");
        sb.AppendLine("==================================");

        Component[] components = go.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component == null)
            {
                sb.AppendLine("Component: Missing/Null Script");
                sb.AppendLine("----------------------------------");
                continue;
            }
            sb.AppendLine(FormatComponentProperties(component));
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public static string FormatComponentProperties(Component component)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== Component: {component.GetType().FullName} ===");

        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.GetIterator();
        
        bool enterChildren = true;
        if (prop.NextVisible(enterChildren))
        {
            do
            {
                if (prop.propertyPath == "m_Script") continue;

                enterChildren = ShouldEnterChildren(prop);

                if (prop.propertyType == SerializedPropertyType.Generic && !prop.isArray)
                {
                    // Skip root generic headers to let their children write value
                }
                else
                {
                    string valueStr = GetPropertyValueString(prop);
                    sb.AppendLine($"{prop.propertyPath} = {valueStr}");
                }
            } while (prop.NextVisible(enterChildren));
        }

        return sb.ToString().TrimEnd();
    }

    private static bool ShouldEnterChildren(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Generic:
                return true;
            case SerializedPropertyType.Vector2:
            case SerializedPropertyType.Vector3:
            case SerializedPropertyType.Vector4:
            case SerializedPropertyType.Rect:
            case SerializedPropertyType.Bounds:
            case SerializedPropertyType.Quaternion:
            case SerializedPropertyType.Vector2Int:
            case SerializedPropertyType.Vector3Int:
            case SerializedPropertyType.RectInt:
            case SerializedPropertyType.BoundsInt:
            case SerializedPropertyType.Color:
                return false;
            default:
                return !property.isArray;
        }
    }

    private static string GetPropertyValueString(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString().ToLower();
            case SerializedPropertyType.Float:
                return property.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case SerializedPropertyType.String:
                return property.stringValue;
            case SerializedPropertyType.Color:
                return $"RGBA({property.colorValue.r.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.colorValue.g.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.colorValue.b.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.colorValue.a.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue != null ? $"{property.objectReferenceValue.name} ({property.objectReferenceValue.GetType().Name})" : "null";
            case SerializedPropertyType.LayerMask:
                return property.intValue.ToString();
            case SerializedPropertyType.Enum:
                if (property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length)
                    return property.enumDisplayNames[property.enumValueIndex];
                return property.enumValueIndex.ToString();
            case SerializedPropertyType.Vector2:
                return $"({property.vector2Value.x.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.vector2Value.y.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
            case SerializedPropertyType.Vector3:
                return $"({property.vector3Value.x.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.vector3Value.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.vector3Value.z.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
            case SerializedPropertyType.Vector4:
                return $"({property.vector4Value.x.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.vector4Value.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.vector4Value.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.vector4Value.w.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
            case SerializedPropertyType.Rect:
                return property.rectValue.ToString();
            case SerializedPropertyType.ArraySize:
                return property.intValue.ToString();
            case SerializedPropertyType.Character:
                return ((char)property.intValue).ToString();
            case SerializedPropertyType.AnimationCurve:
                return "AnimationCurve";
            case SerializedPropertyType.Bounds:
                return property.boundsValue.ToString();
            case SerializedPropertyType.Quaternion:
                return $"({property.quaternionValue.x.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.quaternionValue.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.quaternionValue.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {property.quaternionValue.w.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
            case SerializedPropertyType.ExposedReference:
                return property.exposedReferenceValue != null ? property.exposedReferenceValue.name : "null";
            case SerializedPropertyType.Vector2Int:
                return property.vector2IntValue.ToString();
            case SerializedPropertyType.Vector3Int:
                return property.vector3IntValue.ToString();
            case SerializedPropertyType.RectInt:
                return property.rectIntValue.ToString();
            case SerializedPropertyType.BoundsInt:
                return property.boundsIntValue.ToString();
            case SerializedPropertyType.ManagedReference:
                return property.managedReferenceValue != null ? property.managedReferenceValue.ToString() : "null";
            default:
                return "Unsupported/Compound Type";
        }
    }

    public static void PastePropertiesFromClipboard(GameObject targetGo)
    {
        if (targetGo == null) return;
        string text = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogError("Clipboard is empty or contains no valid properties to paste!");
            return;
        }

        Undo.RegisterCompleteObjectUndo(targetGo, "Paste GameObject Properties");

        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        
        Component currentComponent = null;
        SerializedObject currentSerializedObject = null;
        
        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("GameObject:"))
            {
                // We keep the target GameObject's name instead of overwriting it, but can set it if desired.
            }
            else if (line.StartsWith("Tag:"))
            {
                string tag = line.Substring("Tag:".Length).Trim();
                try 
                { 
                    targetGo.tag = tag; 
                } 
                catch (System.Exception ex) 
                {
                    Debug.LogWarning($"Could not set tag '{tag}': {ex.Message}");
                }
            }
            else if (line.StartsWith("Layer:"))
            {
                string layerName = line.Substring("Layer:".Length).Trim();
                int layer = LayerMask.NameToLayer(layerName);
                if (layer != -1)
                {
                    targetGo.layer = layer;
                }
                else
                {
                    Debug.LogWarning($"Layer '{layerName}' not found in TagManager. Keeping original layer.");
                }
            }
            else if (line.StartsWith("Active:"))
            {
                if (bool.TryParse(line.Substring("Active:".Length).Trim(), out bool active))
                {
                    targetGo.SetActive(active);
                }
            }
            else if (line.StartsWith("=== Component:"))
            {
                if (currentSerializedObject != null)
                {
                    currentSerializedObject.ApplyModifiedProperties();
                }

                string compTypeName = line.Replace("=== Component:", "").Replace("===", "").Trim();
                System.Type type = System.Type.GetType(compTypeName);
                if (type == null)
                {
                    // Search all assemblies
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(compTypeName);
                        if (type != null) break;
                    }
                }

                if (type != null)
                {
                    currentComponent = targetGo.GetComponent(type);
                    if (currentComponent == null)
                    {
                        currentComponent = targetGo.AddComponent(type);
                        Undo.RegisterCreatedObjectUndo(currentComponent, "Add Component for Paste");
                    }
                    currentSerializedObject = new SerializedObject(currentComponent);
                }
                else
                {
                    currentComponent = null;
                    currentSerializedObject = null;
                    Debug.LogWarning($"Could not find component type: {compTypeName}");
                }
            }
            else if (line.Contains("=") && currentSerializedObject != null)
            {
                int index = line.IndexOf('=');
                string path = line.Substring(0, index).Trim();
                string valueStr = line.Substring(index + 1).Trim();

                SerializedProperty prop = currentSerializedObject.FindProperty(path);
                if (prop != null)
                {
                    try
                    {
                        SetPropertyValueFromString(prop, valueStr);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to set property '{path}' to '{valueStr}': {ex.Message}");
                    }
                }
            }
        }

        if (currentSerializedObject != null)
        {
            currentSerializedObject.ApplyModifiedProperties();
        }

        Debug.Log($"Successfully pasted properties to GameObject '{targetGo.name}'!");
    }

    private static void SetPropertyValueFromString(SerializedProperty prop, string valueStr)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                if (int.TryParse(valueStr, out int iVal)) prop.intValue = iVal;
                break;
            case SerializedPropertyType.Boolean:
                if (bool.TryParse(valueStr, out bool bVal)) prop.boolValue = bVal;
                break;
            case SerializedPropertyType.Float:
                if (TryParseFloat(valueStr, out float fVal)) prop.floatValue = fVal;
                break;
            case SerializedPropertyType.String:
                prop.stringValue = valueStr;
                break;
            case SerializedPropertyType.Color:
                prop.colorValue = ParseColor(valueStr);
                break;
            case SerializedPropertyType.LayerMask:
                if (int.TryParse(valueStr, out int lmVal)) prop.intValue = lmVal;
                break;
            case SerializedPropertyType.Enum:
                int enumIdx = System.Array.IndexOf(prop.enumDisplayNames, valueStr);
                if (enumIdx != -1)
                {
                    prop.enumValueIndex = enumIdx;
                }
                else if (int.TryParse(valueStr, out int enumInt))
                {
                    prop.enumValueIndex = enumInt;
                }
                break;
            case SerializedPropertyType.Vector2:
                prop.vector2Value = ParseVector2(valueStr);
                break;
            case SerializedPropertyType.Vector3:
                prop.vector3Value = ParseVector3(valueStr);
                break;
            case SerializedPropertyType.Vector4:
                prop.vector4Value = ParseVector4(valueStr);
                break;
            case SerializedPropertyType.Quaternion:
                prop.quaternionValue = ParseQuaternion(valueStr);
                break;
            case SerializedPropertyType.ArraySize:
                if (int.TryParse(valueStr, out int szVal)) prop.intValue = szVal;
                break;
            case SerializedPropertyType.Character:
                if (valueStr.Length > 0) prop.intValue = valueStr[0];
                break;
            case SerializedPropertyType.Vector2Int:
                prop.vector2IntValue = ParseVector2Int(valueStr);
                break;
            case SerializedPropertyType.Vector3Int:
                prop.vector3IntValue = ParseVector3Int(valueStr);
                break;
        }
    }

    private static bool TryParseFloat(string s, out float result)
    {
        s = s.Trim();
        if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
        {
            return true;
        }
        if (float.TryParse(s.Replace('.', ','), out result))
        {
            return true;
        }
        if (float.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
        {
            return true;
        }
        return float.TryParse(s, out result);
    }

    private static Vector2 ParseVector2(string s)
    {
        s = s.Trim('(', ')', ' ');
        string[] parts = s.Split(',');
        if (parts.Length >= 2)
        {
            TryParseFloat(parts[0], out float x);
            TryParseFloat(parts[1], out float y);
            return new Vector2(x, y);
        }
        return Vector2.zero;
    }

    private static Vector3 ParseVector3(string s)
    {
        s = s.Trim('(', ')', ' ');
        string[] parts = s.Split(',');
        if (parts.Length >= 3)
        {
            TryParseFloat(parts[0], out float x);
            TryParseFloat(parts[1], out float y);
            TryParseFloat(parts[2], out float z);
            return new Vector3(x, y, z);
        }
        return Vector3.zero;
    }

    private static Vector4 ParseVector4(string s)
    {
        s = s.Trim('(', ')', ' ');
        string[] parts = s.Split(',');
        if (parts.Length >= 4)
        {
            TryParseFloat(parts[0], out float x);
            TryParseFloat(parts[1], out float y);
            TryParseFloat(parts[2], out float z);
            TryParseFloat(parts[3], out float w);
            return new Vector4(x, y, z, w);
        }
        return Vector4.zero;
    }

    private static Quaternion ParseQuaternion(string s)
    {
        s = s.Trim('(', ')', ' ');
        string[] parts = s.Split(',');
        if (parts.Length >= 4)
        {
            TryParseFloat(parts[0], out float x);
            TryParseFloat(parts[1], out float y);
            TryParseFloat(parts[2], out float z);
            TryParseFloat(parts[3], out float w);
            return new Quaternion(x, y, z, w);
        }
        return Quaternion.identity;
    }

    private static Vector2Int ParseVector2Int(string s)
    {
        s = s.Trim('(', ')', ' ');
        string[] parts = s.Split(',');
        if (parts.Length >= 2)
        {
            int.TryParse(parts[0].Trim(), out int x);
            int.TryParse(parts[1].Trim(), out int y);
            return new Vector2Int(x, y);
        }
        return Vector2Int.zero;
    }

    private static Vector3Int ParseVector3Int(string s)
    {
        s = s.Trim('(', ')', ' ');
        string[] parts = s.Split(',');
        if (parts.Length >= 3)
        {
            int.TryParse(parts[0].Trim(), out int x);
            int.TryParse(parts[1].Trim(), out int y);
            int.TryParse(parts[2].Trim(), out int z);
            return new Vector3Int(x, y, z);
        }
        return Vector3Int.zero;
    }

    private static Color ParseColor(string s)
    {
        if (s.StartsWith("RGBA"))
        {
            s = s.Substring(4).Trim('(', ')', ' ');
        }
        string[] parts = s.Split(',');
        if (parts.Length >= 4)
        {
            TryParseFloat(parts[0], out float r);
            TryParseFloat(parts[1], out float g);
            TryParseFloat(parts[2], out float b);
            TryParseFloat(parts[3], out float a);
            return new Color(r, g, b, a);
        }
        return Color.white;
    }
}
