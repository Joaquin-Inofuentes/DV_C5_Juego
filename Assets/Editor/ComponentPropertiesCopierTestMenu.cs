using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class ComponentPropertiesCopierTestMenu
{
    [MenuItem("Tests/Test Copy Properties From Colision Scene")]
    public static void RunTest()
    {
        string scenePath = @"Assets/Scenes/Tests/_Pruebas de colision.unity";
        if (!File.Exists(scenePath))
        {
            Debug.LogError($"Scene not found at path: {scenePath}");
            return;
        }

        // Open the scene in editor
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Find any GameObject in the scene
        GameObject[] rootObjects = scene.GetRootGameObjects();
        if (rootObjects.Length == 0)
        {
            Debug.LogError("No root game objects found in the scene.");
            return;
        }

        // Find a GameObject with some components, e.g., a Rigidbody2D or Collider2D
        GameObject targetGo = null;
        foreach (var go in rootObjects)
        {
            targetGo = FindGameObjectWithComponents(go);
            if (targetGo != null) break;
        }

        if (targetGo == null)
        {
            targetGo = rootObjects[0];
        }

        Debug.Log($"Testing ComponentPropertiesCopier on GameObject: {targetGo.name}");

        // Format all properties of the GameObject
        Component[] components = targetGo.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component == null) continue;
            string formatted = ComponentPropertiesCopier.FormatComponentProperties(component);
            Debug.Log($"--- Formatted properties for {component.GetType().Name} ---\n{formatted}");
        }

        // Test GameObject copy
        Selection.activeGameObject = targetGo;
        ComponentPropertiesCopier.CopyGameObjectProperties(new MenuCommand(targetGo));
        Debug.Log("Copied GameObject properties successfully. Clipboard content length: " + GUIUtility.systemCopyBuffer.Length);
    }

    private static GameObject FindGameObjectWithComponents(GameObject go)
    {
        Component[] comps = go.GetComponents<Component>();
        if (comps.Length > 1)
        {
            return go;
        }

        foreach (Transform child in go.transform)
        {
            GameObject found = FindGameObjectWithComponents(child.gameObject);
            if (found != null) return found;
        }

        return null;
    }
}
