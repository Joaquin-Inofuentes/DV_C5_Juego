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

            // ---- Network ----
            var runnerGo = new GameObject("NetworkRunner");
            runnerGo.AddComponent<NetworkRunner>();
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
            NewButton("HostButton", lobby.transform, font, "CREAR SALA", new Vector2(-130, 20));
            NewButton("JoinButton", lobby.transform, font, "UNIRSE A SALA", new Vector2(130, 20));

            // --- HUD ---
            var hud = NewUiPanel("GameHudView", canvasGo.transform);
            hud.AddComponent<GameHudView>();
            NewText("HealthText", hud.transform, font, "Vida: 100", new Vector2(-320, 200), 24, TextAnchor.MiddleLeft);
            NewText("AmmoText", hud.transform, font, "Munición: 6/6", new Vector2(-320, 160), 24, TextAnchor.MiddleLeft);

            // --- Result ---
            var result = NewUiPanel("ResultView", canvasGo.transform);
            result.AddComponent<ResultView>();
            var resultPanel = NewUiPanel("ResultPanel", result.transform);
            NewText("ResultText", resultPanel.transform, font, "RESULTADO", new Vector2(0, 0), 48);
            resultPanel.SetActive(false); // hidden until match ends.
        }

        // ---------- small UI helpers ----------

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
