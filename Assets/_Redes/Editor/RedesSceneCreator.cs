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

            // ---- Ground (primitive plane so the top-down arena is visible) ----
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2, 1, 2);
            var groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = Color.black;
                groundRenderer.sharedMaterial = mat;
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
            NewText("StatusText", lobby.transform, font, "¿Crear sala o unirse?", new Vector2(0, 140), 28);
            NewText("PlayerCountText", lobby.transform, font, "Jugadores: 0/2", new Vector2(0, 90), 24);
            
            var inputField = NewInputField("UsernameInput", lobby.transform, font, "Username", new Vector2(0, 30));
            
            var roomList = NewUiPanel("RoomList", lobby.transform);
            var roomListRt = roomList.GetComponent<RectTransform>();
            roomListRt.anchorMin = new Vector2(0.5f, 0.5f); roomListRt.anchorMax = new Vector2(0.5f, 0.5f);
            roomListRt.sizeDelta = new Vector2(400, 200);
            roomListRt.anchoredPosition = new Vector2(0, -100);
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
            var resultPanel = NewUiPanel("ResultPanel", result.transform);
            NewText("ResultText", resultPanel.transform, font, "RESULTADO", new Vector2(0, 40), 48);
            NewButton("RetryButton", resultPanel.transform, font, "REINTENTAR", new Vector2(0, -40));
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
            go.AddComponent<Image>().color = new Color(0.15f, 0.45f, 0.85f);
            var btn = go.AddComponent<Button>();
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

        [MenuItem("Tools/Redes/5. Corregir", priority = 5)]
        public static void Corregir()
        {
            Debug.Log("[REDES][CORREGIR] Iniciando corrección completa...");
            
            // 1. Recrear prefabs
            RedesPrefabCreator.CreatePrefabs();
            
            // 2. Recrear escena (incluye el terreno negro, nuevos paneles y controladores)
            CreateScene();
            
            // 3. Re-vincular todas las referencias
            RedesSceneLinker.LinkAll();
            
            Debug.Log("[REDES][CORREGIR] ¡Corrección completa finalizada con éxito!");
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
