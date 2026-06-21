using UnityEditor;
using UnityEditor.SceneManagement;
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
        private const string ScenePath = "Assets/_Redes/Scenes/RedesGame.unity";

        [MenuItem("Tools/Redes/3. Link & Assign All", priority = 3)]
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

            // ---- Network ----
            Assign(host, ("_playerSpawner", spawner), ("_playerPrefab", playerPrefabNo));

            // ---- Controllers ----
            Assign(flow, ("_hostService", host), ("_lobbyView", lobby),
                         ("_gameHudView", hud), ("_matchController", match));
            Assign(match, ("_resultView", result));
            Assign(player, ("_hudView", hud));
            Assign(matchNet, ("_matchController", match));

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
                ("_joinButton", ChildComp<Button>(lobby, "JoinButton")));
            Assign(hud,
                ("_healthText", ChildText(hud, "HealthText")),
                ("_ammoText", ChildText(hud, "AmmoText")),
                ("_stateText", ChildText(hud, "StateText")),
                ("_reloadSlider", ChildComp<Slider>(hud, "ReloadSlider")));
            Assign(result,
                ("_resultText", ChildText(result, "ResultText")),
                ("_panelRoot", ChildGO(result, "ResultPanel")),
                ("_retryButton", ChildComp<Button>(result, "RetryButton")));

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
                    Assign(playerPrefab.GetComponent<PlayerHealth>(), ("_eventBus", eventBus));
                    Assign(playerPrefab.GetComponent<PlayerShooting>(), ("_eventBus", eventBus));
                    Assign(playerPrefab.GetComponent<AmmoSystem>(), ("_eventBus", eventBus));
                }
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
    }
}
