using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using DebugSystem;

namespace DebugSystem.Editor
{
    public class CreateScenesAndPrefabsMenu
    {
        private static Font GetDefaultFont()
        {
            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch {}

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch {}
            }

            if (font == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0)
                {
                    font = fonts[0];
                }
            }
            return font;
        }

        // [MenuItem("Tools/Pruebas/Crear Escenas y Prefabs")]
        public static void GenerateProjectSetup()
        {
            try
            {
                Debug.Log("[Setup] Iniciando la generación de prefabs y escenas...");

                // 0. Asegurar que los tags necesarios existen
                EnsureTagExists("Obstacle");

                // 1. Asegurar directorios
                EnsureDirectory("Assets/Prefabs");
                EnsureDirectory("Assets/Scenes");

                // 2. Crear y guardar el prefab de la bala (Bullet)
                GameObject bulletGo = new GameObject("BulletPrefab");
                bulletGo.SetActive(false);

                SpriteRenderer bulletRenderer = bulletGo.AddComponent<SpriteRenderer>();
                bulletRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                bulletRenderer.color = Color.yellow;
                bulletGo.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

                CircleCollider2D bulletCol = bulletGo.AddComponent<CircleCollider2D>();
                bulletCol.isTrigger = true;
                Rigidbody2D bulletRb = bulletGo.AddComponent<Rigidbody2D>();
                bulletRb.bodyType = RigidbodyType2D.Kinematic;

                Bullet bulletComponent = bulletGo.AddComponent<Bullet>();
                bulletComponent.Speed = 20f;
                bulletComponent.Damage = 20f;
                bulletComponent.Lifetime = 3f;

                string bulletPrefabPath = "Assets/Prefabs/BulletPrefab.prefab";
                GameObject bulletPrefab = PrefabUtility.SaveAsPrefabAsset(bulletGo, bulletPrefabPath);
                UnityEngine.Object.DestroyImmediate(bulletGo);
                Debug.Log($"[Setup] Prefab creado en: {bulletPrefabPath}");

                // 3. Crear y guardar el prefab del jugador (Player)
                GameObject playerGo = new GameObject("PlayerPrefab");
                playerGo.SetActive(false);
                playerGo.transform.position = Vector3.zero;

                PlayerModel playerModel = playerGo.AddComponent<PlayerModel>();
                playerGo.AddComponent<PlayerView>();
                playerGo.AddComponent<PlayerController>();

                BoxCollider2D playerCol = playerGo.AddComponent<BoxCollider2D>();
                playerCol.size = new Vector2(1f, 1f);
                Rigidbody2D playerRb = playerGo.AddComponent<Rigidbody2D>();
                playerRb.bodyType = RigidbodyType2D.Kinematic;

                // Visuals
                GameObject visualsGo = new GameObject("Visuals");
                visualsGo.transform.SetParent(playerGo.transform, false);
                SpriteRenderer playerRenderer = visualsGo.AddComponent<SpriteRenderer>();
                playerRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                playerRenderer.color = Color.cyan;
                visualsGo.AddComponent<SimpleAnimator>();

                // Health Bar
                GameObject healthGo = new GameObject("FloatingHealthBar");
                healthGo.transform.SetParent(playerGo.transform, false);
                // Ajustar posicion a 1.5 arriba (pero la escala de player puede afectar si no es riguroso)
                healthGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                healthGo.AddComponent<FloatingHealthBar>();

                GameObject playerWeaponGo = new GameObject("WeaponMount");
                playerWeaponGo.transform.SetParent(playerGo.transform);
                playerWeaponGo.transform.localPosition = new Vector3(0.6f, 0f, 0f);
                Weapon playerWeapon = playerWeaponGo.AddComponent<Weapon>();
                playerWeapon.WeaponName = "Pistol";
                playerWeapon.ClipSize = 12;
                playerWeapon.MaxReserve = 24;

                GameObject playerAimGo = new GameObject("AimIndicator");
                playerAimGo.transform.SetParent(playerWeaponGo.transform); // Attach aim to weapon
                playerAimGo.transform.localPosition = new Vector3(1.0f, 0f, 0f);
                SpriteRenderer aimRenderer = playerAimGo.AddComponent<SpriteRenderer>();
                aimRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                aimRenderer.color = Color.red;
                playerAimGo.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

                string playerPrefabPath = "Assets/Prefabs/PlayerPrefab.prefab";
                GameObject playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerGo, playerPrefabPath);
                UnityEngine.Object.DestroyImmediate(playerGo);
                Debug.Log($"[Setup] Prefab creado en: {playerPrefabPath}");

                // 4. Crear prefab del botón de sala
                GameObject buttonGo = new GameObject("RoomButtonPrefab");
                buttonGo.SetActive(false);
                buttonGo.AddComponent<RectTransform>().sizeDelta = new Vector2(400f, 80f);
                buttonGo.AddComponent<CanvasRenderer>();
                Image btnImg = buttonGo.AddComponent<Image>();
                btnImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                btnImg.type = Image.Type.Sliced;
                buttonGo.AddComponent<Button>();

                GameObject btnTextGo = new GameObject("Text");
                btnTextGo.transform.SetParent(buttonGo.transform);
                RectTransform textRt = btnTextGo.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                btnTextGo.AddComponent<CanvasRenderer>();
                Text btnText = btnTextGo.AddComponent<Text>();
                btnText.font = GetDefaultFont();
                btnText.color = Color.black;
                btnText.fontSize = 24;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.text = "Sala de Test";

                string buttonPrefabPath = "Assets/Prefabs/RoomButtonPrefab.prefab";
                GameObject buttonPrefab = PrefabUtility.SaveAsPrefabAsset(buttonGo, buttonPrefabPath);
                UnityEngine.Object.DestroyImmediate(buttonGo);
                Debug.Log($"[Setup] Prefab de Botón creado en: {buttonPrefabPath}");

                // 5. Crear Escena del Juego (Scene_Game)
                CreateGameScene(bulletPrefab, playerPrefab, buttonPrefab, "Assets/Scenes/Scene_Game.unity");

                // 6. Crear Escena de Tests (Scene_DebugTests)
                CreateTestScene(bulletPrefab, playerPrefab, buttonPrefab, "Assets/Scenes/Scene_DebugTests.unity");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[Setup] Proceso completado exitosamente. Se crearon los prefabs y las 2 escenas.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Setup Exception Global] Error durante la creación de escenas/prefabs: {ex.Message}\nStack Trace:\n{ex.StackTrace}");
            }
        }

        // [MenuItem("Tools/Pruebas/Corregir")]
        public static void FixAndConfigureScene()
        {
            try
            {
                Debug.Log("[Corregir] Iniciando corrección en la escena activa...");
                EnsureTagExists("Obstacle");

                string bulletPath = "Assets/Prefabs/BulletPrefab.prefab";
                string playerPath = "Assets/Prefabs/PlayerPrefab.prefab";
                string buttonPath = "Assets/Prefabs/RoomButtonPrefab.prefab";

                GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
                GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(buttonPath);

                if (bulletPrefab == null || playerPrefab == null || buttonPrefab == null)
                {
                    Debug.LogWarning("[Corregir] Faltan prefabs. Generándolos primero...");
                    GenerateProjectSetup();
                    bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
                    playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
                    buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(buttonPath);
                }

                Scene currentScene = EditorSceneManager.GetActiveScene();
                // Limpiar objetos existentes para reestructurar
                var rootObjects = currentScene.GetRootGameObjects();
                foreach (var obj in rootObjects)
                {
                    if (obj.name != "Main Camera" && obj.name != "Directional Light")
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                }

                SetupBaseScene(currentScene, bulletPrefab, playerPrefab, buttonPrefab, currentScene.name.Contains("Test"));
                EditorSceneManager.MarkSceneDirty(currentScene);
                Debug.Log("[Corregir] Escena activa corregida y organizada correctamente.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Corregir Exception] Error al corregir la escena: {ex.Message}");
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void CreateGameScene(GameObject bulletPrefab, GameObject playerPrefab, GameObject buttonPrefab, string savePath)
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = "Scene_Game";

            SetupBaseScene(newScene, bulletPrefab, playerPrefab, buttonPrefab, false);

            EditorSceneManager.MarkSceneDirty(newScene);
            EditorSceneManager.SaveScene(newScene, savePath);
            Debug.Log($"[Setup] Escena de juego creada y guardada en: {savePath}");
        }

        private static void CreateTestScene(GameObject bulletPrefab, GameObject playerPrefab, GameObject buttonPrefab, string savePath)
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = "Scene_DebugTests";

            SetupBaseScene(newScene, bulletPrefab, playerPrefab, buttonPrefab, true);

            EditorSceneManager.MarkSceneDirty(newScene);
            EditorSceneManager.SaveScene(newScene, savePath);
            Debug.Log($"[Setup] Escena de tests creada y guardada en: {savePath}");
        }

        private static void SetupBaseScene(Scene scene, GameObject bulletPrefab, GameObject playerPrefab, GameObject buttonPrefab, bool isTestScene)
        {
            // Ajustar Cámara
            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Camera cam = defaultCamera.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.black;
                }
            }

            // Contenedor de Managers
            GameObject managersGo = new GameObject("Managers");
            GameManager gm = managersGo.AddComponent<GameManager>();
            DebugLogger logger = managersGo.AddComponent<DebugLogger>();
            managersGo.AddComponent<GameLoopReferee>();

            if (isTestScene)
            {
                logger.IsTestMode = true;
            }

            // PoolManager
            GameObject poolGo = new GameObject("PoolManager");
            poolGo.transform.SetParent(managersGo.transform);
            BulletPool bulletPool = poolGo.AddComponent<BulletPool>();
            poolGo.AddComponent<EffectPool>(); // Add Effect Pool

            // Asignar el prefab de bala al BulletPool
            if (bulletPrefab != null)
            {
                Bullet bulletComponent = bulletPrefab.GetComponent<Bullet>();
                SerializedObject poolSO = new SerializedObject(bulletPool);
                poolSO.FindProperty("bulletPrefab").objectReferenceValue = bulletComponent;
                poolSO.ApplyModifiedProperties();
            }

            // Entorno / Obstáculo
            GameObject environmentGo = new GameObject("Environment");
            GameObject wall = new GameObject("Wall");
            wall.transform.SetParent(environmentGo.transform);
            wall.transform.position = new Vector3(0f, 6f, 0f);
            wall.transform.localScale = new Vector3(10f, 1f, 1f);
            BoxCollider2D wallCol = wall.AddComponent<BoxCollider2D>();
            wallCol.isTrigger = true;
            wall.tag = "Obstacle";

            SpriteRenderer wallRenderer = wall.AddComponent<SpriteRenderer>();
            wallRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            wallRenderer.color = Color.gray;

            // Configurar Spawn Points
            GameObject spawn1 = new GameObject("SpawnPoint1");
            spawn1.transform.position = new Vector3(-4f, 0f, 0f);
            
            GameObject spawn2 = new GameObject("SpawnPoint2");
            spawn2.transform.position = new Vector3(4f, 0f, 0f);

            gm.playerPrefab = playerPrefab;
            gm.spawnPoint1 = spawn1.transform;
            gm.spawnPoint2 = spawn2.transform;

            // --- GENERACIÓN DEL CANVAS DE UI ---
            GameObject canvasGo = new GameObject("Canvas_UI");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Event System
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ScreenManager Component
            ScreenManager screenManager = canvasGo.AddComponent<ScreenManager>();

            // Panel de Lobby
            GameObject lobbyPanel = new GameObject("LobbyPanel");
            lobbyPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform lobbyRt = lobbyPanel.AddComponent<RectTransform>();
            lobbyRt.anchorMin = Vector2.zero;
            lobbyRt.anchorMax = Vector2.one;
            lobbyRt.sizeDelta = Vector2.zero;
            lobbyPanel.AddComponent<CanvasRenderer>();
            Image lobbyImg = lobbyPanel.AddComponent<Image>();
            lobbyImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Título de Lobby
            GameObject titleGo = new GameObject("LobbyTitle");
            titleGo.transform.SetParent(lobbyPanel.transform, false);
            RectTransform titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.9f);
            titleRt.anchorMax = new Vector2(0.5f, 0.9f);
            titleRt.sizeDelta = new Vector2(800f, 100f);
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = GetDefaultFont();
            titleTxt.fontSize = 50;
            titleTxt.color = Color.white;
            titleTxt.text = "LOBBY - MULTIPLAYER";
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Input Username
            GameObject inputGo = new GameObject("UsernameInput");
            inputGo.transform.SetParent(lobbyPanel.transform, false);
            RectTransform inputRt = inputGo.AddComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.5f, 0.75f);
            inputRt.anchorMax = new Vector2(0.5f, 0.75f);
            inputRt.sizeDelta = new Vector2(400f, 60f);
            Image inputImg = inputGo.AddComponent<Image>();
            inputImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            inputImg.type = Image.Type.Sliced;
            InputField inputField = inputGo.AddComponent<InputField>();

            GameObject inputTextGo = new GameObject("Text");
            inputTextGo.transform.SetParent(inputGo.transform, false);
            RectTransform inputTextRt = inputTextGo.AddComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(10, 5);
            inputTextRt.offsetMax = new Vector2(-10, -5);
            Text inputTxt = inputTextGo.AddComponent<Text>();
            inputTxt.font = GetDefaultFont();
            inputTxt.fontSize = 28;
            inputTxt.color = Color.black;
            inputField.textComponent = inputTxt;
            inputField.text = "PlayerName";

            // Botón Hostear
            GameObject hostBtnGo = new GameObject("HostButton");
            hostBtnGo.transform.SetParent(lobbyPanel.transform, false);
            RectTransform hostBtnRt = hostBtnGo.AddComponent<RectTransform>();
            hostBtnRt.anchorMin = new Vector2(0.5f, 0.62f);
            hostBtnRt.anchorMax = new Vector2(0.5f, 0.62f);
            hostBtnRt.sizeDelta = new Vector2(400f, 80f);
            Image hostBtnImg = hostBtnGo.AddComponent<Image>();
            hostBtnImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            hostBtnImg.type = Image.Type.Sliced;
            Button hostBtn = hostBtnGo.AddComponent<Button>();

            GameObject hostBtnTextGo = new GameObject("Text");
            hostBtnTextGo.transform.SetParent(hostBtnGo.transform, false);
            RectTransform hostBtnTextRt = hostBtnTextGo.AddComponent<RectTransform>();
            hostBtnTextRt.anchorMin = Vector2.zero;
            hostBtnTextRt.anchorMax = Vector2.one;
            Text hostBtnText = hostBtnTextGo.AddComponent<Text>();
            hostBtnText.font = GetDefaultFont();
            hostBtnText.fontSize = 32;
            hostBtnText.color = Color.black;
            hostBtnText.alignment = TextAnchor.MiddleCenter;
            hostBtnText.text = "CREAR SALA (HOST)";

            // Botón Join
            GameObject joinBtnGo = new GameObject("JoinButton");
            joinBtnGo.transform.SetParent(lobbyPanel.transform, false);
            RectTransform joinBtnRt = joinBtnGo.AddComponent<RectTransform>();
            joinBtnRt.anchorMin = new Vector2(0.5f, 0.50f);
            joinBtnRt.anchorMax = new Vector2(0.5f, 0.50f);
            joinBtnRt.sizeDelta = new Vector2(400f, 80f);
            Image joinBtnImg = joinBtnGo.AddComponent<Image>();
            joinBtnImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            joinBtnImg.type = Image.Type.Sliced;
            Button joinBtn = joinBtnGo.AddComponent<Button>();

            GameObject joinBtnTextGo = new GameObject("Text");
            joinBtnTextGo.transform.SetParent(joinBtnGo.transform, false);
            RectTransform joinBtnTextRt = joinBtnTextGo.AddComponent<RectTransform>();
            joinBtnTextRt.anchorMin = Vector2.zero;
            joinBtnTextRt.anchorMax = Vector2.one;
            Text joinBtnText = joinBtnTextGo.AddComponent<Text>();
            joinBtnText.font = GetDefaultFont();
            joinBtnText.fontSize = 32;
            joinBtnText.color = Color.black;
            joinBtnText.alignment = TextAnchor.MiddleCenter;
            joinBtnText.text = "UNIRSE A SALA";

            // Contenedor de Botones de Sala
            GameObject containerGo = new GameObject("RoomListContainer");
            containerGo.transform.SetParent(lobbyPanel.transform, false);
            RectTransform containerRt = containerGo.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.35f);
            containerRt.anchorMax = new Vector2(0.5f, 0.35f);
            containerRt.sizeDelta = new Vector2(500f, 400f);
            VerticalLayoutGroup vlg = containerGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = 15f;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Panel de HUD
            GameObject hudPanel = new GameObject("HUDPanel");
            hudPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform hudRt = hudPanel.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.sizeDelta = Vector2.zero;
            hudPanel.AddComponent<CanvasRenderer>();

            // Texto Vida
            GameObject hpGo = new GameObject("HpText");
            hpGo.transform.SetParent(hudPanel.transform, false);
            RectTransform hpRt = hpGo.AddComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0f, 1f);
            hpRt.anchorMax = new Vector2(0f, 1f);
            hpRt.pivot = new Vector2(0f, 1f);
            hpRt.anchoredPosition = new Vector2(20f, -20f);
            hpRt.sizeDelta = new Vector2(600f, 60f);
            Text hpTxt = hpGo.AddComponent<Text>();
            hpTxt.font = GetDefaultFont();
            hpTxt.fontSize = 32;
            hpTxt.color = Color.green;
            hpTxt.alignment = TextAnchor.MiddleLeft;
            hpTxt.text = "HP Jugador: 100 / Escudo: 0";

            // Texto Munición
            GameObject ammoGo = new GameObject("AmmoText");
            ammoGo.transform.SetParent(hudPanel.transform, false);
            RectTransform ammoRt = ammoGo.AddComponent<RectTransform>();
            ammoRt.anchorMin = new Vector2(0f, 1f);
            ammoRt.anchorMax = new Vector2(0f, 1f);
            ammoRt.pivot = new Vector2(0f, 1f);
            ammoRt.anchoredPosition = new Vector2(20f, -80f);
            ammoRt.sizeDelta = new Vector2(600f, 60f);
            Text ammoTxt = ammoGo.AddComponent<Text>();
            ammoTxt.font = GetDefaultFont();
            ammoTxt.fontSize = 32;
            ammoTxt.color = Color.yellow;
            ammoTxt.alignment = TextAnchor.MiddleLeft;
            ammoTxt.text = "Arma: Pistol | Munición: 12/12";

            // Texto Estado
            GameObject statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(hudPanel.transform, false);
            RectTransform statusRt = statusGo.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 1f);
            statusRt.anchorMax = new Vector2(0.5f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0f, -20f);
            statusRt.sizeDelta = new Vector2(600f, 60f);
            Text statusTxt = statusGo.AddComponent<Text>();
            statusTxt.font = GetDefaultFont();
            statusTxt.fontSize = 28;
            statusTxt.color = Color.cyan;
            statusTxt.alignment = TextAnchor.UpperCenter;
            statusTxt.text = "Estado: Lobby";

            // Enlazar todo al ScreenManager
            screenManager.lobbyPanel = lobbyPanel;
            screenManager.hudPanel = hudPanel;
            screenManager.usernameInput = inputField;
            screenManager.createRoomButton = hostBtn;
            screenManager.joinRoomButton = joinBtn;
            screenManager.roomListContainer = containerGo.transform;
            screenManager.roomButtonPrefab = buttonPrefab;
            screenManager.playerHpText = hpTxt;
            screenManager.playerAmmoText = ammoTxt;
            screenManager.currentStatusText = statusTxt;

            // GameOver Panel
            GameObject gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform gameOverRt = gameOverPanel.AddComponent<RectTransform>();
            gameOverRt.anchorMin = Vector2.zero;
            gameOverRt.anchorMax = Vector2.one;
            gameOverRt.sizeDelta = Vector2.zero;
            gameOverPanel.AddComponent<CanvasRenderer>();
            Image gameOverImg = gameOverPanel.AddComponent<Image>();
            gameOverImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            gameOverPanel.SetActive(false);

            GameObject goTitleGo = new GameObject("TitleText");
            goTitleGo.transform.SetParent(gameOverPanel.transform, false);
            RectTransform goTitleRt = goTitleGo.AddComponent<RectTransform>();
            goTitleRt.anchorMin = new Vector2(0.5f, 0.7f);
            goTitleRt.anchorMax = new Vector2(0.5f, 0.7f);
            goTitleRt.sizeDelta = new Vector2(800f, 100f);
            Text goTitleTxt = goTitleGo.AddComponent<Text>();
            goTitleTxt.font = GetDefaultFont();
            goTitleTxt.fontSize = 60;
            goTitleTxt.alignment = TextAnchor.MiddleCenter;

            GameObject goDetailsGo = new GameObject("DetailsText");
            goDetailsGo.transform.SetParent(gameOverPanel.transform, false);
            RectTransform goDetailsRt = goDetailsGo.AddComponent<RectTransform>();
            goDetailsRt.anchorMin = new Vector2(0.5f, 0.5f);
            goDetailsRt.anchorMax = new Vector2(0.5f, 0.5f);
            goDetailsRt.sizeDelta = new Vector2(800f, 100f);
            Text goDetailsTxt = goDetailsGo.AddComponent<Text>();
            goDetailsTxt.font = GetDefaultFont();
            goDetailsTxt.fontSize = 32;
            goDetailsTxt.color = Color.white;
            goDetailsTxt.alignment = TextAnchor.MiddleCenter;

            GameObject restartBtnGo = new GameObject("RestartButton");
            restartBtnGo.transform.SetParent(gameOverPanel.transform, false);
            RectTransform restartBtnRt = restartBtnGo.AddComponent<RectTransform>();
            restartBtnRt.anchorMin = new Vector2(0.5f, 0.3f);
            restartBtnRt.anchorMax = new Vector2(0.5f, 0.3f);
            restartBtnRt.sizeDelta = new Vector2(300f, 80f);
            Image restartBtnImg = restartBtnGo.AddComponent<Image>();
            restartBtnImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            restartBtnImg.type = Image.Type.Sliced;
            Button restartBtn = restartBtnGo.AddComponent<Button>();

            GameObject restartBtnTextGo = new GameObject("Text");
            restartBtnTextGo.transform.SetParent(restartBtnGo.transform, false);
            RectTransform restartBtnTextRt = restartBtnTextGo.AddComponent<RectTransform>();
            restartBtnTextRt.anchorMin = Vector2.zero;
            restartBtnTextRt.anchorMax = Vector2.one;
            Text restartBtnText = restartBtnTextGo.AddComponent<Text>();
            restartBtnText.font = GetDefaultFont();
            restartBtnText.fontSize = 32;
            restartBtnText.color = Color.black;
            restartBtnText.alignment = TextAnchor.MiddleCenter;
            restartBtnText.text = "REINICIAR";

            screenManager.gameOverPanel = gameOverPanel;
            screenManager.gameOverTitleText = goTitleTxt;
            screenManager.gameOverDetailsText = goDetailsTxt;
            screenManager.restartButton = restartBtn;

            // --- PREMIUM DEATH SCREEN UI (MVC) ---
            GameObject deathPanel = new GameObject("DeathScreenPanel");
            deathPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform deathRt = deathPanel.AddComponent<RectTransform>();
            deathRt.anchorMin = Vector2.zero;
            deathRt.anchorMax = Vector2.one;
            deathRt.sizeDelta = Vector2.zero;
            deathPanel.AddComponent<CanvasRenderer>();
            Image deathImg = deathPanel.AddComponent<Image>();
            deathImg.color = new Color(0.08f, 0.01f, 0.01f, 0.88f);
            deathPanel.SetActive(false);

            // Background Ring
            GameObject bgRingGo = new GameObject("BgRing");
            bgRingGo.transform.SetParent(deathPanel.transform, false);
            RectTransform bgRingRt = bgRingGo.AddComponent<RectTransform>();
            bgRingRt.sizeDelta = new Vector2(300, 300);
            bgRingRt.anchoredPosition = new Vector2(0, 50);
            bgRingGo.AddComponent<CanvasRenderer>();
            Image bgRingImg = bgRingGo.AddComponent<Image>();
            bgRingImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            bgRingImg.color = new Color(0.2f, 0.05f, 0.05f, 0.5f);

            // Active Radial Circle
            GameObject radialGo = new GameObject("RadialCircle");
            radialGo.transform.SetParent(deathPanel.transform, false);
            RectTransform radialRt = radialGo.AddComponent<RectTransform>();
            radialRt.sizeDelta = new Vector2(300, 300);
            radialRt.anchoredPosition = new Vector2(0, 50);
            radialGo.AddComponent<CanvasRenderer>();
            Image radialImg = radialGo.AddComponent<Image>();
            radialImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            radialImg.color = new Color(0.85f, 0.15f, 0.15f, 0.9f);
            radialImg.type = Image.Type.Filled;
            radialImg.fillMethod = Image.FillMethod.Radial360;
            radialImg.fillOrigin = (int)Image.Origin360.Top;
            radialImg.fillClockwise = true;
            radialImg.fillAmount = 0f;

            // Countdown Text
            GameObject countTxtGo = new GameObject("CountdownText");
            countTxtGo.transform.SetParent(deathPanel.transform, false);
            RectTransform countTxtRt = countTxtGo.AddComponent<RectTransform>();
            countTxtRt.anchorMin = new Vector2(0.5f, 0.5f);
            countTxtRt.anchorMax = new Vector2(0.5f, 0.5f);
            countTxtRt.sizeDelta = new Vector2(250, 80);
            countTxtRt.anchoredPosition = new Vector2(0, 50);
            countTxtGo.AddComponent<CanvasRenderer>();
            Text countTxt = countTxtGo.AddComponent<Text>();
            countTxt.font = GetDefaultFont();
            countTxt.fontSize = 38;
            countTxt.fontStyle = FontStyle.Bold;
            countTxt.color = Color.white;
            countTxt.alignment = TextAnchor.MiddleCenter;
            countTxt.text = "5.0s";

            // Subtitle text below the circle
            GameObject subTxtGo = new GameObject("SubtitleText");
            subTxtGo.transform.SetParent(deathPanel.transform, false);
            RectTransform subTxtRt = subTxtGo.AddComponent<RectTransform>();
            subTxtRt.anchorMin = new Vector2(0.5f, 0.5f);
            subTxtRt.anchorMax = new Vector2(0.5f, 0.5f);
            subTxtRt.sizeDelta = new Vector2(600, 50);
            subTxtRt.anchoredPosition = new Vector2(0, -140);
            subTxtGo.AddComponent<CanvasRenderer>();
            Text subTxt = subTxtGo.AddComponent<Text>();
            subTxt.font = GetDefaultFont();
            subTxt.fontSize = 20;
            subTxt.color = new Color(0.9f, 0.7f, 0.7f);
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.text = "RESPAWN EN CAMINO...";

            // Wire up components
            Redes.Views.DeathScreenView deathView = deathPanel.AddComponent<Redes.Views.DeathScreenView>();
            System.Reflection.FieldInfo panelField = typeof(Redes.Views.DeathScreenView).GetField("_panel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo circleField = typeof(Redes.Views.DeathScreenView).GetField("_radialCircle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo textField = typeof(Redes.Views.DeathScreenView).GetField("_countdownText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (panelField != null) panelField.SetValue(deathView, deathPanel);
            if (circleField != null) circleField.SetValue(deathView, radialImg);
            if (textField != null) textField.SetValue(deathView, countTxt);

            Redes.Views.DeathScreenController deathCtrl = deathPanel.AddComponent<Redes.Views.DeathScreenController>();
            System.Reflection.FieldInfo viewField = typeof(Redes.Views.DeathScreenController).GetField("_view", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (viewField != null) viewField.SetValue(deathCtrl, deathView);
        }

        private static void EnsureTagExists(string tag)
        {
            UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset != null && asset.Length > 0)
            {
                SerializedObject so = new SerializedObject(asset[0]);
                SerializedProperty tags = so.FindProperty("tags");
                for (int i = 0; i < tags.arraySize; i++)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        return; // Already exists
                    }
                }
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                so.ApplyModifiedProperties();
            }
        }

        // [MenuItem("Tools/Pruebas/Build Game")]
        public static void BuildStandalonePlayer()
        {
            try
            {
                Debug.Log("[Build] Iniciando configuracion de escenas de build...");
                
                string activeScenePath = EditorSceneManager.GetActiveScene().path;
                List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
                
                if (!string.IsNullOrEmpty(activeScenePath))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(activeScenePath, true));
                    Debug.Log($"[Build] Agregando escena activa al inicio: {activeScenePath}");
                }
                else
                {
                    // Fallback to Scene_Game if active scene is not saved/empty
                    activeScenePath = "Assets/Scenes/Scene_Game.unity";
                    buildScenes.Add(new EditorBuildSettingsScene(activeScenePath, true));
                    Debug.Log($"[Build] Advertencia: Escena activa vacia. Usando fallback: {activeScenePath}");
                }
                
                string[] knownScenes = new string[] {
                    "Assets/Scenes/Scene_Game.unity",
                    "Assets/Scenes/Scene_DebugTests.unity"
                };
                
                foreach (var scenePath in knownScenes)
                {
                    if (scenePath.Replace("\\", "/").ToLower() != activeScenePath.Replace("\\", "/").ToLower() && File.Exists(scenePath))
                    {
                        buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                        Debug.Log($"[Build] Preservando escena en la lista: {scenePath}");
                    }
                }
                
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Debug.Log($"[Build] Escenas configuradas. Escena de inicio (Index 0): {EditorBuildSettings.scenes[0].path}");

                // Asegurar que el directorio de salida existe
                if (!Directory.Exists("Builds"))
                {
                    Directory.CreateDirectory("Builds");
                }

                string buildPath = "Builds/Game.exe";
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
                
                List<string> scenePaths = new List<string>();
                foreach (var bs in EditorBuildSettings.scenes)
                {
                    scenePaths.Add(bs.path);
                }
                buildPlayerOptions.scenes = scenePaths.ToArray();
                buildPlayerOptions.locationPathName = buildPath;
                buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
                buildPlayerOptions.options = BuildOptions.None;

                // Configurar 1280x720 en formato ventana
                // PlayerSettings.defaultIsFullScreen = false;
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true;

                Debug.Log("[Build] Compilando Standalone Windows 64 Player (1280x720 Windowed)...");
                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                var summary = report.summary;

                if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    Debug.Log($"[Build OK] Build completada exitosamente en {summary.totalTime.TotalSeconds:F2} segundos. Ubicacion: {buildPath}");
                }
                else
                {
                    Debug.LogError($"[Build Fallida] Fallo la compilacion con resultado: {summary.result}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Build Exception] Error al compilar: {ex.Message}");
            }
        }
    }
}

