using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Redes.Network;
using Redes.Controllers;
using Redes.Views;
using Redes.Gameplay;
using Redes.Player;

namespace Redes.EditorTools
{
    /// <summary>
    /// Tools > Redes > 3. Link & Assign All
    ///
    /// Opens the generated scene and wires EVERY serialized reference between the
    /// systems (network <-> controllers <-> views) and drops the prefabs into the
    /// fields that need them. Run this AFTER "1. Create Scene" and "2. Create Prefabs".
    ///
    /// It finds objects by component type / child name, so it is safe to re-run.
    /// </summary>
    public static class RedesSceneLinker
    {
        private const string ScenePath = "Assets/_Redes/Scenes/__Redes_RedesGame.unity";

        // [MenuItem("Tools/Redes/3. Link & Assign All", priority = 3)]
        public static void LinkAll()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // ---- Find scene components (include inactive) ----
            var host    = Find<HostNetworkService>();
            var spawner = Find<PlayerSpawner>();
            var flow    = Find<GameFlowController>();
            var match   = Find<MatchController>();
            var player  = Find<PlayerController>();
            var matchNet= Find<MatchNetworkController>();
            var lobby   = Find<LobbyView>();
            var hud     = Find<GameHudView>();
            var result  = Find<ResultView>();

            // ---- Load prefabs ----
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedesPrefabCreator.PlayerPrefabPath);
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedesPrefabCreator.BulletPrefabPath);
            var playerPrefabNo = playerPrefab != null ? playerPrefab.GetComponent<NetworkObject>() : null;
            var bulletPrefabNo = bulletPrefab != null ? bulletPrefab.GetComponent<NetworkObject>() : null;

            // ---- Load AudioClips ----
            var shootSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Shoot.wav");
            var hitSound   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Hit.wav");
            var winSound   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Win.wav");
            var loseSound  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/Lose.wav");
            var bgmSound   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Redes/Art/Audio/BGM.wav");

            // ---- Network ----
            Assign(host, ("_playerSpawner", spawner), ("_playerPrefab", playerPrefabNo));

            // Link the VFXManager spark prefab (to keep it clean and robust)
            var vfxManager = Find<VFXManager>();
            if (vfxManager != null)
            {
                var sparkVfxPrefab = vfxManager.transform.Find("SparkVFXPrefab")?.GetComponent<ParticleSystem>();
                if (sparkVfxPrefab != null)
                {
                    Assign(vfxManager, ("_sparkVfxPrefab", sparkVfxPrefab));
                }
            }

            // ---- Controllers ----
            Assign(flow, ("_hostService", host), ("_lobbyView", lobby),
                         ("_gameHudView", hud), ("_matchController", match));
            Assign(match, ("_resultView", result));
            Assign(player, ("_hudView", hud));
            Assign(matchNet, ("_matchController", match), 
                             ("_winSound", winSound), 
                             ("_loseSound", loseSound), 
                             ("_bgmSound", bgmSound));

            var displayManager = Find<EntityDisplayManager>();
            var displayViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedesPrefabCreator.EntityDisplayViewPrefabPath);
            var displayViewPrefabComponent = displayViewPrefab != null ? displayViewPrefab.GetComponent<EntityDisplayView>() : null;
            var canvas = GameObject.Find("Canvas");
            Transform displayPanelTrans = null;
            if (canvas != null)
            {
                displayPanelTrans = canvas.transform.Find("EntityDisplayPanel");
            }
            Assign(displayManager,
                ("_viewPrefab", displayViewPrefabComponent),
                ("_canvasParent", displayPanelTrans));

            // ---- Views (legacy Text) ----
            Assign(lobby,
                ("_statusText", ChildText(lobby, "StatusText")),
                ("_playerCountText", ChildText(lobby, "PlayerCountText")),
                ("_usernameInput", ChildComp<InputField>(lobby, "UsernameInput")),
                ("_roomListContainer", ChildGO(lobby, "RoomList")?.transform),
                ("_hostButton", ChildComp<Button>(lobby, "HostButton")),
                ("_ramboButton", ChildComp<Button>(lobby, "RamboButton")),
                ("_t600Button", ChildComp<Button>(lobby, "T600Button")),
                ("_lionButton", ChildComp<Button>(lobby, "LionButton")));
            Assign(hud,
                ("_healthText", ChildText(hud, "HealthText")),
                ("_ammoText", ChildText(hud, "AmmoText")),
                ("_stateText", ChildText(hud, "StateText")),
                ("_reloadSlider", ChildComp<Slider>(hud, "ReloadSlider")));
            Assign(result,
                ("_resultText", ChildText(result, "ResultText")),
                ("_panelRoot", ChildGO(result, "ResultPanel")),
                ("_retryButton", ChildComp<Button>(result, "RetryButton")),
                ("_lobbyButton", ChildComp<Button>(result, "LobbyButton")),
                ("_winBackground", ChildComp<RawImage>(result, "WinBackground")),
                ("_loseBackground", ChildComp<RawImage>(result, "LoseBackground")));

            // Link the global GameEventBus to everything that needs it
            var eventBus = AssetDatabase.LoadAssetAtPath<Redes.Core.GameEventBus>("Assets/_Redes/Scripts/Core/GameEventBus.asset");
            if (eventBus == null)
            {
                Debug.LogWarning("[REDES][LINK] GameEventBus not found! Need to create one in Assets/_Redes/Scripts/Core.");
            }
            else
            {
                Assign(matchNet, ("_eventBus", eventBus));
                if (playerPrefab != null)
                {
                    Assign(playerPrefab.GetComponent<PlayerHealth>(), ("_eventBus", eventBus), ("_hitSound", hitSound));
                    Assign(playerPrefab.GetComponent<PlayerShooting>(), ("_eventBus", eventBus));
                    Assign(playerPrefab.GetComponent<AmmoSystem>(), ("_eventBus", eventBus));
                    Assign(playerPrefab.GetComponent<Redes.Views.PlayerAnimationView>(), ("_shootSound", shootSound));
                }
            }

            // ---- Link persistent actions to buttons ----
            Debug.Log("[REDES][LINKER] === LISTA DE BOTONES Y ACCIONES ASOCIADAS ===");
            
            if (lobby != null)
            {
                var hostBtn = lobby.HostButton;
                if (hostBtn != null && flow != null)
                {
                    ClearPersistentListeners(hostBtn.onClick);
                    UnityEventTools.AddVoidPersistentListener(hostBtn.onClick, flow.CreateRoom);
                    Debug.Log($"- Botón '{hostBtn.name}' -> Asociado a {flow.GetType().Name}.CreateRoom()");
                }
                
                var ramboBtn = ChildComp<Button>(lobby, "RamboButton");
                if (ramboBtn != null)
                {
                    ClearPersistentListeners(ramboBtn.onClick);
                    UnityEventTools.AddStringPersistentListener(ramboBtn.onClick, lobby.SetUsername, "Rambo");
                    Debug.Log($"- Botón '{ramboBtn.name}' -> Asociado a {lobby.GetType().Name}.SetUsername(\"Rambo\")");
                }
                
                var t600Btn = ChildComp<Button>(lobby, "T600Button");
                if (t600Btn != null)
                {
                    ClearPersistentListeners(t600Btn.onClick);
                    UnityEventTools.AddStringPersistentListener(t600Btn.onClick, lobby.SetUsername, "T600");
                    Debug.Log($"- Botón '{t600Btn.name}' -> Asociado a {lobby.GetType().Name}.SetUsername(\"T600\")");
                }
                
                var lionBtn = ChildComp<Button>(lobby, "LionButton");
                if (lionBtn != null)
                {
                    ClearPersistentListeners(lionBtn.onClick);
                    UnityEventTools.AddStringPersistentListener(lionBtn.onClick, lobby.SetUsername, "Lion");
                    Debug.Log($"- Botón '{lionBtn.name}' -> Asociado a {lobby.GetType().Name}.SetUsername(\"Lion\")");
                }
            }

            if (result != null)
            {
                var retryBtn = ChildComp<Button>(result, "RetryButton");
                if (retryBtn != null)
                {
                    ClearPersistentListeners(retryBtn.onClick);
                    UnityEventTools.AddVoidPersistentListener(retryBtn.onClick, result.TriggerRetry);
                    Debug.Log($"- Botón '{retryBtn.name}' -> Asociado a {result.GetType().Name}.TriggerRetry()");
                }
                
                var lobbyBtn = ChildComp<Button>(result, "LobbyButton");
                if (lobbyBtn != null)
                {
                    ClearPersistentListeners(lobbyBtn.onClick);
                    UnityEventTools.AddVoidPersistentListener(lobbyBtn.onClick, result.TriggerLobby);
                    Debug.Log($"- Botón '{lobbyBtn.name}' -> Asociado a {result.GetType().Name}.TriggerLobby()");
                }
            }
            Debug.Log("[REDES][LINKER] =============================================");

            // ---- Add button sounds dynamically to all UI buttons ----
            var clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(RedesProceduralAudio.ClickPath);
            var sfxGroup = RedesAudioSetup.GetGroup("SFX");
            var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in allButtons)
            {
                var clickPlayer = button.gameObject.GetComponent<PlaySoundOnButtonClick>();
                if (clickPlayer == null)
                {
                    clickPlayer = button.gameObject.AddComponent<PlaySoundOnButtonClick>();
                }
                Assign(clickPlayer, 
                    ("_clickSound", clickSound), 
                    ("_sfxGroup", sfxGroup));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // ---- Prefab-internal cross reference (Bullet -> PlayerShooting) ----
            if (playerPrefab != null && bulletPrefabNo != null)
            {
                var shooting = playerPrefab.GetComponent<PlayerShooting>();
                Assign(shooting, ("_projectilePrefab", bulletPrefabNo));
                EditorUtility.SetDirty(playerPrefab);
                AssetDatabase.SaveAssets();
            }

            // NOTE: PlayerHealth._matchNetwork is a PREFAB -> SCENE reference, which Unity
            // cannot serialize into a prefab asset. The other agent resolves it at runtime
            // (e.g. FindFirstObjectByType<MatchNetworkController>() inside Spawned()).

            Debug.Log("<color=#9E9E9E>[REDES][BOOT]</color> Link & Assign completado. " +
                      "Recuerda registrar el Player y Bullet en el NetworkProjectConfig de Fusion.");
        }

        // ---------- helpers ----------

        private static T Find<T>() where T : Component
        {
            var arr = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (arr == null || arr.Length == 0)
            {
                Debug.LogWarning($"[REDES][LINK] No se encontró {typeof(T).Name} en la escena.");
                return null;
            }
            return arr[0];
        }

        private static Text ChildText(Component root, string name) => ChildComp<Text>(root, name);

        private static TC ChildComp<TC>(Component root, string name) where TC : Component
        {
            if (root == null) return null;
            foreach (var c in root.GetComponentsInChildren<TC>(true))
                if (c.gameObject.name == name) return c;
            Debug.LogWarning($"[REDES][LINK] Hijo '{name}' ({typeof(TC).Name}) no encontrado bajo {root.name}");
            return null;
        }

        private static GameObject ChildGO(Component root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.name == name) return t.gameObject;
            Debug.LogWarning($"[REDES][LINK] Hijo '{name}' no encontrado bajo {root.name}");
            return null;
        }

        private static void Assign(Object target, params (string field, Object value)[] pairs)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var prop = so.FindProperty(field);
                if (prop == null) { Debug.LogWarning($"[REDES][LINK] Campo '{field}' no existe en {target.GetType().Name}"); continue; }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ClearPersistentListeners(UnityEngine.Events.UnityEvent unityEvent)
        {
            while (unityEvent.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, 0);
            }
        }
    }
}

