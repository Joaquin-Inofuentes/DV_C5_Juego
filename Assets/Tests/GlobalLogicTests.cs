using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using DebugSystem;

namespace DebugSystem.Tests
{
    public class GlobalLogicTests
    {
        private DebugLogger logger;
        private GameManager gameManager;
        private GameObject testHarness;
        private GameObject poolManagerGo;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EventBus.ClearAllEvents();
            if (DebugLogger.Instance != null)
            {
                Object.DestroyImmediate(DebugLogger.Instance.gameObject);
            }
            testHarness = new GameObject("TestHarness");
            gameManager = testHarness.AddComponent<GameManager>();
            logger = testHarness.AddComponent<DebugLogger>();
            logger.Awake();
            testHarness.AddComponent<GameLoopReferee>();

            // Configurar logger en modo prueba
            logger.IsTestMode = true;
            logger.ClearLogs();

            // Setup de PoolManager
            poolManagerGo = new GameObject("PoolManager");
            poolManagerGo.SetActive(false);
            poolManagerGo.transform.SetParent(testHarness.transform);
            BulletPool pool = poolManagerGo.AddComponent<BulletPool>();

            // Crear un prefab de bala ficticio en memoria para el pool
            GameObject bulletTemplate = new GameObject("BulletTemplate");
            bulletTemplate.SetActive(false);
            Bullet bulletComponent = bulletTemplate.AddComponent<Bullet>();
            bulletComponent.Speed = 20f;
            bulletComponent.Damage = 20f;

            // Asignar al pool vía reflexión/serialización
            var poolSO = new UnityEditor.SerializedObject(pool);
            poolSO.FindProperty("bulletPrefab").objectReferenceValue = bulletComponent;
            poolSO.ApplyModifiedProperties();

            poolManagerGo.SetActive(true);

            // Destruir el template para evitar fugas una vez asignado o mantenerlo desactivado bajo testHarness
            bulletTemplate.transform.SetParent(testHarness.transform);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (testHarness != null)
            {
                Object.DestroyImmediate(testHarness);
            }
            yield return null;
        }

        private void AssertLogExists(string eventKey)
        {
            bool found = logger.LoggedEvents.Exists(e => e.EventKey == eventKey);
            Assert.IsTrue(found, $"Evento con EventKey '{eventKey}' no fue emitido.");
        }

        private void AssertLogExistsWithMessage(string eventKey, string partialMsg)
        {
            bool found = logger.LoggedEvents.Exists(e => e.EventKey == eventKey && e.FormattedMessage.Contains(partialMsg));
            Assert.IsTrue(found, $"Evento '{eventKey}' con mensaje conteniendo '{partialMsg}' no fue emitido.");
        }

        // ==========================================
        // 🔩 SYSTEM TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_System_Events()
        {
            EventBus.TriggerGameStarted("1.0.0", 42);
            EventBus.TriggerGameClosed();
            EventBus.TriggerSceneLoaded("Scene_Game");
            EventBus.TriggerSceneLoadError("Scene_Missing");
            EventBus.TriggerGameMode("Deathmatch", 4);
            EventBus.TriggerRegion("us-east");
            EventBus.TriggerTargetFrameRate(60);
            EventBus.TriggerVSync(true);
            EventBus.TriggerEditor(true);
            EventBus.TriggerPlatform("Windows");
            EventBus.TriggerTimeScale(1.0f);
            EventBus.TriggerTestMode(true);

            yield return null;

            AssertLogExists("GAME_STARTED");
            AssertLogExists("GAME_CLOSED");
            AssertLogExists("SCENE_LOADED");
            AssertLogExists("SCENE_LOAD_ERROR");
            AssertLogExists("GAME_MODE");
            AssertLogExists("REGION");
            AssertLogExists("TARGET_FRAMERATE");
            AssertLogExists("VSYNC");
            AssertLogExists("EDITOR");
            AssertLogExists("PLATFORM");
            AssertLogExists("TIME_SCALE");
            AssertLogExists("TEST_MODE");
        }

        // ==========================================
        // 🔐 AUTH / IDENTIDAD TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Auth_Events()
        {
            EventBus.TriggerNameEntered("Joaco");
            EventBus.TriggerNameEmptyOrInvalid();
            EventBus.TriggerLocalIDAssigned("UID-9999");
            EventBus.TriggerExternalAuthStarted("Steam");
            EventBus.TriggerExternalAuthError("Timeout");

            yield return null;

            AssertLogExists("NAME_ENTERED");
            AssertLogExists("NAME_INVALID");
            AssertLogExists("LOCAL_ID");
            AssertLogExists("AUTH_EXTERNAL_START");
            AssertLogExists("AUTH_EXTERNAL_ERROR");
        }

        // ==========================================
        // 🏠 LOBBY / SALAS TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Lobby_Events()
        {
            EventBus.TriggerRoomCreated("Room_ABC");
            EventBus.TriggerRoomCreateError("Already exists");
            EventBus.TriggerRoomListReceived(5);
            EventBus.TriggerRoomJoin("Room_ABC");
            EventBus.TriggerRoomJoinError("Room full");
            EventBus.TriggerPlayerEntered(1, "Player_1");
            EventBus.TriggerPlayerLeft(1);
            EventBus.TriggerRoomPropUpdated("map", "dust2");
            EventBus.TriggerPlayerPropUpdated(1, "ready", "true");

            yield return null;

            AssertLogExists("ROOM_CREATED");
            AssertLogExists("ROOM_CREATE_ERROR");
            AssertLogExists("ROOM_LIST");
            AssertLogExists("ROOM_JOIN");
            AssertLogExists("ROOM_JOIN_ERROR");
            AssertLogExists("PLAYER_ENTERED");
            AssertLogExists("PLAYER_LEFT");
            AssertLogExists("ROOM_PROP_UPDATED");
            AssertLogExists("PLAYER_PROP_UPDATED");
        }

        // ==========================================
        // 🔄 MATCHMAKING TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Matchmaking_Events()
        {
            EventBus.TriggerMatchmakingSearching("Casual");
            EventBus.TriggerMatchmakingCancelled();
            EventBus.TriggerMatchFound("Room_ABC");
            EventBus.TriggerMatchmakingTimeout();
            EventBus.TriggerEnteringMatch();
            EventBus.TriggerMatchmakingError("No servers available");

            yield return null;

            AssertLogExists("MATCHMAKING_SEARCHING");
            AssertLogExists("MATCHMAKING_CANCELLED");
            AssertLogExists("MATCH_FOUND");
            AssertLogExists("MATCHMAKING_TIMEOUT");
            AssertLogExists("ENTERING_MATCH");
            AssertLogExists("MATCHMAKING_ERROR");
        }

        // ==========================================
        // 🌐 NETWORK / PHOTON TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Network_Events()
        {
            EventBus.TriggerConnectingMaster();
            EventBus.TriggerConnectedMaster();
            EventBus.TriggerConnectMasterError("No Internet");
            EventBus.TriggerPing(45);
            EventBus.TriggerPingHigh(150);
            EventBus.TriggerNetworkRegion("us-west");
            EventBus.TriggerNetworkRegionAutoChanged("eu-central");
            EventBus.TriggerDisconnected("Client disconnect");
            EventBus.TriggerReconnecting(2);
            EventBus.TriggerActorIDAssigned(1);
            EventBus.TriggerActorIDRemoved(1);
            EventBus.TriggerIsHost(true);
            EventBus.TriggerHostMigrated(2, 1);
            EventBus.TriggerRPCSent("OnShot", "All");
            EventBus.TriggerRPCReceived("OnShot", 2);
            EventBus.TriggerRPCIgnored("OnMove");
            EventBus.TriggerRPCDiscarded();
            EventBus.TriggerNetEventSent(200);
            EventBus.TriggerNetEventReceived(200, 2);
            EventBus.TriggerMsgBuffer(1024);
            EventBus.TriggerPacketLost(543);
            EventBus.TriggerNetSimActive(100, 5);

            yield return null;

            AssertLogExists("CONNECTING_MASTER");
            AssertLogExists("CONNECTED_MASTER");
            AssertLogExists("CONNECT_MASTER_ERROR");
            AssertLogExists("PING");
            AssertLogExists("PING_HIGH");
            AssertLogExists("NET_REGION");
            AssertLogExists("NET_REGION_AUTO");
            AssertLogExists("DISCONNECTED");
            AssertLogExists("RECONNECTING");
            AssertLogExists("ACTOR_ASSIGNED");
            AssertLogExists("ACTOR_REMOVED");
            AssertLogExists("IS_HOST");
            AssertLogExists("HOST_MIGRATED");
            AssertLogExists("RPC_SENT");
            AssertLogExists("RPC_RECEIVED");
            AssertLogExists("RPC_IGNORED");
            AssertLogExists("RPC_DISCARDED");
            AssertLogExists("NET_EVENT_SENT");
            AssertLogExists("NET_EVENT_RECEIVED");
            AssertLogExists("MSG_BUFFER");
            AssertLogExists("PACKET_LOST");
            AssertLogExists("NET_SIM_ACTIVE");
        }

        // ==========================================
        // 🧠 STATE MACHINE TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_StateMachine_Events()
        {
            EventBus.TriggerStateCurrent("LobbyState");
            EventBus.TriggerStateInvalid("UnknownState");
            EventBus.TriggerStateTransition("LobbyState", "GameState");
            EventBus.TriggerStateBlocked("LobbyState", "GameState", "Not enough players");
            EventBus.TriggerStateSynced("GameState");

            yield return null;

            AssertLogExists("STATE_CURRENT");
            AssertLogExists("STATE_INVALID");
            AssertLogExists("STATE_TRANSITION");
            AssertLogExists("STATE_BLOCKED");
            AssertLogExists("STATE_SYNCED");
        }

        // ==========================================
        // 🧍 PLAYER SYNC TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_PlayerSync_Events()
        {
            EventBus.TriggerPlayerReady(1, true);
            EventBus.TriggerPlayerReadyTimeout(1);
            EventBus.TriggerPlayerSceneLoaded(1, 100f);
            EventBus.TriggerSpawnRequestedLocal();
            EventBus.TriggerSpawnConfirmed(1);
            EventBus.TriggerSpawnDenied();
            EventBus.TriggerInputReceived("Horizontal", 15);
            EventBus.TriggerInputDiscarded();
            EventBus.TriggerAvatarAssigned(1, "SoldierPrefab");
            EventBus.TriggerPlayerDisconnected(1);
            EventBus.TriggerPlayerReconnected();

            yield return null;

            AssertLogExists("PLAYER_READY");
            AssertLogExists("PLAYER_READY_TIMEOUT");
            AssertLogExists("PLAYER_SCENE_LOADED");
            AssertLogExists("SPAWN_REQ");
            AssertLogExists("SPAWN_CONF");
            AssertLogExists("SPAWN_DENIED");
            AssertLogExists("INPUT_RECEIVED");
            AssertLogExists("INPUT_DISCARDED");
            AssertLogExists("AVATAR_ASSIGNED");
            AssertLogExists("PLAYER_DISCONNECTED");
            AssertLogExists("PLAYER_RECONNECTED");
        }

        // ==========================================
        // 🔫 ARMAS / DISPARO / RECARGA TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Weapon_Events()
        {
            EventBus.TriggerWeaponEquipped(1, "Rifle", 2);
            EventBus.TriggerShootStart(1, "Rifle", 29);
            EventBus.TriggerShootNoAmmo();
            EventBus.TriggerShootConfirmed(1, 123.45f);
            EventBus.TriggerProjectileCreated(10, 1, "Bullet", 0f, 1f, 2f);
            EventBus.TriggerImpactClient(10, 2, 25f);
            EventBus.TriggerImpactDiscarded();
            EventBus.TriggerImpactHost(10, 2, 20f);
            EventBus.TriggerReloadStart(1, "Rifle");
            EventBus.TriggerReloadComplete();
            EventBus.TriggerReloadCancel(1, "Rifle");
            EventBus.TriggerAmmoUpdated(1, "Rifle", 30, 30, 90);

            yield return null;

            AssertLogExists("WEAPON_EQUIP");
            AssertLogExists("SHOOT_START");
            AssertLogExists("SHOOT_NO_AMMO");
            AssertLogExists("SHOOT_CONF");
            AssertLogExists("PROJ_CREATED");
            AssertLogExists("IMPACT_CLIENT");
            AssertLogExists("IMPACT_DISCARDED");
            AssertLogExists("IMPACT_HOST");
            AssertLogExists("RELOAD_START");
            AssertLogExists("RELOAD_COMP");
            AssertLogExists("RELOAD_CANCEL");
            AssertLogExists("AMMO_UPDATED");
        }

        // ==========================================
        // 💥 DAÑO / VIDA / MUERTE TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Damage_Events()
        {
            EventBus.TriggerDamageApplied(1, 20f, 100f, 80f, 2);
            EventBus.TriggerHealthSynced(1, 80f, 50f);
            EventBus.TriggerPlayerDeath(1, 2, "Pistol");
            EventBus.TriggerPlayerDeathCause("Zone");
            EventBus.TriggerRespawnRequested(1, 5f);
            EventBus.TriggerRespawnExecuted();
            EventBus.TriggerInvulnerability(1, 3f);
            EventBus.TriggerHealReceived(1, 15f, 2);

            yield return null;

            AssertLogExists("DAMAGE_APPLIED");
            AssertLogExists("HEALTH_SYNCED");
            AssertLogExists("PLAYER_DEATH");
            AssertLogExists("PLAYER_DEATH_CAUSE");
            AssertLogExists("RESPAWN_REQ");
            AssertLogExists("RESPAWN_EXEC");
            AssertLogExists("INVULNERABILITY");
            AssertLogExists("HEAL_RECEIVED");
        }

        // ==========================================
        // 🎯 GANAR / PERDER TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_WinLose_Events()
        {
            EventBus.TriggerWinCondition("TimeOut");
            EventBus.TriggerTeamWin(1);
            EventBus.TriggerPlayerWin(1);
            EventBus.TriggerMatchFinished("Victory", 1);
            EventBus.TriggerTotalTime(300.5f);
            EventBus.TriggerFinalScore(1, 10);
            EventBus.TriggerMVP(1, "kills", 15f);
            EventBus.TriggerMatchRestartReq();
            EventBus.TriggerReturnToLobby();

            yield return null;

            AssertLogExists("WIN_COND");
            AssertLogExists("TEAM_WIN");
            AssertLogExists("PLAYER_WIN");
            AssertLogExists("MATCH_FINISHED");
            AssertLogExists("TOTAL_TIME");
            AssertLogExists("FINAL_SCORE");
            AssertLogExists("MVP");
            AssertLogExists("RESTART_REQ");
            AssertLogExists("RETURN_LOBBY");
        }

        // ==========================================
        // 🧩 NOTIFICACIONES / UI TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Notifications_Events()
        {
            EventBus.TriggerGlobalNotif("Welcome!", "System");
            EventBus.TriggerPersonalNotif(1, "Quest Complete");
            EventBus.TriggerAlertShow("Warning", "WarningIcon");
            EventBus.TriggerAlertHide();
            EventBus.TriggerStreakAnnounce(1, "Triple Kill");
            EventBus.TriggerTimeWarning(10f, "Zone Shrink");
            EventBus.TriggerObjectiveUpdate("Capture Flag", 1, 2);

            yield return null;

            AssertLogExists("GLOBAL_NOTIF");
            AssertLogExists("PERSONAL_NOTIF");
            AssertLogExists("ALERT_SHOW");
            AssertLogExists("ALERT_HIDE");
            AssertLogExists("STREAK");
            AssertLogExists("TIME_WARN");
            AssertLogExists("OBJECTIVE");
        }

        // ==========================================
        // 🎞️ ANIMACIONES / VISUALES TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Animations_Events()
        {
            EventBus.TriggerAnimTrigger(1, "Shoot");
            EventBus.TriggerAnimDamage(1, "Hit", 0f, -1f, 0f);
            EventBus.TriggerAnimDeath(1, "Explosion");
            EventBus.TriggerAnimReload(1, "Tactical");
            EventBus.TriggerVFX("MuzzleFlash", 1f, 2f, 3f, 1);
            EventBus.TriggerSFX("ExplosionSound", 1, 0.8f);
            EventBus.TriggerCameraShake(0.5f, 0.2f);
            EventBus.TriggerDamageUI(1, "left");
            EventBus.TriggerWeaponAnimBool(1, "isAiming", true);

            yield return null;

            AssertLogExists("ANIM_TRIGGER");
            AssertLogExists("ANIM_DAMAGE");
            AssertLogExists("ANIM_DEATH");
            AssertLogExists("ANIM_RELOAD");
            AssertLogExists("VFX");
            AssertLogExists("SFX");
            AssertLogExists("CAM_SHAKE");
            AssertLogExists("DAMAGE_UI");
            AssertLogExists("WEAPON_ANIM_BOOL");
        }

        // ==========================================
        // 🔍 DESYNC / DEBUG AVANZADO TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Desync_Events()
        {
            EventBus.TriggerSnapshotSent(100, 5000);
            EventBus.TriggerSnapshotReceived();
            EventBus.TriggerHashDesync(1111, 2222);
            EventBus.TriggerDiffActor(1, "Position", "0,0,0", "1,0,0");
            EventBus.TriggerCorrectionActor(1, "Position", "1,0,0");
            EventBus.TriggerForceResync("Player");

            yield return null;

            AssertLogExists("SNAPSHOT_SENT");
            AssertLogExists("SNAPSHOT_RECV");
            AssertLogExists("HASH_DESYNC");
            AssertLogExists("DIFF_ACTOR");
            AssertLogExists("CORRECTION");
            AssertLogExists("FORCE_RESYNC");
        }

        // ==========================================
        // 🔁 FALLBACKS / RECONEXIÓN TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_Fallbacks_Events()
        {
            EventBus.TriggerReconnectStart();
            EventBus.TriggerReconnectSuccess("Room_ABC");
            EventBus.TriggerReconnectDenied();
            EventBus.TriggerStateRecoverHost(2);
            EventBus.TriggerHostLost(3.0f);
            EventBus.TriggerNewHost(2);
            EventBus.TriggerMatchAborted("Host left");

            yield return null;

            AssertLogExists("RECONNECT_START");
            AssertLogExists("RECONNECT_SUCCESS");
            AssertLogExists("RECONNECT_DENIED");
            AssertLogExists("STATE_RECOVER");
            AssertLogExists("HOST_LOST");
            AssertLogExists("NEW_HOST");
            AssertLogExists("MATCH_ABORTED");
        }

        // ==========================================
        // 🧪 TEST / DEBUG TOOLS TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_DebugTools_Events()
        {
            EventBus.TriggerTestSeed(777);
            EventBus.TriggerTestBotCreate("Bot_Easy", 1);
            EventBus.TriggerTestBotRemove();
            EventBus.TriggerTestCmd("speed", "2.0");
            EventBus.TriggerTestOverrideState("GameState");
            EventBus.TriggerTestSimNet(80, 2);
            EventBus.TriggerTestPerf(60f, 16.6f, 500f);
            EventBus.TriggerTestDumpRoom(4, "map=dust2;round=3");

            yield return null;

            AssertLogExists("TEST_SEED");
            AssertLogExists("BOT_CREATE");
            AssertLogExists("BOT_REMOVE");
            AssertLogExists("TEST_CMD");
            AssertLogExists("TEST_OVERRIDE");
            AssertLogExists("TEST_SIM_NET");
            AssertLogExists("TEST_PERF");
            AssertLogExists("TEST_DUMP_ROOM");
        }
        // ==========================================
        // 📡 LOCAL NETWORK MOCK TESTS
        // ==========================================
        [UnityTest]
        public IEnumerator Test_LocalNetworkMock()
        {
            LocalNetworkMock.ClearRoom();
            Assert.IsFalse(LocalNetworkMock.RoomExists());

            LocalNetworkMock.CreateRoom("TestHost");
            Assert.IsTrue(LocalNetworkMock.RoomExists());
            Assert.IsTrue(LocalNetworkMock.IsHost);

            LocalNetworkMock.JoinRoom("TestClient");
            var data = LocalNetworkMock.GetRoomData();
            Assert.IsNotNull(data);
            Assert.AreEqual("TestHost", data.HostName);
            Assert.AreEqual("TestClient", data.Player2Name);
            Assert.IsTrue(data.Player2Ready);

            // Need to set host back to true to simulate host pressing start
            LocalNetworkMock.CreateRoom("TestHost"); // Re-claim host for test
            LocalNetworkMock.StartGame();
            
            data = LocalNetworkMock.GetRoomData();
            Assert.IsTrue(data.GameStarted);

            LocalNetworkMock.ClearRoom();
            Assert.IsFalse(LocalNetworkMock.RoomExists());
            yield return null;
        }
        // ==========================================
        // ⚔️ FULL MATCH SIMULATION TEST
        // ==========================================
        [UnityTest]
        public IEnumerator Test_FullMatch_Simulation()
        {
            // 1. Inicia jugador 1 y crea sala
            EventBus.TriggerNameEntered("Player1");
            LocalNetworkMock.ClearRoom();
            LocalNetworkMock.CreateRoom("Player1");
            Assert.IsTrue(LocalNetworkMock.IsHost);
            
            // 2. Inicia jugador 2 y se une
            EventBus.TriggerNameEntered("Player2");
            LocalNetworkMock.JoinRoom("Player2");
            var data = LocalNetworkMock.GetRoomData();
            Assert.IsTrue(data.GameStarted);
            
            // 3. Spawns (Simulated via EventBus since we are testing Logic/Events)
            EventBus.TriggerStateTransition("Lobby", "Playing");
            EventBus.TriggerSpawnConfirmed(1);
            EventBus.TriggerSpawnConfirmed(2);
            
            // 4. 1 recibe impacto del 2
            EventBus.TriggerShootStart(2, "Pistol", 11);
            EventBus.TriggerDamageApplied(1, 20f, 100f, 80f, 2);
            
            // 5. 2 recibe impacto del 1 (Daño letal)
            EventBus.TriggerShootStart(1, "Pistol", 11);
            EventBus.TriggerDamageApplied(2, 100f, 100f, 0f, 1);
            
            // 6. Gano 1, Perdio 2
            EventBus.TriggerPlayerDeath(2, 1, "Pistol");
            EventBus.TriggerMatchFinished("Victory", 1);
            
            // 7. Ambos aceptan reiniciar
            EventBus.TriggerMatchRestartReq();
            LocalNetworkMock.ClearRoom();
            Assert.IsFalse(LocalNetworkMock.RoomExists());
            
            yield return null;
            
            AssertLogExists("NAME_ENTERED");
            AssertLogExists("STATE_TRANSITION");
            AssertLogExists("SPAWN_CONF");
            AssertLogExists("SHOOT_START");
            AssertLogExists("DAMAGE_APPLIED");
            AssertLogExists("PLAYER_DEATH");
            AssertLogExists("MATCH_FINISHED");
            AssertLogExists("RESTART_REQ");
        }

        [UnityTest]
        public IEnumerator Test_Player_Instantiation_On_Playing_State()
        {
            // Prepare a player prefab
            GameObject fakePrefab = new GameObject("FakePlayerPrefab");
            fakePrefab.AddComponent<PlayerModel>();
            fakePrefab.AddComponent<PlayerView>();
            fakePrefab.AddComponent<PlayerController>();
            
            // Assign prefab and spawn points to GameManager
            gameManager.playerPrefab = fakePrefab;
            
            GameObject spawn1 = new GameObject("Spawn1");
            spawn1.transform.position = new Vector3(-2f, 0f, 0f);
            GameObject spawn2 = new GameObject("Spawn2");
            spawn2.transform.position = new Vector3(2f, 0f, 0f);
            
            gameManager.spawnPoint1 = spawn1.transform;
            gameManager.spawnPoint2 = spawn2.transform;

            // Trigger transition
            EventBus.TriggerStateTransition("Lobby", "Playing");
            yield return null;

            // Check if players were instantiated
            GameObject p1 = GameObject.Find("Player");
            GameObject p2 = GameObject.Find("Player2");

            Assert.IsNotNull(p1, "Player 1 should be instantiated!");
            Assert.IsNotNull(p2, "Player 2 should be instantiated!");

            // Cleanup
            if (p1 != null) Object.DestroyImmediate(p1);
            if (p2 != null) Object.DestroyImmediate(p2);
            Object.DestroyImmediate(spawn1);
            Object.DestroyImmediate(spawn2);
            Object.DestroyImmediate(fakePrefab);

            yield return null;
        }
    }
}
