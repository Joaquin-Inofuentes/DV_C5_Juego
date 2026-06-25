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

        // [MenuItem("Tools/Redes/6. Crear Escena de Test", priority = 6)]
        public static void BuildTestScene()
        {
            Debug.Log("[TEST][BUILDER] === Creando escena de test offline ===");

            // Create or open the test scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- ENVIRONMENT ----
            BuildEnvironment();

            // ---- PLAYER ----
            var player = BuildPlayer();
            player.name = "TestPlayer";

            // ---- WIRE CAMERA FOLLOW ----
            var mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam != null)
            {
                var follow = mainCam.AddComponent<CameraFollow>();
                Assign(follow, "_target", player.transform);
            }

            // ---- ENEMY ----
            var dummy = BuildDummy(new Vector3(5f, 0f, 0f));
            dummy.name = "DummyEnemy";

            // ---- UI ----
            var (debugText, killText, legendText, logText, ammoText) = BuildUI(player);
            var canvasGo = debugText.canvas.gameObject;

            // Load and instantiate EntityDisplayView prefab for Player and Enemy
            var displayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedesPrefabCreator.EntityDisplayViewPrefabPath);
            Views.EntityDisplayView playerDisplay = null;
            Views.EntityDisplayView dummyDisplay = null;

            if (displayPrefab != null)
            {
                // Instantiate for player
                var playerDisplayGo = (GameObject)PrefabUtility.InstantiatePrefab(displayPrefab, canvasGo.transform);
                playerDisplayGo.name = "PlayerDisplayView";
                playerDisplay = playerDisplayGo.GetComponent<Views.EntityDisplayView>();
                playerDisplay.SetNickname("PLAYER");
                playerDisplay.SetHealth(1f);

                // Instantiate for dummy
                var dummyDisplayGo = (GameObject)PrefabUtility.InstantiatePrefab(displayPrefab, canvasGo.transform);
                dummyDisplayGo.name = "DummyDisplayView";
                dummyDisplay = dummyDisplayGo.GetComponent<Views.EntityDisplayView>();
                dummyDisplay.SetNickname("ENEMY");
                dummyDisplay.SetHealth(1f);
            }

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
                if (playerDisplay != null) Assign(tester, "_displayView", playerDisplay);

                // Wire EventBus and Muzzle
                var peb = player.GetComponent<PlayerEventBus>();
                Assign(tester, "_eventBus", peb);

                // Find OrigenDeDisparo (or Muzzle fallback) in hierarchy
                var muzzle = FindInChildren(player.transform, "OrigenDeDisparo") ?? FindInChildren(player.transform, "Muzzle");
                if (muzzle != null)
                    Assign(tester, "_muzzle", muzzle);

                // Wire shoot sound if available
                var shootSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Disparo de Metralleta (1).mp3");
                if (shootSound != null)
                    Assign(tester, "_shootSound", shootSound);

                var reloadSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Recarga de Metralleta.mp3");
                if (reloadSound != null)
                    Assign(tester, "_reloadSound", reloadSound);

                var deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Lose.wav");
                if (deathSound != null)
                    Assign(tester, "_deathSound", deathSound);

                // Wire bullet prefab
                var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedesPrefabCreator.BulletPrefabPath);
                if (bulletPrefab != null)
                    Assign(tester, "_bulletPrefab", bulletPrefab);
            }

            // ---- WIRE DUMMY ENEMY AUTO-SHOOT & DISPLAY ----
            var dummyComp = dummy.GetComponent<DummyEnemy>();
            if (dummyComp != null)
            {
                if (dummyDisplay != null) Assign(dummyComp, "_displayView", dummyDisplay);

                // Create muzzle for dummy if not already present
                var dummyMuzzle = FindInChildren(dummy.transform, "OrigenDeDisparo") ?? FindInChildren(dummy.transform, "Muzzle");
                if (dummyMuzzle == null)
                {
                    var newMuzzle = new GameObject("OrigenDeDisparo");
                    newMuzzle.transform.SetParent(dummy.transform, false);
                    newMuzzle.transform.localPosition = new Vector3(0f, 1.5f, 0.8f);
                    dummyMuzzle = newMuzzle.transform;
                }
                Assign(dummyComp, "_muzzle", dummyMuzzle);

                // Wire bullet prefab for dummy
                var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedesPrefabCreator.BulletPrefabPath);
                if (bulletPrefab != null)
                    Assign(dummyComp, "_bulletPrefab", bulletPrefab);
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
            cam.orthographicSize = 13.0f; // Double orthographic size to zoom out double
            camGo.transform.position = new Vector3(0f, 15f, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camGo.AddComponent<AudioListener>();

            // Ambient light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.4f);

            // Ambient Music (3D sound)
            var bgmGo = new GameObject("AmbientMusic");
            bgmGo.transform.position = Vector3.zero;
            var bgmAudio = bgmGo.AddComponent<AudioSource>();
            bgmAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Musica de fondo Audiomachine - The Last Stand (No Vocal).mp3");
            bgmAudio.loop = true;
            bgmAudio.volume = 0.35f;
            bgmAudio.spatialBlend = 1.0f; // 3D
            bgmAudio.maxDistance = 45f;
            bgmAudio.minDistance = 5f;
            bgmAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            bgmAudio.playOnAwake = true;

            // Route to Music Mixer Group
            RedesAudioSetup.CreateAudioMixerAndSetup();
            var musicGroup = RedesAudioSetup.GetGroup("Music");
            if (musicGroup != null)
            {
                bgmAudio.outputAudioMixerGroup = musicGroup;
            }
            // bgmAudio.Play(); -> Removed to avoid playing music inside the Unity editor in edit mode (playOnAwake is enough for runtime play mode)

            Debug.Log("[TEST][BUILDER] Entorno creado (suelo, cámara, luz, BGM 3D).");
        }

        private static GameObject BuildPlayer()
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Redes/Prefabs/PlayerNuevo.prefab");
            GameObject playerGo;

            if (prefabAsset != null)
            {
                playerGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                playerGo.transform.position = Vector3.zero;
                playerGo.transform.rotation = Quaternion.identity;
                Debug.Log("[TEST][BUILDER] Instanciado PlayerNuevo.prefab.");
            }
            else
            {
                playerGo = new GameObject("TestPlayer");
                playerGo.transform.position = Vector3.zero;
                Debug.LogWarning("[TEST][BUILDER] PlayerNuevo.prefab no encontrado. Creando fallback.");

                // Create Model fallback
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonSoldiers_demo/models/ToonSoldier_demo.FBX");
                if (modelAsset != null)
                {
                    var modelObj = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, playerGo.transform);
                    modelObj.name = "Model";
                    modelObj.transform.localPosition = Vector3.zero;
                    modelObj.transform.localScale = Vector3.one * 1.5f;
                }
                else
                {
                    var modelObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    modelObj.name = "Model";
                    modelObj.transform.SetParent(playerGo.transform, false);
                    modelObj.transform.localPosition = new Vector3(0, 1f, 0);
                }

                // Create Muzzle fallback
                var muzzle = new GameObject("Muzzle");
                muzzle.transform.SetParent(playerGo.transform, false);
                muzzle.transform.localPosition = new Vector3(0, 1.5f, 0.8f);

                // Create OrigenDeDisparo fallback
                var origen = new GameObject("OrigenDeDisparo");
                origen.transform.SetParent(playerGo.transform, false);
                origen.transform.localPosition = new Vector3(0, 1.5f, 0.8f);
            }

            // Find or setup Animator
            var modelChild = playerGo.transform.Find("Model");
            Animator animator = null;
            if (modelChild != null)
            {
                animator = modelChild.GetComponent<Animator>();
                if (animator == null) animator = modelChild.gameObject.AddComponent<Animator>();
                animator.applyRootMotion = false;
                animator.runtimeAnimatorController = GetOrCreatePlayerAnimator();
            }

            // Safe add components if missing
            var peb = playerGo.GetComponent<PlayerEventBus>();
            if (peb == null) peb = playerGo.AddComponent<PlayerEventBus>();

            var animView = playerGo.GetComponent<PlayerAnimationView>();
            if (animView == null) animView = playerGo.AddComponent<PlayerAnimationView>();

            if (animator != null)
            {
                Assign(animView, "_animator", animator);
                Assign(animView, "_eventBus", peb);
            }

            var tester = playerGo.GetComponent<OfflinePlayerTester>();
            if (tester == null) tester = playerGo.AddComponent<OfflinePlayerTester>();

            // Setup AudioSource for 3D sound if missing
            var audioSource = playerGo.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = playerGo.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f; // 3D Sound
                audioSource.playOnAwake = false;
                audioSource.minDistance = 3f;
                audioSource.maxDistance = 25f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }

            var sfxGroup = RedesAudioSetup.GetGroup("SFX");
            if (sfxGroup != null) audioSource.outputAudioMixerGroup = sfxGroup;

            // Load audio clips
            var shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Disparo de Metralleta (1).mp3");
            var reloadClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Recarga de Metralleta.mp3");
            var deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Lose.wav");
            var footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Caminar.mp3");

            Assign(animView, "_audioSource", audioSource);
            Assign(animView, "_shootSound", shootClip);
            Assign(animView, "_reloadSound", reloadClip);
            Assign(animView, "_deathSound", deathClip);
            Assign(animView, "_footstepSound", footstepClip);

            Assign(tester, "_shootSound", shootClip);
            Assign(tester, "_reloadSound", reloadClip);
            Assign(tester, "_deathSound", deathClip);

            // Setup Collider if missing
            var col = playerGo.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = playerGo.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.35f;
                col.center = new Vector3(0, 0.9f, 0);
            }

            // Setup Ragdoll
            var ragdoll = playerGo.GetComponent<Gameplay.RagdollController>();
            if (ragdoll == null) ragdoll = playerGo.AddComponent<Gameplay.RagdollController>();

            Debug.Log($"[TEST][BUILDER] Player offline creado: EventBus={peb != null}, Animator={animator != null}");
            return playerGo;
        }

        private static GameObject BuildDummy(Vector3 position)
        {
            var dummy = new GameObject("DummyEnemy");
            dummy.transform.position = position;
            dummy.transform.rotation = Quaternion.Euler(0, 180, 0);

            // Visual body
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonSoldiers_demo/models/ToonSoldier_demo.FBX");
            if (modelAsset != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, dummy.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 1.5f;

                // Setup animator to play the Idle animation by default
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.applyRootMotion = false;
                animator.runtimeAnimatorController = GetOrCreatePlayerAnimator();

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
            dummy.AddComponent<Gameplay.RagdollController>();

            // Setup AudioSource for 3D sound
            var audioSource = dummy.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Sound
            audioSource.playOnAwake = false;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 25f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

            // Route to SFX Group in GameMixer
            var sfxGroup = RedesAudioSetup.GetGroup("SFX");
            if (sfxGroup != null)
            {
                audioSource.outputAudioMixerGroup = sfxGroup;
            }

            // Load and wire AudioClips
            var dShoot = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Disparo de Metralleta (1).mp3");
            var dHit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Hit.wav");
            var dDeath = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Lose.wav");

            Assign(dummyComp, "_audioSource", audioSource);
            Assign(dummyComp, "_shootSound", dShoot);
            Assign(dummyComp, "_hitSound", dHit);
            Assign(dummyComp, "_deathSound", dDeath);

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

            // Ammo display (bottom-right) - doubled in size for 1920x1080
            var ammoPanel = CreatePanel(canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-20, 20), new Vector2(440, 160), new Color(0, 0, 0, 0.7f));
            ammoPanel.name = "AmmoPanel";
            ((RectTransform)ammoPanel.transform).pivot = new Vector2(1, 0);

            // Centered ammo text - doubled in size and font size
            var ammoText = CreateText(ammoPanel.transform, "AmmoText", "AMMO: 10/10",
                new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(420, 60), 36, Color.white, TextAnchor.MiddleCenter);
            ammoText.fontStyle = FontStyle.Bold;

            // Create Slider for reload progress
            var sliderGo = new GameObject("ReloadSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(ammoPanel.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.1f, 0.15f);
            sliderRect.anchorMax = new Vector2(0.9f, 0.35f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = Vector2.zero;

            var slider = sliderGo.AddComponent<Slider>();

            // Slider Background
            var sBg = new GameObject("Background", typeof(RectTransform));
            sBg.transform.SetParent(sliderGo.transform, false);
            var sBgImg = sBg.AddComponent<Image>();
            sBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var sBgRect = sBg.GetComponent<RectTransform>();
            sBgRect.anchorMin = Vector2.zero;
            sBgRect.anchorMax = Vector2.one;
            sBgRect.sizeDelta = Vector2.zero;

            // Slider Fill Area
            var sFillArea = new GameObject("Fill Area", typeof(RectTransform));
            sFillArea.transform.SetParent(sliderGo.transform, false);
            var sFillAreaRect = sFillArea.GetComponent<RectTransform>();
            sFillAreaRect.anchorMin = Vector2.zero;
            sFillAreaRect.anchorMax = Vector2.one;
            sFillAreaRect.sizeDelta = Vector2.zero;

            var sFill = new GameObject("Fill", typeof(RectTransform));
            sFill.transform.SetParent(sFillArea.transform, false);
            var sFillImg = sFill.AddComponent<Image>();
            sFillImg.color = Color.yellow;
            var sFillRect = sFill.GetComponent<RectTransform>();
            sFillRect.anchorMin = Vector2.zero;
            sFillRect.anchorMax = Vector2.one;
            sFillRect.sizeDelta = Vector2.zero;

            slider.fillRect = sFillRect;
            slider.targetGraphic = sFillImg;

            // Attach PlayerAmmoView (MVC View)
            var ammoView = ammoText.gameObject.AddComponent<PlayerAmmoView>();
            var peb = player.GetComponent<PlayerEventBus>();
            Assign(ammoView, "_eventBus", peb);
            Assign(ammoView, "_ammoText", ammoText);
            Assign(ammoView, "_reloadSlider", slider);

            // Create CustomCursorView on Canvas
            var cursorView = canvasGo.AddComponent<CustomCursorView>();
            Assign(cursorView, "_eventBus", peb);

            // Load cursor sprites and assign them
            var cBase = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Redes/Art/Textures/CursorBase.png");
            var cShoot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Redes/Art/Textures/CursorShoot.png");
            var cHit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Redes/Art/Textures/CursorHit.png");
            var cReload = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Redes/Art/Textures/CursorReload.png");
            
            Assign(cursorView, "_cursorBase", cBase);
            Assign(cursorView, "_cursorShoot", cShoot);
            Assign(cursorView, "_cursorHit", cHit);
            Assign(cursorView, "_cursorReload", cReload);

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

            // --- PREMIUM DEATH SCREEN UI (MVC) ---
            var deathPanel = CreatePanel(canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.08f, 0.01f, 0.01f, 0.88f));
            deathPanel.name = "DeathScreenPanel";
            deathPanel.SetActive(false);

            // Background Ring (radial guide)
            var bgRingGo = new GameObject("BgRing", typeof(RectTransform));
            bgRingGo.transform.SetParent(deathPanel.transform, false);
            var bgRingRt = bgRingGo.GetComponent<RectTransform>();
            bgRingRt.sizeDelta = new Vector2(300, 300);
            bgRingRt.anchoredPosition = new Vector2(0, 50);
            var bgRingImg = bgRingGo.AddComponent<Image>();
            bgRingImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            bgRingImg.color = new Color(0.2f, 0.05f, 0.05f, 0.5f);

            // Active Radial Circle
            var radialGo = new GameObject("RadialCircle", typeof(RectTransform));
            radialGo.transform.SetParent(deathPanel.transform, false);
            var radialRt = radialGo.GetComponent<RectTransform>();
            radialRt.sizeDelta = new Vector2(300, 300);
            radialRt.anchoredPosition = new Vector2(0, 50);
            var radialImg = radialGo.AddComponent<Image>();
            radialImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            radialImg.color = new Color(0.85f, 0.15f, 0.15f, 0.9f);
            radialImg.type = Image.Type.Filled;
            radialImg.fillMethod = Image.FillMethod.Radial360;
            radialImg.fillOrigin = (int)Image.Origin360.Top;
            radialImg.fillClockwise = true;
            radialImg.fillAmount = 0f;

            // Countdown Text (inside the circle)
            var countTxt = CreateText(deathPanel.transform, "CountdownText", "5.0s",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250, 80), 38, Color.white, TextAnchor.MiddleCenter);
            countTxt.fontStyle = FontStyle.Bold;
            ((RectTransform)countTxt.transform).anchoredPosition = new Vector2(0, 50);

            // Subtitle text below the circle
            var subTxt = CreateText(deathPanel.transform, "SubtitleText", "RAGDOLL DECAY & RESPAWNING...",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600, 50), 20, new Color(0.9f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
            ((RectTransform)subTxt.transform).anchoredPosition = new Vector2(0, -140);

            // MVC Components wire up
            var deathView = deathPanel.AddComponent<Views.DeathScreenView>();
            Assign(deathView, "_panel", deathPanel);
            Assign(deathView, "_radialCircle", radialImg);
            Assign(deathView, "_countdownText", countTxt);

            var deathCtrl = deathPanel.AddComponent<Views.DeathScreenController>();
            Assign(deathCtrl, "_view", deathView);
            Assign(deathCtrl, "_playerEventBus", peb);

            Debug.Log("[TEST][BUILDER] HUD de test creado con Death Screen premium.");
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

