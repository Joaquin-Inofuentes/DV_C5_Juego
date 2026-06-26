#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AmmoCreator : EditorWindow
{
    [MenuItem("Tools/Corregir")]
    public static void CrearPrefabMunicion()
    {
        // 1. Crear el GameObject base
        GameObject ammoObj = new GameObject("InteractableAmmo");

        // 2. Cargar el Sprite de munición
        SpriteRenderer sr = ammoObj.AddComponent<SpriteRenderer>();
        string spritePath = "Assets/Textures/PickUp Municion.png";
        
        Sprite ammoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (ammoSprite != null)
        {
            sr.sprite = ammoSprite;
        }
        else
        {
            // Si no está importado como Sprite, intenta como textura y advierte
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (tex != null)
            {
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            else
            {
                Debug.LogWarning("[AmmoCreator] No se encontró el sprite en la ruta: " + spritePath);
            }
        }
        
        // Ajustar sorting layer y order para que se vea por encima del fondo
        sr.sortingOrder = 5;

        // 3. Agregar el Collider 2D para el Trigger
        BoxCollider2D collider = ammoObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        
        // El tamaño del collider depende del sprite, BoxCollider2D lo ajusta automáticamente
        // si el sprite ya está asignado al momento de crearlo.

        // 4. Añadir el script de funcionalidad
        InteractableAmmo interactable = ammoObj.AddComponent<InteractableAmmo>();
        interactable.ammoAmount = 300;
        interactable.reloadToMax = true;

        // 5. Posicionarlo en el centro de la cámara de la escena actual
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            Vector3 camPos = view.camera.transform.position;
            // Lo ponemos frente a la cámara en Z=0
            ammoObj.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        }
        else
        {
            ammoObj.transform.position = Vector3.zero;
        }

        // 6. Seleccionarlo automáticamente para el usuario
        Selection.activeGameObject = ammoObj;
        
        // 7. Guardarlo como un Prefab (opcional pero muy recomendado para reutilizar)
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        string prefabPath = "Assets/Prefabs/InteractableAmmo.prefab";
        // Guardamos el prefab y conectamos la instancia actual a ese prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(ammoObj, prefabPath, InteractionMode.UserAction, out bool success);
        
        if (success)
        {
            Debug.Log($"<color=lime>[Herramienta]</color> ¡Munición Creada y guardada como Prefab en {prefabPath}! Instanciada en la escena actual.");
        }
        else
        {
            Debug.Log($"<color=lime>[Herramienta]</color> ¡Munición Creada en la escena actual!");
        }

        // 8. Registrar el cambio para poder deshacer con Ctrl+Z
        Undo.RegisterCreatedObjectUndo(ammoObj, "Crear Munición");
    }
}
#endif
