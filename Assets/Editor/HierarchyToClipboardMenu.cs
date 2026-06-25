using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

namespace DebugSystem.Editor
{
    public class HierarchyToClipboardMenu
    {
        // [MenuItem("Tools/Pruebas/Copiar Jerarquia")]
        public static void CopyHierarchyToClipboard()
        {
            try
            {
                Scene activeScene = SceneManager.GetActiveScene();
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"=========================================");
                sb.AppendLine($"REPORTE DE JERARQUÍA Y COMPONENTES");
                sb.AppendLine($"Escena Activa: {activeScene.name}");
                sb.AppendLine($"Ruta de Escena: {activeScene.path}");
                sb.AppendLine($"=========================================\n");

                GameObject[] rootObjects = activeScene.GetRootGameObjects();
                foreach (GameObject rootGo in rootObjects)
                {
                    DumpGameObject(rootGo, sb, 0);
                }

                // Guardar en el portapapeles
                GUIUtility.systemCopyBuffer = sb.ToString();
                
                Debug.Log($"[HierarchyToClipboard] ¡Jerarquía copiada con éxito! Se procesaron {rootObjects.Length} raíces de GameObjects.");
                EditorUtility.DisplayDialog("Jerarquía Copiada", "La jerarquía completa, sus componentes y valores de variables se han copiado al portapapeles.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HierarchyToClipboard Error] Falló el copiado de jerarquía: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void DumpGameObject(GameObject go, StringBuilder sb, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);
            sb.AppendLine($"{indent}▶ GameObject: \"{go.name}\" (Tag: {go.tag}, Layer: {LayerMask.LayerToName(go.layer)}, Active: {go.activeSelf})");

            // Listar componentes en el GameObject
            Component[] components = go.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null) continue; // Componente roto o nulo

                string compName = comp.GetType().Name;
                sb.AppendLine($"{indent}   ├─ [Componente] {compName}");

                // Inspeccionar variables del componente usando SerializedObject
                SerializedObject serializedComp = new SerializedObject(comp);
                SerializedProperty prop = serializedComp.GetIterator();

                bool isFirst = true;
                if (prop.NextVisible(true))
                {
                    do
                    {
                        // Omitir propiedades internas obsoletas o de control por defecto de Unity para evitar ruido
                        if (prop.name == "m_ObjectHideFlags" || prop.name == "m_CorrespondingSourceObject" ||
                            prop.name == "m_PrefabInstance" || prop.name == "m_PrefabAsset" ||
                            prop.name == "m_GameObject" || prop.name == "m_Enabled" || prop.name == "m_EditorHideFlags")
                        {
                            continue;
                        }

                        if (isFirst)
                        {
                            sb.AppendLine($"{indent}   │   Variable(s) expuesta(s):");
                            isFirst = false;
                        }

                        string valueStr = GetPropertyValueAsString(prop);
                        sb.AppendLine($"{indent}   │    • {prop.name} ({prop.propertyType}) = {valueStr}");
                    }
                    while (prop.NextVisible(false));
                }
            }

            sb.AppendLine(); // Espaciador visual

            // Recorrer hijos recursivamente
            for (int i = 0; i < go.transform.childCount; i++)
            {
                DumpGameObject(go.transform.GetChild(i).gameObject, sb, indentLevel + 1);
            }
        }

        private static string GetPropertyValueAsString(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return prop.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return prop.floatValue.ToString();
                case SerializedPropertyType.String:
                    return $"\"{prop.stringValue}\"";
                case SerializedPropertyType.Color:
                    return prop.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null ? $"Ref: {prop.objectReferenceValue.name} ({prop.objectReferenceValue.GetType().Name})" : "None/Null";
                case SerializedPropertyType.Enum:
                    int index = prop.enumValueIndex;
                    return index >= 0 && index < prop.enumDisplayNames.Length ? prop.enumDisplayNames[index] : index.ToString();
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value.ToString();
                case SerializedPropertyType.Rect:
                    return prop.rectValue.ToString();
                case SerializedPropertyType.Bounds:
                    return prop.boundsValue.ToString();
                case SerializedPropertyType.Character:
                    return ((char)prop.intValue).ToString();
                default:
                    // Si es un tipo personalizado estructurado o array genérico
                    if (prop.isArray)
                    {
                        return $"Array/List (Size: {prop.arraySize})";
                    }
                    return "...";
            }
        }
    }
}

