using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class Physics2DMigrator
{
    [MenuItem("Tools/Migrate Project to Physics 2D")]
    public static void Migrate()
    {
        Debug.Log("--- INICIANDO MIGRACION A FISICA 2D ---");

        // 1. Migrar Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Plugins") || path.Contains("Photon") || path.Contains("TextMesh Pro") || path.Contains("ParrelSync"))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Instanciar temporalmente el prefab para editarlo
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) continue;

            // Limpiar scripts rotos/perdidos recursivamente en la instancia
            CleanMissingScriptsRecursively(instance);

            bool modified = Convert3DTo2DComponents(instance);
            
            // Alinear Z a 0 en el prefab
            AlignZDepthRecursively(instance);

            // Guardar cambios si hubo modificaciones o limpieza
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Debug.Log($"Prefab migrado a 2D y guardado: {path}");

            GameObject.DestroyImmediate(instance);
        }

        // 2. Migrar Escenas (en particular, buscaremos la escena _USP)
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("_USP")) continue; // Enfocarse principalmente en la escena _USP

            Debug.Log($"Abriendo escena para migracion: {path}");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            
            GameObject[] rootObjects = scene.GetRootGameObjects();
            bool sceneModified = false;

            foreach (GameObject rootObj in rootObjects)
            {
                // Limpiar scripts rotos en escena
                CleanMissingScriptsRecursively(rootObj);

                // Buscar recursivamente todos los objetos de la escena
                Transform[] allChildren = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (Convert3DTo2DComponents(child.gameObject))
                    {
                        sceneModified = true;
                    }

                    // Alinear profundidad Z a 0 para asegurar alineación de físicas y sprites
                    if (child.GetComponent<Camera>() == null)
                    {
                        Vector3 pos = child.position;
                        if (Mathf.Abs(pos.z) > 0.001f)
                        {
                            child.position = new Vector3(pos.x, pos.y, 0f);
                            sceneModified = true;
                            Debug.Log($"Alineado eje Z a 0 para objeto de escena: {child.name}");
                        }
                    }

                    // Asegurar coherencia para Muros u Obstáculos
                    string nameLower = child.name.ToLower();
                    if (nameLower.Contains("muro") || nameLower.Contains("obstaculo") || nameLower.Contains("obstacule"))
                    {
                        // Asegurar que estén en la capa 6 (Obstáculos)
                        if (child.gameObject.layer != 6)
                        {
                            child.gameObject.layer = 6;
                            sceneModified = true;
                            Debug.Log($"Ajustada capa a 6 (Obstaculos) para: {child.name}");
                        }

                        // Asegurar que tenga BoxCollider2D
                        BoxCollider2D col2D = child.GetComponent<BoxCollider2D>();
                        if (col2D == null)
                        {
                            col2D = child.gameObject.AddComponent<BoxCollider2D>();
                            sceneModified = true;
                            Debug.Log($"Agregado BoxCollider2D faltante a obstaculo: {child.name}");
                        }
                    }
                }
            }

            if (sceneModified)
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log($"Escena migrada y guardada: {path}");
            }
        }

        Debug.Log("--- MIGRACION COMPLETADA CON EXITO ---");
    }

    private static void CleanMissingScriptsRecursively(GameObject obj)
    {
        if (obj == null) return;
        
        // Remover MonoBehaviours rotos
        int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
        if (removedCount > 0)
        {
            Debug.Log($"[Limpieza] Removidos {removedCount} scripts perdidos en '{obj.name}'");
        }

        // Hacer lo mismo para todos los hijos
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            CleanMissingScriptsRecursively(obj.transform.GetChild(i).gameObject);
        }
    }

    private static void AlignZDepthRecursively(GameObject obj)
    {
        if (obj == null) return;

        // Alinear localPosition en Z a 0 si no es una cámara
        if (obj.GetComponent<Camera>() == null)
        {
            Vector3 localPos = obj.transform.localPosition;
            if (Mathf.Abs(localPos.z) > 0.001f)
            {
                obj.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            }
        }

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            AlignZDepthRecursively(obj.transform.GetChild(i).gameObject);
        }
    }

    private static bool Convert3DTo2DComponents(GameObject obj)
    {
        bool modified = false;

        // 1. Guardar propiedades de los componentes 3D existentes
        BoxCollider col3D = obj.GetComponent<BoxCollider>();
        Rigidbody rb3D = obj.GetComponent<Rigidbody>();

        bool hasCol = col3D != null;
        bool hasRb = rb3D != null;

        Vector3 size = Vector3.one;
        Vector3 center = Vector3.zero;
        bool isTrigger = false;

        float mass = 1f;
        float drag = 0f;
        float angularDrag = 0.05f;
        bool isKinematic = false;

        if (hasCol)
        {
            size = col3D.size;
            center = col3D.center;
            isTrigger = col3D.isTrigger;
        }

        if (hasRb)
        {
            mass = rb3D.mass;
            drag = rb3D.drag;
            angularDrag = rb3D.angularDrag;
            isKinematic = rb3D.isKinematic;
        }

        // 2. Destruir TODOS los componentes 3D conflictivos ANTES de agregar los de 2D
        if (hasCol)
        {
            GameObject.DestroyImmediate(col3D, true);
        }
        if (hasRb)
        {
            GameObject.DestroyImmediate(rb3D, true);
        }

        // 3. Crear componentes 2D equivalentes sin conflicto
        if (hasCol)
        {
            BoxCollider2D col2D = obj.AddComponent<BoxCollider2D>();
            if (col2D != null)
            {
                col2D.size = new Vector2(size.x, size.y);
                col2D.offset = new Vector2(center.x, center.y);
                col2D.isTrigger = isTrigger;
            }
            modified = true;
            Debug.Log($"[Convertido] BoxCollider -> BoxCollider2D en '{obj.name}'");
        }

        if (hasRb)
        {
            Rigidbody2D rb2D = obj.AddComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.mass = mass;
                rb2D.drag = drag;
                rb2D.angularDrag = angularDrag;
                rb2D.isKinematic = isKinematic;
                rb2D.gravityScale = 0f;
                rb2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            modified = true;
            Debug.Log($"[Convertido] Rigidbody -> Rigidbody2D en '{obj.name}'");
        }

        return modified;
    }

    public static void MigratePrefabsTo2D()
    {
        Migrate();
        EditorApplication.Exit(0);
    }
}
