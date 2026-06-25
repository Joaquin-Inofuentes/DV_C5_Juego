using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Fusion;
using Redes.Network;
using Redes.Controllers;
using Redes.Views;
using Redes.Gameplay;

namespace Redes.EditorTools
{
    /// <summary>
    /// Tools > Redes > 1. Create Scene
    ///
    /// Builds the full primitive scene STRUCTURE (camera, legacy-Text UI, and all
    /// the system GameObjects with their components attached). It only creates the
    /// hierarchy + components; wiring the serialized references is done by
    /// "3. Link & Assign All".
    /// </summary>
    public static class RedesSceneCreator
    {
        private const string SceneFolder = "Assets/_Redes/Scenes";
        private const string ScenePath = SceneFolder + "/RedesGame.unity";

        [MenuItem("Tools/Redes/1. Create Scene", priority = 1)]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Redes", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- Camera (top-down) ----
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 12, 0);
            camGo.transform.rotation = Quaternion.Euler(90, 0, 0); // look straight down

            // ---- Light ----
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            // ---- Ground (TerrainController MVC) ----
            var ground = new GameObject("Ground");
            var terrainController = ground.AddComponent<Redes.Controllers.TerrainController>();
            // Since it requires TerrainModel and TerrainView, they are added automatically.
            
            // Try to assign the downloaded terrain texture via SerializedObject since the field is private serialized
            var texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Redes/Art/Textures/Terrain.png");
            if (texAsset != null)
            {
                var so = new SerializedObject(terrainController);
                var texProp = so.FindProperty("_terrainTexture");
                if (texProp != null) texProp.objectReferenceValue = texAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ---- Network ----
            // OJO: NO agregamos NetworkRunner aquí. HostNetworkService crea sus
            // propios runners en runtime (lobbyRunner + gameRunner frescos) para
            // evitar reutilizar un runner muerto tras un Shutdown (Fusion 2).
            var runnerGo = new GameObject("NetworkManager");
            runnerGo.AddComponent<HostNetworkService>();
            runnerGo.AddComponent<PlayerSpawner>();

            // ---- GameManager (scene Network Object for win/lose RPC) ----
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<NetworkObject>();
            gmGo.AddComponent<MatchNetworkController>();

            // ---- Controllers ----
            var ctrlGo = new GameObject("Controllers");
            ctrlGo.AddComponent<GameFlowController>();
            ctrlGo.AddComponent<MatchController>();
            ctrlGo.AddComponent<PlayerController>();
            ctrlGo.AddComponent<EntityDisplayManager>();
            
            // ---- VFX Manager ----
            var vfxGo = new GameObject("VFXManager");
            var vfxManager = vfxGo.AddComponent<VFXManager>();
            
            // Create Hit VFX Prefab
            var hitGo = new GameObject("HitVFXPrefab");
            hitGo.transform.SetParent(vfxGo.transform, false);
            hitGo.SetActive(false); // keep it as a prefab
            var hitPs = hitGo.AddComponent<ParticleSystem>();
            var hitMain = hitPs.main;
            hitMain.duration = 0.5f;
            hitMain.loop = false;
            hitMain.startLifetime = 0.5f;
            hitMain.startSpeed = 5f;
            hitMain.startSize = 0.3f;
            hitMain.startColor = Color.red;
            var hitEmission = hitPs.emission;
            hitEmission.rateOverTime = 0;
            hitEmission.SetBursts(new[] { new ParticleSystem.Burst(0, 20) });
            var hitShape = hitPs.shape;
            hitShape.shapeType = ParticleSystemShapeType.Sphere;

            // Create Muzzle VFX Prefab
            var muzzleGo = new GameObject("MuzzleVFXPrefab");
            muzzleGo.transform.SetParent(vfxGo.transform, false);
            muzzleGo.SetActive(false);
            var muzzlePs = muzzleGo.AddComponent<ParticleSystem>();
            var muzzleMain = muzzlePs.main;
            muzzleMain.duration = 0.1f;
            muzzleMain.loop = false;
            muzzleMain.startLifetime = 0.1f;
            muzzleMain.startSpeed = 10f;
            muzzleMain.startSize = 0.2f;
            muzzleMain.startColor = Color.yellow;
            var muzzleEmission = muzzlePs.emission;
            muzzleEmission.rateOverTime = 0;
            muzzleEmission.SetBursts(new[] { new ParticleSystem.Burst(0, 10) });
            var muzzleShape = muzzlePs.shape;
            muzzleShape.shapeType = ParticleSystemShapeType.Cone;
            muzzleShape.angle = 15f;

            // Try to assign the particle texture
            var partTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Redes/Art/Textures/Particle.png");
            if (partTex != null)
            {
                var mat = new Material(Shader.Find("Particles/Standard Unlit"));
                mat.mainTexture = partTex;
                var hitRenderer = hitPs.GetComponent<ParticleSystemRenderer>();
                var muzzleRenderer = muzzlePs.GetComponent<ParticleSystemRenderer>();
                if (hitRenderer != null) hitRenderer.sharedMaterial = mat;
                if (muzzleRenderer != null) muzzleRenderer.sharedMaterial = mat;
            }

            var soVfx = new SerializedObject(vfxManager);
            soVfx.FindProperty("_hitVfxPrefab").objectReferenceValue = hitPs;
            soVfx.FindProperty("_muzzleFlashPrefab").objectReferenceValue = muzzlePs;
            soVfx.ApplyModifiedPropertiesWithoutUndo();

            // ---- UI (legacy Text) ----
            BuildUI();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("<color=#9E9E9E>[REDES][BOOT]</color> Escena creada en " + ScenePath +
                      ". Ahora ejecuta 'Tools > Redes > 2. Create Prefabs' y luego '3. Link & Assign All'.");
        }

        private static void BuildUI()
        {
            // EventSystem (needed for the Host button).
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            // Canvas.
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = GetLegacyFont();

            // --- Lobby ---
            var lobby = NewUiPanel("LobbyView", canvasGo.transform);
            lobby.AddComponent<LobbyView>();
            NewText("StatusText", lobby.transform, font, "¿Crear sala o unirse?", new Vector2(0, 180), 28);
            NewText("PlayerCountText", lobby.transform, font, "Jugadores: 0/2", new Vector2(0, 130), 24);
            
            // Ingrese nombre aqui (Label/Title above input)
            NewText("InputLabelText", lobby.transform, font, "Ingrese nombre aqui", new Vector2(0, 75), 24);
            
            var inputField = NewInputField("UsernameInput", lobby.transform, font, "Username", new Vector2(0, 25));
            
            // Layout container for the 3 name buttons
            var nameButtonsLayout = new GameObject("NameButtonsLayout", typeof(RectTransform));
            nameButtonsLayout.transform.SetParent(lobby.transform, false);
            var layoutRt = nameButtonsLayout.GetComponent<RectTransform>();
            layoutRt.anchoredPosition = new Vector2(0, -35);
            layoutRt.sizeDelta = new Vector2(400, 50);
            var hlg = nameButtonsLayout.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            
            // The 3 generic name buttons with eye-catching hover/normal colors
            var rambo = NewButton("RamboButton", nameButtonsLayout.transform, font, "Rambo", Vector2.zero);
            rambo.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f); // Dark Red
            var rColors = rambo.colors;
            rColors.normalColor = new Color(0.6f, 0.1f, 0.1f);
            rColors.highlightedColor = new Color(1.0f, 0.2f, 0.2f); // Bright Neon Red
            rColors.selectedColor = new Color(1.0f, 0.2f, 0.2f);
            rambo.colors = rColors;
            
            var t600 = NewButton("T600Button", nameButtonsLayout.transform, font, "T-600", Vector2.zero);
            t600.GetComponent<Image>().color = new Color(0.6f, 0.4f, 0.0f); // Dark Orange/Gold
            var tColors = t600.colors;
            tColors.normalColor = new Color(0.6f, 0.4f, 0.0f);
            tColors.highlightedColor = new Color(1.0f, 0.7f, 0.0f); // Bright Neon Gold
            tColors.selectedColor = new Color(1.0f, 0.7f, 0.0f);
            t600.colors = tColors;
            
            var lion = NewButton("LionButton", nameButtonsLayout.transform, font, "Lion", Vector2.zero);
            lion.GetComponent<Image>().color = new Color(0.0f, 0.5f, 0.5f); // Teal
            var lColors = lion.colors;
            lColors.normalColor = new Color(0.0f, 0.5f, 0.5f);
            lColors.highlightedColor = new Color(0.0f, 0.9f, 0.9f); // Bright Neon Cyan
            lColors.selectedColor = new Color(0.0f, 0.9f, 0.9f);
            lion.colors = lColors;
            
            var roomList = NewUiPanel("RoomList", lobby.transform);
            var roomListRt = roomList.GetComponent<RectTransform>();
            roomListRt.anchorMin = new Vector2(0.5f, 0.5f); roomListRt.anchorMax = new Vector2(0.5f, 0.5f);
            roomListRt.sizeDelta = new Vector2(400, 120);
            roomListRt.anchoredPosition = new Vector2(0, -135);
            roomList.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            var vlg = roomList.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            NewButton("HostButton", lobby.transform, font, "CREAR SALA", new Vector2(0, -230));

            // --- HUD ---
            var hud = NewUiPanel("GameHudView", canvasGo.transform);
            hud.AddComponent<GameHudView>();
            
            var hpText = NewText("HealthText", hud.transform, font, "Vida: 100", Vector2.zero, 24, TextAnchor.MiddleLeft);
            var hpRt = hpText.rectTransform;
            hpRt.anchorMin = new Vector2(0, 1); hpRt.anchorMax = new Vector2(0, 1); hpRt.pivot = new Vector2(0, 1);
            hpRt.anchoredPosition = new Vector2(30, -30);

            var amText = NewText("AmmoText", hud.transform, font, "Munición: 6/6", Vector2.zero, 24, TextAnchor.MiddleLeft);
            var amRt = amText.rectTransform;
            amRt.anchorMin = new Vector2(0, 1); amRt.anchorMax = new Vector2(0, 1); amRt.pivot = new Vector2(0, 1);
            amRt.anchoredPosition = new Vector2(30, -70);

            var stText = NewText("StateText", hud.transform, font, "Estado: Quieto", Vector2.zero, 24, TextAnchor.MiddleLeft);
            var stRt = stText.rectTransform;
            stRt.anchorMin = new Vector2(0, 1); stRt.anchorMax = new Vector2(0, 1); stRt.pivot = new Vector2(0, 1);
            stRt.anchoredPosition = new Vector2(30, -110);

            var reloadSlider = NewSlider("ReloadSlider", hud.transform, new Vector2(30, -160), new Vector2(200, 20));
            var slRt = reloadSlider.GetComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0, 1); slRt.anchorMax = new Vector2(0, 1); slRt.pivot = new Vector2(0, 1);
            slRt.anchoredPosition = new Vector2(30, -150);

            // --- Entity Display Overlay Panel (for enemy/player overhead health bars) ---
            NewUiPanel("EntityDisplayPanel", canvasGo.transform);

            // --- Result ---
            var result = NewUiPanel("ResultView", canvasGo.transform);
            result.AddComponent<ResultView>();
            
            // Backgrounds - occupying the full screen, placed first under ResultView so they are behind other UI
            var winBgGo = new GameObject("WinBackground", typeof(RectTransform));
            winBgGo.transform.SetParent(result.transform, false);
            var winBgRt = winBgGo.GetComponent<RectTransform>();
            winBgRt.anchorMin = Vector2.zero; winBgRt.anchorMax = Vector2.one;
            winBgRt.offsetMin = Vector2.zero; winBgRt.offsetMax = Vector2.zero;
            var winRaw = winBgGo.AddComponent<RawImage>();
            winRaw.color = new Color(0.1f, 0.4f, 0.2f, 0.75f); // Transparent sleek green background for win
            winBgGo.SetActive(false);

            var loseBgGo = new GameObject("LoseBackground", typeof(RectTransform));
            loseBgGo.transform.SetParent(result.transform, false);
            var loseBgRt = loseBgGo.GetComponent<RectTransform>();
            loseBgRt.anchorMin = Vector2.zero; loseBgRt.anchorMax = Vector2.one;
            loseBgRt.offsetMin = Vector2.zero; loseBgRt.offsetMax = Vector2.zero;
            var loseRaw = loseBgGo.AddComponent<RawImage>();
            loseRaw.color = new Color(0.5f, 0.1f, 0.1f, 0.75f); // Transparent sleek red background for lose
            loseBgGo.SetActive(false);

            var resultPanel = NewUiPanel("ResultPanel", result.transform);
            NewText("ResultText", resultPanel.transform, font, "RESULTADO", new Vector2(0, 100), 48);
            NewButton("RetryButton", resultPanel.transform, font, "REINTENTAR", new Vector2(0, 0));
            NewButton("LobbyButton", resultPanel.transform, font, "VOLVER AL LOBBY", new Vector2(0, -80));
            resultPanel.SetActive(false); // hidden until match ends.
        }

        // ---------- small UI helpers ----------

        private static Slider NewSlider(string name, Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.7f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one; faRect.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fRect = fill.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one; fRect.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = Color.yellow;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fRect;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            return slider;
        }

        private static GameObject NewUiPanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static Text NewText(string name, Transform parent, Font font, string content,
                                    Vector2 anchoredPos, int size, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(600, 60);
            rt.anchoredPosition = anchoredPos;
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false; // Prevent text from intercepting UI clicks
            return text;
        }

        private static Button NewButton(string name, Transform parent, Font font, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(220, 60);
            rt.anchoredPosition = anchoredPos;
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.9f); // Sleek modern dark blue/slate color
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.16f, 0.24f, 0.9f);
            colors.highlightedColor = new Color(0.44f, 0.12f, 0.94f, 1f); // Vibrant neon magenta/purple highlight
            colors.pressedColor = new Color(0.08f, 0.10f, 0.16f, 1f); // Deep dark blue pressed
            colors.selectedColor = new Color(0.44f, 0.12f, 0.94f, 1f); // Match highlighted
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            btn.colors = colors;

            var txt = NewText("Text", go.transform, font, label, Vector2.zero, 26);
            txt.color = Color.white;
            return btn;
        }

        private static InputField NewInputField(string name, Transform parent, Font font, string placeholderTxt, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(300, 40);
            rt.anchoredPosition = anchoredPos;
            
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            
            var input = go.AddComponent<InputField>();
            
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            var textComp = textGo.AddComponent<Text>();
            textComp.font = font;
            textComp.fontSize = 20;
            textComp.color = Color.black;
            textComp.alignment = TextAnchor.MiddleCenter;
            
            input.textComponent = textComp;
            
            return input;
        }

        private static Font GetLegacyFont()
        {
            // Unity 2022.3 ships the legacy font as LegacyRuntime.ttf.
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }


        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
