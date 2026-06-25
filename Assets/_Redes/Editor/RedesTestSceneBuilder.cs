using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Redes.Test;
using Redes.Views;
using Redes.Player;

namespace Redes.EditorTools
{
    /// <summary>
    /// Builds the offline "RedesTest" scene for testing animations, input, VFX, sounds offline.
    /// Invoked by Tools > Redes > 6. Crear Escena de Test
    /// and also called by Corregir (step 4).
    /// </summary>
    public static class RedesTestSceneBuilder
    {
        public const string TestScenePath = "Assets/_Redes/Scenes/RedesTest.unity";

        [MenuItem("Tools/Redes/6. Crear Escena de Test", priority = 6)]
        public static void BuildTestScene()
        {
            Debug.Log("[TEST][BUILDER] === Creando escena de test offline ===");

            // Create or open the test scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- ENVIRONMENT ----
            BuildEnvironment();

            // ---- PLAYER ----
            var player = BuildPlayer();

            // ---- WIRE CAMERA FOLLOW ----
            var mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam != null)
            {
                var follow = mainCam.AddComponent<CameraFollow>();
                Assign(follow, "_target", player.transform);
            }

            // ---- ENEMY ----
            var dummy = BuildDummy();

            // ---- UI ----
            var (debugText, killText, legendText, logText, ammoText) = BuildUI(player);

            // ---- TEST MANAGER ----
            var managerGo = new GameObject("TestSceneManager");
            var manager = managerGo.AddComponent<TestSceneManager>();
            Assign(manager, "_killCounterText", killText);
            Assign(manager, "_controlsLegendText", legendText);
            Assign(manager, "_eventLog", logText);
            Assign(manager, "_dummy", dummy.GetComponent<DummyEnemy>());

            // ---- WIRE PLAYER TESTER ----
            var tester = player.GetComponent<OfflinePlayerTester>();
            if (tester != null)
            {
                Assign(tester, "_debugText", debugText);
                Assign(tester, "_target", dummy.GetComponent<DummyEnemy>());

                // Wire EventBus and Muzzle
                var peb = player.GetComponent<PlayerEventBus>();
                Assign(tester, "_eventBus", peb);

                // Find muzzle in hierarchy
                var muzzle = FindInChildren(player.transform, "Muzzle");
                if (muzzle != null)
                    Assign(tester, "_muzzle", muzzle);
                else
                    Debug.LogWarning("[TEST][BUILDER] Muzzle no encontrado en el player — asignar manualmente.");

                // Wire shoot sound if available
                var shootSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Shoot.wav");
                if (shootSound != null)
                    Assign(tester, "_shootSound", shootSound);

                // Wire bullet prefab
                var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedesPrefabCreator.BulletPrefabPath);
                if (bulletPrefab != null)
                    Assign(tester, "_bulletPrefab", bulletPrefab);
            }

            // ---- SAVE ----
            if (!AssetDatabase.IsValidFolder("Assets/_Redes/Scenes"))
                AssetDatabase.CreateFolder("Assets/_Redes", "Scenes");

            EditorSceneManager.SaveScene(scene, TestScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[TEST][BUILDER] Escena de test creada en: {TestScenePath}");
            Debug.Log("[TEST][BUILDER] Presiona Play para testear. Controles: WASD=mover, LMB=disparar, R=debug.");
        }

        // ----------------------------------------
        private static void BuildEnvironment()
        {
            // Lighting
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.84f);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            var groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.3f, 0.5f, 0.3f);
            ground.GetComponent<Renderer>().material = groundMat;

            // Camera (Orthogonal 90 deg Pleno)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            camGo.transform.position = new Vector3(0f, 15f, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camGo.AddComponent<AudioListener>();

            // Ambient light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.4f);

            Debug.Log("[TEST][BUILDER] Entorno creado (suelo, cámara, luz).");
        }

        private static GameObject BuildPlayer()
        {
            var playerGo = new GameObject("TestPlayer");
            playerGo.transform.position = new Vector3(0, 0, 0);

            // -- Model --
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonSoldiers_demo/models/ToonSoldier_demo.FBX");
            GameObject modelObj;
            Animator animator = null;

            if (modelAsset != null)
            {
                modelObj = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, playerGo.transform);
                modelObj.name = "Model";
                modelObj.transform.localPosition = Vector3.zero;
                modelObj.transform.localRotation = Quaternion.identity;
                modelObj.transform.localScale = Vector3.one * 1.5f;

                animator = modelObj.GetComponent<Animator>();
                if (animator == null) animator = modelObj.AddComponent<Animator>();
                animator.applyRootMotion = false;

                // Assign or create animator controller
                var ctrl = GetOrCreatePlayerAnimator();
                animator.runtimeAnimatorController = ctrl;

                Debug.Log("[TEST][BUILDER] Modelo ToonSoldier cargado. Scale=0.01");
            }
            else
            {
                Debug.LogWarning("[TEST][BUILDER] ToonSoldier FBX no encontrado. Usando cápsula básica.");
                modelObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                modelObj.name = "Model";
                modelObj.transform.SetParent(playerGo.transform, false);
                modelObj.transform.localPosition = new Vector3(0, 1f, 0);
            }

            // -- Muzzle --
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(playerGo.transform, false);
            muzzle.transform.localPosition = new Vector3(0, 1.5f, 0.8f);

            // -- Player systems (offline-only) --
            var peb = playerGo.AddComponent<PlayerEventBus>();

            var animView = playerGo.AddComponent<PlayerAnimationView>();
            if (animator != null)
            {
                Assign(animView, "_animator", animator);
                Assign(animView, "_eventBus", peb);
            }

            var tester = playerGo.AddComponent<OfflinePlayerTester>();
            // Tester wired later in BuildTestScene to have dummy reference

            // -- Capsule collider for reference --
            var col = playerGo.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0, 0.9f, 0);

            Debug.Log($"[TEST][BUILDER] Player offline creado: EventBus={peb != null}, Animator={animator != null}");
            return playerGo;
        }

        private static GameObject BuildDummy()
        {
            var dummy = new GameObject("DummyEnemy");
            dummy.transform.position = new Vector3(5f, 0f, 3f);
            dummy.transform.rotation = Quaternion.Euler(0, 180, 0);

            // Visual body
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonSoldiers_demo/models/ToonSoldier_demo.FBX");
            if (modelAsset != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, dummy.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 1.5f;

                // Tint red so it's clearly an enemy
                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material mat = new Material(r.sharedMaterial != null ? r.sharedMaterial.shader : shader);
                    mat.color = new Color(0.8f, 0.2f, 0.2f);
                    r.sharedMaterial = mat;
                }
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(dummy.transform, false);
                body.transform.localPosition = new Vector3(0, 1f, 0);
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                Material mat = new Material(shader);
                mat.color = new Color(0.8f, 0.2f, 0.2f);
                body.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var col = dummy.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0, 0.9f, 0);

            var dummyComp = dummy.AddComponent<DummyEnemy>();

            // Health bar above head (world-space canvas)
            var canvasGo = new GameObject("HealthBarCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(dummy.transform, false);
            canvasGo.transform.localPosition = new Vector3(0, 2.5f, 0);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 30);
            canvasGo.transform.localScale = Vector3.one * 0.01f;

            var bg = new GameObject("BG", typeof(RectTransform));
            bg.transform.SetParent(canvasGo.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var bgRect = (RectTransform)bg.transform;
            bgRect.sizeDelta = new Vector2(200, 20);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(bg.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = Color.red;
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.pivot = new Vector2(0, 0.5f);

            // Label
            var label = new GameObject("HPLabel", typeof(RectTransform));
            label.transform.SetParent(canvasGo.transform, false);
            var txt = label.AddComponent<Text>();
            txt.text = "ENEMIGO";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            var lblRect = (RectTransform)label.transform;
            lblRect.sizeDelta = new Vector2(200, 30);
            lblRect.anchoredPosition = new Vector2(0, 20);

            Debug.Log($"[TEST][BUILDER] DummyEnemy creado en {dummy.transform.position}");
            return dummy;
        }

        private static (Text debug, Text kills, Text legend, Text log, Text ammo) BuildUI(GameObject player)
        {
            var canvasGo = new GameObject("TestHUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Background panel
            var panel = CreatePanel(canvasGo.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10, -10), new Vector2(300, 120), new Color(0, 0, 0, 0.55f));
            panel.name = "DebugPanel";
            ((RectTransform)panel.transform).pivot = new Vector2(0, 1);

            // Debug text (player state)
            var debugText = CreateText(panel.transform, "DebugText", "",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, 13, Color.white, TextAnchor.UpperLeft);

            // Kill counter (top-center)
            var killPanel = CreatePanel(canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -10), new Vector2(200, 50), new Color(0, 0, 0, 0.7f));
            killPanel.name = "KillPanel";
            ((RectTransform)killPanel.transform).pivot = new Vector2(0.5f, 1);
            var killText = CreateText(killPanel.transform, "KillText", "KILLS: 0",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, 22, Color.yellow, TextAnchor.MiddleCenter);
            killText.fontStyle = FontStyle.Bold;

            // Controls legend (top-right)
            var legendPanel = CreatePanel(canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-10, -10), new Vector2(220, 140), new Color(0, 0, 0, 0.6f));
            legendPanel.name = "LegendPanel";
            ((RectTransform)legendPanel.transform).pivot = new Vector2(1, 1);
            var legendText = CreateText(legendPanel.transform, "LegendText", "",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, 12, new Color(0.8f, 0.9f, 1f), TextAnchor.UpperLeft);

            // Ammo display (bottom-right)
            var ammoPanel = CreatePanel(canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-10, 10), new Vector2(220, 60), new Color(0, 0, 0, 0.7f));
            ammoPanel.name = "AmmoPanel";
            ((RectTransform)ammoPanel.transform).pivot = new Vector2(1, 0);

            // Centered ammo text to prevent anchoredPosition shifts
            var ammoText = CreateText(ammoPanel.transform, "AmmoText", "AMMO: 10/10",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210, 50), 18, Color.white, TextAnchor.MiddleCenter);
            ammoText.fontStyle = FontStyle.Bold;

            // Attach PlayerAmmoView (MVC View)
            var ammoView = ammoText.gameObject.AddComponent<PlayerAmmoView>();
            var peb = player.GetComponent<PlayerEventBus>();
            Assign(ammoView, "_eventBus", peb);
            Assign(ammoView, "_ammoText", ammoText);

            // Event log (bottom)
            var logPanel = CreatePanel(canvasGo.transform, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 10), new Vector2(-20, 140), new Color(0, 0, 0, 0.6f));
            logPanel.name = "EventLogPanel";
            ((RectTransform)logPanel.transform).anchorMax = new Vector2(1, 0);
            ((RectTransform)logPanel.transform).anchorMin = new Vector2(0, 0);
            ((RectTransform)logPanel.transform).pivot = new Vector2(0.5f, 0);
            ((RectTransform)logPanel.transform).sizeDelta = new Vector2(-20, 140);
            var logText = CreateText(logPanel.transform, "EventLog", "",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, 11, Color.green, TextAnchor.LowerLeft);
            logText.supportRichText = true;

            Debug.Log("[TEST][BUILDER] HUD de test creado.");
            return (debugText, killText, legendText, logText, ammoText);
        }

        // ----------------------------------------
        // Helpers
        // ----------------------------------------

        private static AnimatorController GetOrCreatePlayerAnimator()
        {
            string path = "Assets/_Redes/Art/PlayerAnimator.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) return controller;

            // Reuse creator from prefab builder
            return RedesPrefabCreator.GetOrCreateAnimator(path);
        }

        private static Transform FindInChildren(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>())
                if (t.name == name) return t;
            return null;
        }

        private static void Assign(Object target, string field, Object value)
        {
            if (target == null) { Debug.LogWarning($"[TEST][BUILDER] target null al asignar '{field}'"); return; }
            var so = new UnityEditor.SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[TEST][BUILDER] Campo '{field}' no encontrado en {target.GetType().Name}");
        }

        private static GameObject CreatePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta,
            int fontSize, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            if (sizeDelta == Vector2.zero)
            {
                rect.offsetMin = new Vector2(5, 5);
                rect.offsetMax = new Vector2(-5, -5);
            }
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.text = content;
            txt.alignment = anchor;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }
    }
}
