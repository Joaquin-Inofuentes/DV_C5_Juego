#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Game.Sensors;

public class AmmoCreator : EditorWindow
{
    [MenuItem("Tools/Corregir")]
    public static void CorregirEscena()
    {
        CrearPrefabMunicion();
        ConfigurarYCorregirUnidades();
    }

    private static void CrearPrefabMunicion()
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
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (tex != null)
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            else
                Debug.LogWarning("[AmmoCreator] No se encontró el sprite en la ruta: " + spritePath);
        }
        
        sr.sortingOrder = 5;

        // 3. Agregar el Collider 2D para el Trigger
        BoxCollider2D collider = ammoObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        
        // 4. Añadir el script de funcionalidad
        InteractableAmmo interactable = ammoObj.AddComponent<InteractableAmmo>();
        interactable.ammoAmount = 300;
        interactable.reloadToMax = true;

        // 5. Posicionarlo en el centro de la cámara
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            Vector3 camPos = view.camera.transform.position;
            ammoObj.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        }
        else
        {
            ammoObj.transform.position = Vector3.zero;
        }

        // 6. Seleccionarlo automáticamente para el usuario
        Selection.activeGameObject = ammoObj;
        
        // 7. Guardarlo como un Prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        string prefabPath = "Assets/Prefabs/InteractableAmmo.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(ammoObj, prefabPath, InteractionMode.UserAction, out bool success);
        
        if (success)
            Debug.Log($"<color=lime>[Herramienta]</color> ¡Munición Creada y guardada como Prefab en {prefabPath}! Instanciada en la escena actual.");
        else
            Debug.Log($"<color=lime>[Herramienta]</color> ¡Munición Creada en la escena actual!");

        // 8. Registrar el cambio para poder deshacer con Ctrl+Z
        Undo.RegisterCreatedObjectUndo(ammoObj, "Crear Munición");
    }

    private static void ConfigurarYCorregirUnidades()
    {
        Game.Squad.UnitController[] unidades = Object.FindObjectsOfType<Game.Squad.UnitController>(true);
        int modificadas = 0;

        Debug.Log($"<color=orange>[Herramienta]</color> Iniciando corrección y auto-asignación de variables en {unidades.Length} unidades...");

        foreach (var unit in unidades)
        {
            bool modificadoEsteUnit = false;

            // --- 1. Asegurar Componentes Básicos en UnitController ---
            if (unit.model == null)
            {
                unit.model = unit.GetComponent<UnitModel>();
                if (unit.model != null)
                {
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado model (UnitModel)");
                    modificadoEsteUnit = true;
                }
            }

            if (unit.view == null)
            {
                unit.view = unit.GetComponent<UnitView>();
                if (unit.view != null)
                {
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado view (UnitView)");
                    modificadoEsteUnit = true;
                }
            }

            if (unit.agent == null)
            {
                unit.agent = unit.GetComponent<IA_P2_AgentIA>();
                if (unit.agent != null)
                {
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado agent (IA_P2_AgentIA)");
                    modificadoEsteUnit = true;
                }
            }

            if (unit.shooter == null)
            {
                unit.shooter = unit.GetComponentInChildren<Disparador>();
                if (unit.shooter != null)
                {
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado shooter (Disparador)");
                    modificadoEsteUnit = true;
                }
            }

            if (unit.detector == null)
            {
                unit.detector = unit.GetComponentInChildren<GenericDetector>();
                if (unit.detector != null)
                {
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado detector (GenericDetector)");
                    modificadoEsteUnit = true;
                }
            }

            if (unit.unitCollider == null)
            {
                unit.unitCollider = unit.GetComponent<Collider2D>();
                if (unit.unitCollider != null)
                {
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado unitCollider (Collider2D)");
                    modificadoEsteUnit = true;
                }
            }

            // --- 2. Buscar vivoGO y caidoGO en Hijos ---
            if (unit.vivoGO == null)
            {
                Transform tVivo = unit.transform.Find("V_Vivo");
                if (tVivo == null) tVivo = unit.transform.Find("Vivo");
                if (tVivo != null)
                {
                    unit.vivoGO = tVivo.gameObject;
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado vivoGO -> {unit.vivoGO.name}");
                    modificadoEsteUnit = true;
                }
            }

            if (unit.caidoGO == null)
            {
                Transform tCaido = unit.transform.Find("V_Muerto");
                if (tCaido == null) tCaido = unit.transform.Find("V_Caido");
                if (tCaido == null) tCaido = unit.transform.Find("Muerto");
                if (tCaido == null) tCaido = unit.transform.Find("Caido");
                if (tCaido != null)
                {
                    unit.caidoGO = tCaido.gameObject;
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado caidoGO -> {unit.caidoGO.name}");
                    modificadoEsteUnit = true;
                }
            }

            // --- 2.5. Asegurar ShotSensor ---
            ShotSensor[] sensores = unit.GetComponentsInChildren<ShotSensor>();
            foreach (var sensor in sensores)
            {
                if (sensor.miController == null)
                {
                    sensor.miController = unit;
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name}: Asignado miController en ShotSensor");
                    modificadoEsteUnit = true;
                }
            }

            // --- 3. Asegurar variables básicas en UnitView ---
            if (unit.view != null)
            {
                if (unit.view.model == null && unit.model != null)
                {
                    unit.view.model = unit.model;
                    Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name} (View): Asignado model");
                    modificadoEsteUnit = true;
                }

                if (unit.view.mainSprite == null)
                {
                    // Intentar con un SpriteRenderer llamado V_Soldier o V_Color, o el primero del hijo
                    SpriteRenderer srComp = unit.transform.Find("V_Soldier")?.GetComponent<SpriteRenderer>();
                    if (srComp == null) srComp = unit.transform.Find("V_Color")?.GetComponent<SpriteRenderer>();
                    if (srComp == null) srComp = unit.GetComponentInChildren<SpriteRenderer>();
                    if (srComp != null)
                    {
                        unit.view.mainSprite = srComp;
                        Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name} (View): Asignado mainSprite -> {srComp.name}");
                        modificadoEsteUnit = true;
                    }
                }

                if (unit.view.graphicsRoot == null)
                {
                    Transform tGraf = unit.transform.Find("V_Soldier");
                    if (tGraf == null) tGraf = unit.transform.Find("Graphics");
                    if (tGraf != null)
                    {
                        unit.view.graphicsRoot = tGraf;
                        Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name} (View): Asignado graphicsRoot -> {tGraf.name}");
                        modificadoEsteUnit = true;
                    }
                }

                if (unit.view.lineRenderer == null)
                {
                    unit.view.lineRenderer = unit.GetComponent<LineRenderer>();
                    if (unit.view.lineRenderer != null)
                    {
                        Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name} (View): Asignado lineRenderer");
                        modificadoEsteUnit = true;
                    }
                }

                if (unit.view.selectionRing == null)
                {
                    Transform tSel = unit.transform.Find("V_Seleccionado");
                    if (tSel == null) tSel = unit.transform.Find("Seleccionado");
                    if (tSel != null)
                    {
                        unit.view.selectionRing = tSel.gameObject;
                        Debug.Log($"<color=cyan>[Autocorrección]</color> {unit.name} (View): Asignado selectionRing -> {unit.view.selectionRing.name}");
                        modificadoEsteUnit = true;
                    }
                }
            }

            // --- 4. Configurar AudioSource ---
            if (unit.view != null && unit.model != null)
            {
                AudioSource source = unit.view.gameObject.GetComponent<AudioSource>();
                if (source == null)
                {
                    source = unit.view.gameObject.AddComponent<AudioSource>();
                    Debug.Log($"<color=cyan>[Herramienta]</color> Añadido AudioSource a {unit.name}");
                    modificadoEsteUnit = true;
                }
                
                // Configurar 3D
                source.spatialBlend = 1f; // 100% 3D
                source.minDistance = 5f;
                source.maxDistance = 20f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.playOnAwake = false;

                if (unit.view.audioSource != source)
                {
                    unit.view.audioSource = source;
                    modificadoEsteUnit = true;
                }

                // Mapeo de Audios según Especialidad
                string shootClipPath = "";
                string damageClipPath = "";

                switch (unit.model.specialization)
                {
                    case UnitSpecialization.Flancotirador:
                        shootClipPath = "Assets/_Redes/Audio/Importados/SFX_Disparo3.wav";
                        damageClipPath = "Assets/_Redes/Audio/Importados/SFX_RecibirDano1.wav";
                        break;
                    case UnitSpecialization.Asalto:
                        shootClipPath = "Assets/_Redes/Audio/Importados/SFX_Disparo1.wav";
                        damageClipPath = "Assets/_Redes/Audio/Importados/SFX_RecibirDano2.wav";
                        break;
                    case UnitSpecialization.Medico:
                    case UnitSpecialization.Apoyo:
                        shootClipPath = "Assets/_Redes/Audio/Importados/SFX_Disparo2.wav";
                        damageClipPath = "Assets/_Redes/Audio/Importados/SFX_RecibirDano3.wav";
                        break;
                    case UnitSpecialization.EnemigoSimple:
                        shootClipPath = "Assets/_Redes/Audio/Importados/SFX_Disparo2.wav";
                        damageClipPath = "Assets/_Redes/Audio/Importados/SFX_RecibirDano4.wav";
                        break;
                    default:
                        shootClipPath = "Assets/_Redes/Audio/Importados/SFX_Disparo1.wav";
                        damageClipPath = "Assets/_Redes/Audio/Importados/SFX_RecibirDano1.wav";
                        break;
                }

                AudioClip shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(shootClipPath);
                AudioClip damageClip = AssetDatabase.LoadAssetAtPath<AudioClip>(damageClipPath);

                if (unit.view.shootSound != shootClip || unit.view.damageSound != damageClip)
                {
                    unit.view.shootSound = shootClip;
                    unit.view.damageSound = damageClip;
                    modificadoEsteUnit = true;
                }

                if (modificadoEsteUnit)
                {
                    EditorUtility.SetDirty(unit.view);
                    EditorUtility.SetDirty(source);
                    EditorUtility.SetDirty(unit);
                    modificadas++;
                    Debug.Log($"<color=lime>[Herramienta]</color> Configurado Audio 3D MVC para {unit.name} ({unit.model.specialization}). Disparo: {unit.view.shootSound?.name}, Daño: {unit.view.damageSound?.name}");
                }
            }
        }

        if (modificadas > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=yellow>[Herramienta]</color> Se han corregido y configurado {modificadas} unidades.");
        }
        else
        {
            Debug.Log("<color=green>[Herramienta]</color> Todas las unidades ya estaban correctamente configuradas.");
        }

        // --- 5. Limpieza de ShotSensors Huérfanos (ej: Formacion) ---
        ShotSensor[] todosSensores = Object.FindObjectsOfType<ShotSensor>(true);
        int sensoresRemovidos = 0;
        foreach (var sensor in todosSensores)
        {
            if (sensor.GetComponentInParent<Game.Squad.UnitController>() == null)
            {
                Debug.Log($"<color=orange>[Limpieza]</color> Se ha removido un ShotSensor del objeto '{sensor.gameObject.name}' ya que no pertenece a una unidad.");
                Object.DestroyImmediate(sensor, true);
                sensoresRemovidos++;
            }
        }
        if (sensoresRemovidos > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
