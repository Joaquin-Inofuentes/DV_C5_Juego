using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Redes.Player;
using Redes.Combat;
using Redes.Views;

namespace Redes.EditorTools
{
    /// <summary>
    /// Tools > Redes > 2. Create Prefabs
    ///
    /// Creates the primitive prefabs (Player capsule + Bullet sphere) with ALL
    /// their network components attached. References between components on the
    /// SAME prefab are wired here; cross-asset wiring (bullet prefab into the
    /// player, etc.) is done by "3. Link & Assign All".
    /// </summary>
    public static class RedesPrefabCreator
    {
        private const string PrefabFolder = "Assets/_Redes/Prefabs";
        public const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
        public const string BulletPrefabPath = PrefabFolder + "/Bullet.prefab";
        public const string EntityDisplayViewPrefabPath = PrefabFolder + "/EntityDisplayView.prefab";
        public const string CustomObstaclePrefabPath = PrefabFolder + "/CustomObstacle.prefab";
        public const string BombObstaclePrefabPath = PrefabFolder + "/BombObstacle.prefab";

        // [MenuItem("Tools/Redes/2. Create Prefabs", priority = 2)]
        public static void CreatePrefabs()
        {
            EnsureFolder("Assets/_Redes", "Prefabs");

            CreateGameEventBusAsset();
            CreateBulletPrefab();
            CreatePlayerPrefab();
            CreateEntityDisplayViewPrefab();
            CreateCustomObstaclePrefab();
            CreateBombObstaclePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#9E9E9E>[REDES][BOOT]</color> Prefabs creados.");
        }

        private static void CreateGameEventBusAsset()
        {
            string path = "Assets/_Redes/Scripts/Core/GameEventBus.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Redes.Core.GameEventBus>(path);
            if (existing == null)
            {
                EnsureFolder("Assets/_Redes/Scripts", "Core");
                var asset = ScriptableObject.CreateInstance<Redes.Core.GameEventBus>();
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[REDES][PREFABS] GameEventBus asset created at {path}");
            }
        }

        private static void CreatePlayerPrefab()
        {
            // Root
            var go = new GameObject("Player");

            // Physics + Fusion identity.
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.7f;
            col.center = new Vector3(0, 0.9f, 0); // pivot at feet, center at mid-body

            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();

            // Player systems.
            var net     = go.AddComponent<Player.NetworkPlayer>();
            var move    = go.AddComponent<PlayerMovement>();
            var shoot   = go.AddComponent<PlayerShooting>();
            var hp      = go.AddComponent<PlayerHealth>();
            var ammo    = go.AddComponent<AmmoSystem>();
            var peb     = go.AddComponent<PlayerEventBus>();
            var animV   = go.AddComponent<PlayerAnimationView>();
            var crouch  = go.AddComponent<PlayerCrouch>();     // Mecánica extra: agacharse
            var teleport= go.AddComponent<PlayerTeleport>();  // Mecánica extra: teletransporte

            // Setup AudioSource for 3D sound
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Sound
            audioSource.playOnAwake = false;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 25f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

            // Ensure mixer exists and route to SFX group
            RedesAudioSetup.CreateAudioMixerAndSetup();
            var sfxGroup = RedesAudioSetup.GetGroup("SFX");
            if (sfxGroup != null)
            {
                audioSource.outputAudioMixerGroup = sfxGroup;
            }

            // Setup Ragdoll
            go.AddComponent<Gameplay.RagdollController>();

            // Visual Model
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonSoldiers_demo/models/ToonSoldier_demo.FBX");
            GameObject modelObj;
            Animator animator = null;

            if (modelAsset != null)
            {
                modelObj = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                modelObj.name = "[MA] Player Visual Model";
                modelObj.transform.SetParent(go.transform, false);
                modelObj.transform.localPosition = Vector3.zero; // pivot already at feet in FBX
                modelObj.transform.localRotation = Quaternion.identity;
                modelObj.transform.localScale = Vector3.one * 1.5f; // Match the offline fallback scale so it's visible

                animator = modelObj.GetComponent<Animator>();
                if (animator == null) animator = modelObj.AddComponent<Animator>();
                animator.runtimeAnimatorController = GetOrCreatePlayerAnimator();
            }
            else
            {
                modelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                modelObj.name = "[MA] Player Visual Model";
                modelObj.transform.SetParent(go.transform, false);
                UnityEngine.Object.DestroyImmediate(modelObj.GetComponent<BoxCollider>());
            }

            // Muzzle (where bullets will spawn).
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0, 1.5f, 0.8f); // ~chest height, forward

            // ─── Cargar clips de audio importados ─────────────────────────────────
            // 3 Disparos distintos por jugador (PlayerId determina cuál usa cada uno)
            var shoot1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_Disparo1.wav");
            var shoot2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_Disparo2.wav");
            var shoot3 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_Disparo3.wav");
            // 4 clips de dolor aleatorios al recibir daño
            var ouch1  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_RecibirDano1.wav");
            var ouch2  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_RecibirDano2.wav");
            var ouch3  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_RecibirDano3.wav");
            var ouch4  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_RecibirDano4.wav");
            // Recarga y pasos
            var reloadClip   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/Importados/SFX_Recargar.wav");
            var deathClip    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Lose.wav");
            var footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audios/Caminar.mp3");
            var hitClip      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Hit.wav");

            // Load bullet prefab for PlayerShooting assignment
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
            var bulletPrefabNo = bulletPrefab != null ? bulletPrefab.GetComponent<NetworkObject>() : null;

            // Load global GameEventBus
            var eventBus = AssetDatabase.LoadAssetAtPath<Redes.Core.GameEventBus>("Assets/_Redes/Scripts/Core/GameEventBus.asset");

            // Wire SAME-PREFAB references via SerializedObject.
            AssignRefs(net,   ("_movement", move), ("_shooting", shoot), ("_health", hp),
                              ("_ammo", ammo), ("_eventBus", peb),
                              ("_crouch", crouch), ("_teleport", teleport));  // nuevas mecánicas
            AssignRefs(shoot, ("_ammo", ammo), ("_muzzle", muzzle.transform), ("_projectilePrefab", bulletPrefabNo), ("_eventBus", eventBus));
            AssignRefs(move,  ("_body", rb));
            AssignRefs(hp,    ("_hitSound", hitClip), ("_eventBus", eventBus));
            AssignRefs(ammo,  ("_eventBus", eventBus));

            // Asignar campos escalares de animV
            AssignRefs(animV, ("_animator", animator), ("_eventBus", peb), ("_audioSource", audioSource),
                              ("_reloadSound", reloadClip),
                              ("_deathSound", deathClip), ("_footstepSound", footstepClip));
            // Asignar arrays de clips en animV
            AssignArrayRefs(animV, "_shootSounds",
                new AudioClip[] { shoot1, shoot2, shoot3 });
            AssignArrayRefs(animV, "_ouchSounds",
                new AudioClip[] { ouch1, ouch2, ouch3, ouch4 });

            PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
        }

        /// <summary>Public entry-point so the test scene builder can reuse this.</summary>
        public static AnimatorController GetOrCreateAnimator(string path = null)
        {
            return GetOrCreatePlayerAnimator(path);
        }

        private static AnimatorController GetOrCreatePlayerAnimator(string overridePath = null)
        {
            string path = overridePath ?? "Assets/_Redes/Art/PlayerAnimator.controller";
            
            // Re-create to force update/regeneration
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Redes/Art"))
            {
                AssetDatabase.CreateFolder("Assets/_Redes", "Art");
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // Parameters
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveSpeedMultiplier", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ShootSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            var parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == "MoveSpeedMultiplier") parameters[i].defaultFloat = 1f;
                if (parameters[i].name == "ShootSpeed") parameters[i].defaultFloat = 1f;
            }
            controller.parameters = parameters;

            // Load clips
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ToonSoldiers_demo/animation/assault_combat_idle.FBX");
            var runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ToonSoldiers_demo/animation/assault_combat_run.FBX");
            var shootClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ToonSoldiers_demo/animation/assault_combat_shoot.FBX");

            // --- Layer 0: Base Layer (Movement & Death) ---
            var baseStateMachine = controller.layers[0].stateMachine;

            var baseIdle = baseStateMachine.AddState("Idle");
            baseIdle.motion = idleClip;

            var baseRun = baseStateMachine.AddState("Run");
            baseRun.motion = runClip;
            baseRun.speedParameterActive = true;
            baseRun.speedParameter = "MoveSpeedMultiplier";

            var baseDead = baseStateMachine.AddState("Dead");

            baseStateMachine.defaultState = baseIdle;

            // Transitions for Base Layer
            var idleToRun = baseIdle.AddTransition(baseRun);
            idleToRun.hasExitTime = false;
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "MoveSpeed");

            var runToIdle = baseRun.AddTransition(baseIdle);
            runToIdle.hasExitTime = false;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "MoveSpeed");

            var anyToDead = baseStateMachine.AddAnyStateTransition(baseDead);
            anyToDead.hasExitTime = false;
            anyToDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            var deadToIdle = baseDead.AddTransition(baseIdle);
            deadToIdle.hasExitTime = false;
            deadToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");

            // --- Avatar Mask for Upper Body ---
            string maskPath = "Assets/_Redes/Art/PlayerUpperBodyMask.mask";
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                
                // Mask out legs/feet, keep spine, head, and arms
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

                AssetDatabase.CreateAsset(mask, maskPath);
            }

            // --- Layer 1: Shooting Layer (Upper Body Masked) ---
            controller.AddLayer("Shooting");
            
            var layers = controller.layers;
            var shootingLayer = layers[1];
            shootingLayer.name = "Shooting";
            shootingLayer.avatarMask = mask;
            shootingLayer.defaultWeight = 1f;
            shootingLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            
            var shootingStateMachine = shootingLayer.stateMachine;
            
            var stateEmpty = shootingStateMachine.AddState("Empty");
            stateEmpty.motion = null;
            
            var stateShoot = shootingStateMachine.AddState("Shoot");
            stateShoot.motion = shootClip;
            stateShoot.speedParameterActive = true;
            stateShoot.speedParameter = "ShootSpeed";
            
            shootingStateMachine.defaultState = stateEmpty;

            // Transitions in Shooting Layer
            var emptyToShoot = stateEmpty.AddTransition(stateShoot);
            emptyToShoot.hasExitTime = false;
            emptyToShoot.duration = 0f; // Snap instantly into shooting pose
            emptyToShoot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");

            var shootToEmpty = stateShoot.AddTransition(stateEmpty);
            shootToEmpty.hasExitTime = true;
            shootToEmpty.exitTime = 1f; // Let the FULL animation play before returning
            shootToEmpty.duration = 0f; // Snap back instantly — no blend

            var shootToEmptyOnDead = stateShoot.AddTransition(stateEmpty);
            shootToEmptyOnDead.hasExitTime = false;
            shootToEmptyOnDead.duration = 0.05f;
            shootToEmptyOnDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            controller.layers = layers;

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void CreateBulletPrefab()
        {
            var go = new GameObject("Bullet");

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Redes/Art/Models/BulletModel.obj");
            if (modelAsset != null)
            {
                var modelObj = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                modelObj.name = "Model";
                modelObj.transform.SetParent(go.transform, false);
                modelObj.transform.localScale = Vector3.one * 0.5f;
            }
            else
            {
                var modelObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                modelObj.name = "Model";
                modelObj.transform.SetParent(go.transform, false);
                modelObj.transform.localScale = Vector3.one * 0.3f;
                UnityEngine.Object.DestroyImmediate(modelObj.GetComponent<SphereCollider>());
            }

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();
            go.AddComponent<Projectile>();

            PrefabUtility.SaveAsPrefabAsset(go, BulletPrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void CreateEntityDisplayViewPrefab()
        {
            var go = new GameObject("EntityDisplayView", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 15f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(go.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = Color.green;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var nicknameGo = new GameObject("NicknameText", typeof(RectTransform));
            nicknameGo.transform.SetParent(go.transform, false);
            var nicknameRect = nicknameGo.GetComponent<RectTransform>();
            nicknameRect.anchorMin = new Vector2(0f, 1f);
            nicknameRect.anchorMax = new Vector2(1f, 1f);
            nicknameRect.pivot = new Vector2(0.5f, 0f);
            nicknameRect.anchoredPosition = new Vector2(0f, 10f); // Adjust position slightly higher for larger font
            nicknameRect.sizeDelta = new Vector2(240f, 40f);

            var nicknameTxt = nicknameGo.AddComponent<Text>();
            nicknameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nicknameTxt.fontSize = 24;
            nicknameTxt.color = Color.white;
            nicknameTxt.alignment = TextAnchor.MiddleCenter;
            nicknameTxt.fontStyle = FontStyle.Bold;

            // --- Reload Area ---
            var reloadAreaGo = new GameObject("ReloadArea", typeof(RectTransform));
            reloadAreaGo.transform.SetParent(go.transform, false);
            var reloadAreaRect = reloadAreaGo.GetComponent<RectTransform>();
            reloadAreaRect.anchorMin = new Vector2(0.5f, 1f);
            reloadAreaRect.anchorMax = new Vector2(0.5f, 1f);
            reloadAreaRect.pivot = new Vector2(0.5f, 0f);
            reloadAreaRect.anchoredPosition = new Vector2(0f, 40f); // slightly above nickname
            reloadAreaRect.sizeDelta = new Vector2(40f, 40f);

            var reloadBgGo = new GameObject("ReloadBackground", typeof(RectTransform));
            reloadBgGo.transform.SetParent(reloadAreaGo.transform, false);
            var reloadBgRect = reloadBgGo.GetComponent<RectTransform>();
            reloadBgRect.anchorMin = Vector2.zero;
            reloadBgRect.anchorMax = Vector2.one;
            reloadBgRect.sizeDelta = Vector2.zero;
            var reloadBgImg = reloadBgGo.AddComponent<Image>();
            reloadBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            reloadBgImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            reloadBgImg.type = Image.Type.Simple;

            var reloadFillGo = new GameObject("ReloadFill", typeof(RectTransform));
            reloadFillGo.transform.SetParent(reloadAreaGo.transform, false);
            var reloadFillRect = reloadFillGo.GetComponent<RectTransform>();
            reloadFillRect.anchorMin = Vector2.zero;
            reloadFillRect.anchorMax = Vector2.one;
            reloadFillRect.sizeDelta = Vector2.zero;
            var reloadFillImg = reloadFillGo.AddComponent<Image>();
            reloadFillImg.color = Color.yellow;
            reloadFillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            reloadFillImg.type = Image.Type.Filled;
            reloadFillImg.fillMethod = Image.FillMethod.Radial360;
            reloadFillImg.fillOrigin = (int)Image.Origin360.Top;
            reloadFillImg.fillAmount = 0f;
            reloadFillImg.fillClockwise = true;

            var view = go.AddComponent<Views.EntityDisplayView>();
            AssignRefs(view, ("_healthSlider", (Object)slider), ("_nicknameText", (Object)nicknameTxt), 
                             ("_reloadFillImage", (Object)reloadFillImg), ("_reloadArea", (Object)reloadAreaGo));

            PrefabUtility.SaveAsPrefabAsset(go, EntityDisplayViewPrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void CreateCustomObstaclePrefab()
        {
            var go = new GameObject("CustomObstacle");
            go.tag = "Obstacle";
            go.layer = 6; // Obstacles / Obstaculos

            // Add standard cube visuals
            var modelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelObj.name = "Visuals";
            modelObj.transform.SetParent(go.transform, false);
            // Destroy default box collider on primitive to manage it on root
            var primCollider = modelObj.GetComponent<BoxCollider>();
            if (primCollider != null) Object.DestroyImmediate(primCollider);

            // Add physics collider on root
            var boxCol = go.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one;

            // Make it dynamic standard obstacle
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            PrefabUtility.SaveAsPrefabAsset(go, CustomObstaclePrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void CreateBombObstaclePrefab()
        {
            var go = new GameObject("BombObstacle");
            go.tag = "Obstacle";
            go.layer = 6; // Obstacles / Obstaculos

            // Fusion core
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();

            // Solid blocker box collider
            var boxCol = go.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one;

            // Explosion SphereCollider trigger (double size, so radius is 2 for diameter 4)
            var sphereCol = go.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 2.0f;
            sphereCol.enabled = false;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // 3D Cube Visuals
            var cubeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeVisual.name = "CubeVisual";
            cubeVisual.transform.SetParent(go.transform, false);
            var primCollider = cubeVisual.GetComponent<BoxCollider>();
            if (primCollider != null) Object.DestroyImmediate(primCollider);

            // Setup renderer with orange color initially for bomb
            var renderer = cubeVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var orangeMat = new Material(Shader.Find("Standard"));
                orangeMat.color = new Color(0.85f, 0.4f, 0.1f);
                renderer.sharedMaterial = orangeMat;
            }

            // Get default particle material
            var defaultParticleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            if (defaultParticleMat == null)
            {
                defaultParticleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            }

            // Fire particles placeholder
            var fireGo = new GameObject("FireParticles");
            fireGo.transform.SetParent(go.transform, false);
            var firePs = fireGo.AddComponent<ParticleSystem>();
            var firePsMain = firePs.main;
            firePsMain.duration = 2.0f;
            firePsMain.loop = true;
            firePsMain.startColor = new Color(1.0f, 0.3f, 0f, 1.0f);
            var firePsEmission = firePs.emission;
            firePsEmission.rateOverTime = 15;
            var fireRend = fireGo.GetComponent<ParticleSystemRenderer>();
            if (fireRend != null && defaultParticleMat != null) fireRend.sharedMaterial = defaultParticleMat;
            fireGo.SetActive(true); // Must be active, but PS plays when triggered

            // Explosion particles placeholder
            var explosionGo = new GameObject("ExplosionParticles");
            explosionGo.transform.SetParent(go.transform, false);
            var explosionPs = explosionGo.AddComponent<ParticleSystem>();
            var expMain = explosionPs.main;
            expMain.duration = 1.0f;
            expMain.loop = false;
            expMain.startSpeed = 8f;
            expMain.startSize = 0.5f;
            expMain.startColor = new Color(1f, 0.2f, 0f, 1.0f);
            var expEmission = explosionPs.emission;
            expEmission.SetBursts(new[] { new ParticleSystem.Burst(0, 30) });
            var expRend = explosionGo.GetComponent<ParticleSystemRenderer>();
            if (expRend != null && defaultParticleMat != null) expRend.sharedMaterial = defaultParticleMat;
            explosionGo.SetActive(false); // Enable and play on explode

            // AudioSource
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Sound
            audioSource.playOnAwake = false;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 25f;
            var sfxGroup = RedesAudioSetup.GetGroup("SFX");
            if (sfxGroup != null) audioSource.outputAudioMixerGroup = sfxGroup;

            // Load SFX_Bomba.mp3
            var bombSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Audio/SFX_Bomba.mp3");
            if (bombSfx == null)
            {
                // Try alternate local path
                bombSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Bomba.mp3");
            }

            // Script and wire properties
            var bombComponent = go.AddComponent<Redes.Combat.DestructibleBombObstacle>();
            
            // Assign fields via SerializedObject
            AssignRefs(bombComponent, new (string field, Object value)[] {
                ("_boxCollider", (Object)boxCol),
                ("_explosionTrigger", (Object)sphereCol),
                ("_fireParticles", (Object)firePs),
                ("_explosionParticles", (Object)explosionPs),
                ("_audioSource", (Object)audioSource),
                ("_visualCube", (Object)cubeVisual.transform),
                ("_explosionSound", (Object)bombSfx)
            });

            PrefabUtility.SaveAsPrefabAsset(go, BombObstaclePrefabPath);
            Object.DestroyImmediate(go);
        }

        // Assigns (fieldName, value) pairs onto a component via SerializedObject.
        private static void AssignRefs(Object target, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var prop = so.FindProperty(field);
                if (prop != null) prop.objectReferenceValue = value;
                else Debug.LogWarning($"[REDES][LINK] Campo '{field}' no encontrado en {target.GetType().Name}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Asigna un array de AudioClips a un campo serializado de tipo AudioClip[].</summary>
        private static void AssignArrayRefs(Object target, string fieldName, AudioClip[] clips)
        {
            if (target == null || clips == null) return;
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[REDES][LINK] Array field '{fieldName}' no encontrado en {target.GetType().Name}");
                return;
            }
            prop.ClearArray();
            prop.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.objectReferenceValue = clips[i];
                if (clips[i] == null)
                    Debug.LogWarning($"[REDES][LINK] Clip[{i}] en '{fieldName}' es null — verificá la ruta de Audio/Importados/");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}

