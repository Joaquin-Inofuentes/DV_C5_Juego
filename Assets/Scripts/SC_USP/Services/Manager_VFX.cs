using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Administrador y Pool de efectos visuales (VFX).
/// Carga automáticamente todos los prefabs de las subcarpetas de VFXPACK y permite instanciarlos mediante nombre.
/// </summary>
public class Manager_VFX : MonoBehaviour
{
    public static Manager_VFX Instance { get; private set; }

    [Header("Efectos Registrados (Cargados por Inspector / Script)")]
    public List<GameObject> vfxPrefabs = new List<GameObject>();

    [Header("Pool de Partículas")]
    public int poolSize = 30;

    private List<GameObject> pooledObjects = new List<GameObject>();
    private int currentPoolIndex = 0;

    private void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        InicializarPool();
    }

    private void InicializarPool()
    {
        if (vfxPrefabs.Count == 0)
        {
            Debug.LogWarning("[Manager_VFX] No hay prefabs de VFX asignados. Ejecuta el método de carga desde el Inspector o agrega prefabs manualmente.");
            return;
        }

        // Crear contenedor padre para el pool
        GameObject parentContainer = new GameObject("VFX_Pool_Container");
        parentContainer.transform.SetParent(transform);

        // Llenar el pool con copias inactivas del primer elemento o elementos por defecto
        for (int i = 0; i < poolSize; i++)
        {
            // Rotamos los prefabs disponibles para rellenar el pool inicialmente
            GameObject prefab = vfxPrefabs[i % vfxPrefabs.Count];
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(parentContainer.transform);
            pooledObjects.Add(obj);
        }
    }

    /// <summary>
    /// Obtiene un efecto del pool, le cambia la posición e inicia su reproducción.
    /// Si el efecto solicitado coincide por nombre, se adapta su prefab temporalmente.
    /// </summary>
    public GameObject SpawnVFX(string effectName, Vector3 position)
    {
        if (pooledObjects.Count == 0) return null;

        // Buscar el prefab correspondiente por nombre
        GameObject targetPrefab = vfxPrefabs.Find(p => p != null && p.name.Equals(effectName, System.StringComparison.OrdinalIgnoreCase));
        if (targetPrefab == null)
        {
            Debug.LogWarning($"[Manager_VFX] No se encontró ningún prefab de VFX con el nombre '{effectName}'");
            // Usamos el de respaldo (primer prefab)
            targetPrefab = vfxPrefabs[0];
        }

        // Obtener el siguiente objeto del pool circular
        GameObject obj = pooledObjects[currentPoolIndex];
        currentPoolIndex = (currentPoolIndex + 1) % poolSize;

        // Si el objeto del pool activo no es del tipo del prefab que queremos, lo re-instanciamos
        if (obj.name.Replace("(Clone)", "").Trim() != targetPrefab.name)
        {
            Destroy(obj);
            obj = Instantiate(targetPrefab);
            obj.transform.SetParent(pooledObjects[0].transform.parent);
            pooledObjects[currentPoolIndex == 0 ? poolSize - 1 : currentPoolIndex - 1] = obj;
        }

        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * 2f;
        obj.SetActive(false);
        obj.SetActive(true);

        // Si tiene partículas, reproducir
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        return obj;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Método público para cargar todos los prefabs de las subcarpetas del pack de VFX.
    /// Llamar desde un botón personalizado o el Inspector.
    /// </summary>
    [ContextMenu("Cargar Todos los VFX desde Carpetas")]
    public void CargarTodosLosVFX()
    {
        vfxPrefabs.Clear();
        string absolutePath = Path.Combine(Application.dataPath, "VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab");

        if (!Directory.Exists(absolutePath))
        {
            Debug.LogError($"[Manager_VFX] La ruta del VFX pack no existe: {absolutePath}");
            return;
        }

        // Buscar todos los archivos .prefab de manera recursiva
        string[] files = Directory.GetFiles(absolutePath, "*.prefab", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            // Convertir ruta absoluta a ruta de Assets de Unity
            string unityPath = "Assets" + file.Replace(Application.dataPath, "").Replace('\\', '/');
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(unityPath);
            if (prefab != null)
            {
                vfxPrefabs.Add(prefab);
            }
        }

        Debug.Log($"[Manager_VFX] Se han cargado exitosamente {vfxPrefabs.Count} prefabs de VFX a la lista.");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
