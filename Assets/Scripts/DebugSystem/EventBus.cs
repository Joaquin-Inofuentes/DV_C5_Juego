using System;

namespace DebugSystem
{
    public static class EventBus
    {
        // 🔩 SYSTEM
        public static event Action<string, int> OnGameStarted;
        public static event Action OnGameClosed;
        public static event Action<string> OnSceneLoaded;
        public static event Action<string> OnSceneLoadError;
        public static event Action<string, int> OnGameMode;
        public static event Action<string> OnRegion;
        public static event Action<int> OnTargetFrameRate;
        public static event Action<bool> OnVSync;
        public static event Action<bool> OnEditor;
        public static event Action<string> OnPlatform;
        public static event Action<float> OnTimeScale;
        public static event Action<bool> OnTestMode;

        // 🔐 AUTH / IDENTIDAD
        public static event Action<string> OnNameEntered;
        public static event Action OnNameEmptyOrInvalid;
        public static event Action<string> OnLocalIDAssigned;
        public static event Action<string> OnExternalAuthStarted;
        public static event Action<string> OnExternalAuthError;

        // 🏠 LOBBY / SALAS
        public static event Action<string> OnRoomCreated;
        public static event Action<string> OnRoomCreateError;
        public static event Action<int> OnRoomListReceived;
        public static event Action<string> OnRoomJoin;
        public static event Action<string> OnRoomJoinError;
        public static event Action<int, string> OnPlayerEntered;
        public static event Action<int> OnPlayerLeft;
        public static event Action<string, string> OnRoomPropUpdated;
        public static event Action<int, string, string> OnPlayerPropUpdated;

        // 🔄 MATCHMAKING
        public static event Action<string> OnMatchmakingSearching;
        public static event Action OnMatchmakingCancelled;
        public static event Action<string> OnMatchFound;
        public static event Action OnMatchmakingTimeout;
        public static event Action OnEnteringMatch;
        public static event Action<string> OnMatchmakingError;

        // 🌐 NETWORK / PHOTON (crítico)
        public static event Action OnConnectingMaster;
        public static event Action OnConnectedMaster;
        public static event Action<string> OnConnectMasterError;
        public static event Action<int> OnPing;
        public static event Action<int> OnPingHigh;
        public static event Action<string> OnNetworkRegion;
        public static event Action<string> OnNetworkRegionAutoChanged;
        public static event Action<string> OnDisconnected;
        public static event Action<int> OnReconnecting;
        public static event Action<int> OnActorIDAssigned;
        public static event Action<int> OnActorIDRemoved;
        public static event Action<bool> OnIsHost;
        public static event Action<int, int> OnHostMigrated;
        public static event Action<string, string> OnRPCSent;
        public static event Action<string, int> OnRPCReceived;
        public static event Action<string> OnRPCIgnored;
        public static event Action OnRPCDiscarded;
        public static event Action<int> OnNetEventSent;
        public static event Action<int, int> OnNetEventReceived;
        public static event Action<int> OnMsgBuffer;
        public static event Action<int> OnPacketLost;
        public static event Action<int, int> OnNetSimActive;

        // 🧠 STATE MACHINE
        public static event Action<string> OnStateCurrent;
        public static event Action<string> OnStateInvalid;
        public static event Action<string, string> OnStateTransition;
        public static event Action<string, string, string> OnStateBlocked;
        public static event Action<string> OnStateSynced;

        // 🧍 PLAYER SYNC
        public static event Action<int, bool> OnPlayerReady;
        public static event Action<int> OnPlayerReadyTimeout;
        public static event Action<int, float> OnPlayerSceneLoaded;
        public static event Action OnSpawnRequested;
        public static event Action<int> OnSpawnConfirmed;
        public static event Action OnSpawnDenied;
        public static event Action<string, int> OnInputReceived;
        public static event Action OnInputDiscarded;
        public static event Action<int, string> OnAvatarAssigned;
        public static event Action<int> OnPlayerDisconnected;
        public static event Action OnPlayerReconnected;

        // 🔫 ARMAS / DISPARO / RECARGA
        public static event Action<int, string, int> OnWeaponEquipped;
        public static event Action<int, string, int> OnShootStart;
        public static event Action OnShootNoAmmo;
        public static event Action<int, float> OnShootConfirmed;
        public static event Action<int, int, string, float, float, float> OnProjectileCreated;
        public static event Action<int, int, float> OnImpactClient;
        public static event Action OnImpactDiscarded;
        public static event Action<int, int, float> OnImpactHost;
        public static event Action<int, string> OnReloadStart;
        public static event Action OnReloadComplete;
        public static event Action<int, string> OnReloadCancel;
        public static event Action<int, string, int, int, int> OnAmmoUpdated;

        // 💥 DAÑO / VIDA / MUERTE
        public static event Action<int, float, float, float, int> OnDamageApplied;
        public static event Action<int, float, float> OnHealthSynced;
        public static event Action<int, int, string> OnPlayerDeath;
        public static event Action<string> OnPlayerDeathCause;
        public static event Action<int, float> OnRespawnRequested;
        public static event Action OnRespawnExecuted;
        public static event Action<int, float> OnInvulnerability;
        public static event Action<int, float, int> OnHealReceived;

        // 🎯 GANAR / PERDER / FIN DE PARTIDA
        public static event Action<string> OnWinCondition;
        public static event Action<int> OnTeamWin;
        public static event Action<int> OnPlayerWin;
        public static event Action<string, int> OnMatchFinished;
        public static event Action<float> OnTotalTime;
        public static event Action<int, int> OnFinalScore;
        public static event Action<int, string, float> OnMVP;
        public static event Action OnMatchRestartReq;
        public static event Action OnReturnToLobby;

        // 🧩 NOTIFICACIONES / EVENTOS DE UI
        public static event Action<string, string> OnGlobalNotif;
        public static event Action<int, string> OnPersonalNotif;
        public static event Action<string, string> OnAlertShow;
        public static event Action OnAlertHide;
        public static event Action<int, string> OnStreakAnnounce;
        public static event Action<float, string> OnTimeWarning;
        public static event Action<string, int, int> OnObjectiveUpdate;

        // 🎞️ ANIMACIONES / ACTIVACIONES VISUALES
        public static event Action<int, string> OnAnimTrigger;
        public static event Action<int, string, float, float, float> OnAnimDamage;
        public static event Action<int, string> OnAnimDeath;
        public static event Action<int, string> OnAnimReload;
        public static event Action<string, float, float, float, int> OnVFX;
        public static event Action<string, int, float> OnSFX;
        public static event Action<float, float> OnCameraShake;
        public static event Action<int, string> OnDamageUI;
        public static event Action<int, string, bool> OnWeaponAnimBool;

        // 🔍 DESYNC / DEBUG AVANZADO
        public static event Action<int, int> OnSnapshotSent;
        public static event Action OnSnapshotReceived;
        public static event Action<int, int> OnHashDesync;
        public static event Action<int, string, string, string> OnDiffActor;
        public static event Action<int, string, string> OnCorrectionActor;
        public static event Action<string> OnForceResync;

        // 🔁 FALLBACKS / RECONEXIÓN
        public static event Action OnReconnectStart;
        public static event Action<string> OnReconnectSuccess;
        public static event Action OnReconnectDenied;
        public static event Action<int> OnStateRecoverHost;
        public static event Action<float> OnHostLost;
        public static event Action<int> OnNewHost;
        public static event Action<string> OnMatchAborted;

        // 🧪 TEST / DEBUG TOOLS
        public static event Action<int> OnTestSeed;
        public static event Action<string, int> OnTestBotCreate;
        public static event Action OnTestBotRemove;
        public static event Action<string, string> OnTestCmd;
        public static event Action<string> OnTestOverrideState;
        public static event Action<int, int> OnTestSimNet;
        public static event Action<float, float, float> OnTestPerf;
        public static event Action<int, string> OnTestDumpRoom;


        // ==========================================
        // TRIGGERS
        // ==========================================

        public static void TriggerGameStarted(string v, int s) => OnGameStarted?.Invoke(v, s);
        public static void TriggerGameClosed() => OnGameClosed?.Invoke();
        public static void TriggerSceneLoaded(string s) => OnSceneLoaded?.Invoke(s);
        public static void TriggerSceneLoadError(string s) => OnSceneLoadError?.Invoke(s);
        public static void TriggerGameMode(string s, int i) => OnGameMode?.Invoke(s, i);
        public static void TriggerRegion(string s) => OnRegion?.Invoke(s);
        public static void TriggerTargetFrameRate(int i) => OnTargetFrameRate?.Invoke(i);
        public static void TriggerVSync(bool b) => OnVSync?.Invoke(b);
        public static void TriggerEditor(bool b) => OnEditor?.Invoke(b);
        public static void TriggerPlatform(string s) => OnPlatform?.Invoke(s);
        public static void TriggerTimeScale(float f) => OnTimeScale?.Invoke(f);
        public static void TriggerTestMode(bool b) => OnTestMode?.Invoke(b);

        public static void TriggerNameEntered(string s) => OnNameEntered?.Invoke(s);
        public static void TriggerNameEmptyOrInvalid() => OnNameEmptyOrInvalid?.Invoke();
        public static void TriggerLocalIDAssigned(string s) => OnLocalIDAssigned?.Invoke(s);
        public static void TriggerExternalAuthStarted(string s) => OnExternalAuthStarted?.Invoke(s);
        public static void TriggerExternalAuthError(string s) => OnExternalAuthError?.Invoke(s);

        public static void TriggerRoomCreated(string s) => OnRoomCreated?.Invoke(s);
        public static void TriggerRoomCreateError(string s) => OnRoomCreateError?.Invoke(s);
        public static void TriggerRoomListReceived(int i) => OnRoomListReceived?.Invoke(i);
        public static void TriggerRoomJoin(string s) => OnRoomJoin?.Invoke(s);
        public static void TriggerRoomJoinError(string s) => OnRoomJoinError?.Invoke(s);
        public static void TriggerPlayerEntered(int i, string s) => OnPlayerEntered?.Invoke(i, s);
        public static void TriggerPlayerLeft(int i) => OnPlayerLeft?.Invoke(i);
        public static void TriggerRoomPropUpdated(string s1, string s2) => OnRoomPropUpdated?.Invoke(s1, s2);
        public static void TriggerPlayerPropUpdated(int i, string s1, string s2) => OnPlayerPropUpdated?.Invoke(i, s1, s2);

        public static void TriggerMatchmakingSearching(string s) => OnMatchmakingSearching?.Invoke(s);
        public static void TriggerMatchmakingCancelled() => OnMatchmakingCancelled?.Invoke();
        public static void TriggerMatchFound(string s) => OnMatchFound?.Invoke(s);
        public static void TriggerMatchmakingTimeout() => OnMatchmakingTimeout?.Invoke();
        public static void TriggerEnteringMatch() => OnEnteringMatch?.Invoke();
        public static void TriggerMatchmakingError(string s) => OnMatchmakingError?.Invoke(s);

        public static void TriggerConnectingMaster() => OnConnectingMaster?.Invoke();
        public static void TriggerConnectedMaster() => OnConnectedMaster?.Invoke();
        public static void TriggerConnectMasterError(string s) => OnConnectMasterError?.Invoke(s);
        public static void TriggerPing(int i) => OnPing?.Invoke(i);
        public static void TriggerPingHigh(int i) => OnPingHigh?.Invoke(i);
        public static void TriggerNetworkRegion(string s) => OnNetworkRegion?.Invoke(s);
        public static void TriggerNetworkRegionAutoChanged(string s) => OnNetworkRegionAutoChanged?.Invoke(s);
        public static void TriggerDisconnected(string s) => OnDisconnected?.Invoke(s);
        public static void TriggerReconnecting(int i) => OnReconnecting?.Invoke(i);
        public static void TriggerActorIDAssigned(int i) => OnActorIDAssigned?.Invoke(i);
        public static void TriggerActorIDRemoved(int i) => OnActorIDRemoved?.Invoke(i);
        public static void TriggerIsHost(bool b) => OnIsHost?.Invoke(b);
        public static void TriggerHostMigrated(int i1, int i2) => OnHostMigrated?.Invoke(i1, i2);
        public static void TriggerRPCSent(string s1, string s2) => OnRPCSent?.Invoke(s1, s2);
        public static void TriggerRPCReceived(string s, int i) => OnRPCReceived?.Invoke(s, i);
        public static void TriggerRPCIgnored(string s) => OnRPCIgnored?.Invoke(s);
        public static void TriggerRPCDiscarded() => OnRPCDiscarded?.Invoke();
        public static void TriggerNetEventSent(int i) => OnNetEventSent?.Invoke(i);
        public static void TriggerNetEventReceived(int i1, int i2) => OnNetEventReceived?.Invoke(i1, i2);
        public static void TriggerMsgBuffer(int i) => OnMsgBuffer?.Invoke(i);
        public static void TriggerPacketLost(int i) => OnPacketLost?.Invoke(i);
        public static void TriggerNetSimActive(int i1, int i2) => OnNetSimActive?.Invoke(i1, i2);

        public static void TriggerStateCurrent(string s) => OnStateCurrent?.Invoke(s);
        public static void TriggerStateInvalid(string s) => OnStateInvalid?.Invoke(s);
        public static void TriggerStateTransition(string s1, string s2) => OnStateTransition?.Invoke(s1, s2);
        public static void TriggerStateBlocked(string s1, string s2, string s3) => OnStateBlocked?.Invoke(s1, s2, s3);
        public static void TriggerStateSynced(string s) => OnStateSynced?.Invoke(s);

        public static void TriggerPlayerReady(int i, bool b) => OnPlayerReady?.Invoke(i, b);
        public static void TriggerPlayerReadyTimeout(int i) => OnPlayerReadyTimeout?.Invoke(i);
        public static void TriggerPlayerSceneLoaded(int i, float f) => OnPlayerSceneLoaded?.Invoke(i, f);
        public static void TriggerSpawnRequestedLocal() => OnSpawnRequested?.Invoke();
        public static void TriggerSpawnConfirmed(int i) => OnSpawnConfirmed?.Invoke(i);
        public static void TriggerSpawnDenied() => OnSpawnDenied?.Invoke();
        public static void TriggerInputReceived(string s, int i) => OnInputReceived?.Invoke(s, i);
        public static void TriggerInputDiscarded() => OnInputDiscarded?.Invoke();
        public static void TriggerAvatarAssigned(int i, string s) => OnAvatarAssigned?.Invoke(i, s);
        public static void TriggerPlayerDisconnected(int i) => OnPlayerDisconnected?.Invoke(i);
        public static void TriggerPlayerReconnected() => OnPlayerReconnected?.Invoke();

        public static void TriggerWeaponEquipped(int i1, string s, int i2) => OnWeaponEquipped?.Invoke(i1, s, i2);
        public static void TriggerShootStart(int i1, string s, int i2) => OnShootStart?.Invoke(i1, s, i2);
        public static void TriggerShootNoAmmo() => OnShootNoAmmo?.Invoke();
        public static void TriggerShootConfirmed(int i, float f) => OnShootConfirmed?.Invoke(i, f);
        public static void TriggerProjectileCreated(int i1, int i2, string s, float f1, float f2, float f3) => OnProjectileCreated?.Invoke(i1, i2, s, f1, f2, f3);
        public static void TriggerImpactClient(int i1, int i2, float f) => OnImpactClient?.Invoke(i1, i2, f);
        public static void TriggerImpactDiscarded() => OnImpactDiscarded?.Invoke();
        public static void TriggerImpactHost(int i1, int i2, float f) => OnImpactHost?.Invoke(i1, i2, f);
        public static void TriggerReloadStart(int i, string s) => OnReloadStart?.Invoke(i, s);
        public static void TriggerReloadComplete() => OnReloadComplete?.Invoke();
        public static void TriggerReloadCancel(int i, string s) => OnReloadCancel?.Invoke(i, s);
        public static void TriggerAmmoUpdated(int i1, string s, int i2, int i3, int i4) => OnAmmoUpdated?.Invoke(i1, s, i2, i3, i4);

        public static void TriggerDamageApplied(int i1, float f1, float f2, float f3, int i2) => OnDamageApplied?.Invoke(i1, f1, f2, f3, i2);
        public static void TriggerHealthSynced(int i, float f1, float f2) => OnHealthSynced?.Invoke(i, f1, f2);
        public static void TriggerPlayerDeath(int i1, int i2, string s) => OnPlayerDeath?.Invoke(i1, i2, s);
        public static void TriggerPlayerDeathCause(string s) => OnPlayerDeathCause?.Invoke(s);
        public static void TriggerRespawnRequested(int i, float f) => OnRespawnRequested?.Invoke(i, f);
        public static void TriggerRespawnExecuted() => OnRespawnExecuted?.Invoke();
        public static void TriggerInvulnerability(int i, float f) => OnInvulnerability?.Invoke(i, f);
        public static void TriggerHealReceived(int i1, float f, int i2) => OnHealReceived?.Invoke(i1, f, i2);

        public static void TriggerWinCondition(string s) => OnWinCondition?.Invoke(s);
        public static void TriggerTeamWin(int i) => OnTeamWin?.Invoke(i);
        public static void TriggerPlayerWin(int i) => OnPlayerWin?.Invoke(i);
        public static void TriggerMatchFinished(string s, int i) => OnMatchFinished?.Invoke(s, i);
        public static void TriggerTotalTime(float f) => OnTotalTime?.Invoke(f);
        public static void TriggerFinalScore(int i1, int i2) => OnFinalScore?.Invoke(i1, i2);
        public static void TriggerMVP(int i, string s, float f) => OnMVP?.Invoke(i, s, f);
        public static void TriggerMatchRestartReq() => OnMatchRestartReq?.Invoke();
        public static void TriggerReturnToLobby() => OnReturnToLobby?.Invoke();

        public static void TriggerGlobalNotif(string s1, string s2) => OnGlobalNotif?.Invoke(s1, s2);
        public static void TriggerPersonalNotif(int i, string s) => OnPersonalNotif?.Invoke(i, s);
        public static void TriggerAlertShow(string s1, string s2) => OnAlertShow?.Invoke(s1, s2);
        public static void TriggerAlertHide() => OnAlertHide?.Invoke();
        public static void TriggerStreakAnnounce(int i, string s) => OnStreakAnnounce?.Invoke(i, s);
        public static void TriggerTimeWarning(float f, string s) => OnTimeWarning?.Invoke(f, s);
        public static void TriggerObjectiveUpdate(string s, int i1, int i2) => OnObjectiveUpdate?.Invoke(s, i1, i2);

        public static void TriggerAnimTrigger(int i, string s) => OnAnimTrigger?.Invoke(i, s);
        public static void TriggerAnimDamage(int i, string s, float f1, float f2, float f3) => OnAnimDamage?.Invoke(i, s, f1, f2, f3);
        public static void TriggerAnimDeath(int i, string s) => OnAnimDeath?.Invoke(i, s);
        public static void TriggerAnimReload(int i, string s) => OnAnimReload?.Invoke(i, s);
        public static void TriggerVFX(string s, float f1, float f2, float f3, int i) => OnVFX?.Invoke(s, f1, f2, f3, i);
        public static void TriggerSFX(string s, int i, float f) => OnSFX?.Invoke(s, i, f);
        public static void TriggerCameraShake(float f1, float f2) => OnCameraShake?.Invoke(f1, f2);
        public static void TriggerDamageUI(int i, string s) => OnDamageUI?.Invoke(i, s);
        public static void TriggerWeaponAnimBool(int i, string s, bool b) => OnWeaponAnimBool?.Invoke(i, s, b);

        public static void TriggerSnapshotSent(int i1, int i2) => OnSnapshotSent?.Invoke(i1, i2);
        public static void TriggerSnapshotReceived() => OnSnapshotReceived?.Invoke();
        public static void TriggerHashDesync(int i1, int i2) => OnHashDesync?.Invoke(i1, i2);
        public static void TriggerDiffActor(int i, string s1, string s2, string s3) => OnDiffActor?.Invoke(i, s1, s2, s3);
        public static void TriggerCorrectionActor(int i, string s1, string s2) => OnCorrectionActor?.Invoke(i, s1, s2);
        public static void TriggerForceResync(string s) => OnForceResync?.Invoke(s);

        public static void TriggerReconnectStart() => OnReconnectStart?.Invoke();
        public static void TriggerReconnectSuccess(string s) => OnReconnectSuccess?.Invoke(s);
        public static void TriggerReconnectDenied() => OnReconnectDenied?.Invoke();
        public static void TriggerStateRecoverHost(int i) => OnStateRecoverHost?.Invoke(i);
        public static void TriggerHostLost(float f) => OnHostLost?.Invoke(f);
        public static void TriggerNewHost(int i) => OnNewHost?.Invoke(i);
        public static void TriggerMatchAborted(string s) => OnMatchAborted?.Invoke(s);

        public static void TriggerTestSeed(int i) => OnTestSeed?.Invoke(i);
        public static void TriggerTestBotCreate(string s, int i) => OnTestBotCreate?.Invoke(s, i);
        public static void TriggerTestBotRemove() => OnTestBotRemove?.Invoke();
        public static void TriggerTestCmd(string s1, string s2) => OnTestCmd?.Invoke(s1, s2);
        public static void TriggerTestOverrideState(string s) => OnTestOverrideState?.Invoke(s);
        public static void TriggerTestSimNet(int i1, int i2) => OnTestSimNet?.Invoke(i1, i2);
        public static void TriggerTestPerf(float f1, float f2, float f3) => OnTestPerf?.Invoke(f1, f2, f3);
        public static void TriggerTestDumpRoom(int i, string s) => OnTestDumpRoom?.Invoke(i, s);

        public static void ClearAllEvents()
        {
            OnGameStarted = null;
            OnGameClosed = null;
            OnSceneLoaded = null;
            OnSceneLoadError = null;
            OnGameMode = null;
            OnRegion = null;
            OnTargetFrameRate = null;
            OnVSync = null;
            OnEditor = null;
            OnPlatform = null;
            OnTimeScale = null;
            OnTestMode = null;

            OnNameEntered = null;
            OnNameEmptyOrInvalid = null;
            OnLocalIDAssigned = null;
            OnExternalAuthStarted = null;
            OnExternalAuthError = null;

            OnRoomCreated = null;
            OnRoomCreateError = null;
            OnRoomListReceived = null;
            OnRoomJoin = null;
            OnRoomJoinError = null;
            OnPlayerEntered = null;
            OnPlayerLeft = null;
            OnRoomPropUpdated = null;
            OnPlayerPropUpdated = null;

            OnMatchmakingSearching = null;
            OnMatchmakingCancelled = null;
            OnMatchFound = null;
            OnMatchmakingTimeout = null;
            OnEnteringMatch = null;
            OnMatchmakingError = null;

            OnConnectingMaster = null;
            OnConnectedMaster = null;
            OnConnectMasterError = null;
            OnPing = null;
            OnPingHigh = null;
            OnNetworkRegion = null;
            OnNetworkRegionAutoChanged = null;
            OnDisconnected = null;
            OnReconnecting = null;
            OnActorIDAssigned = null;
            OnActorIDRemoved = null;
            OnIsHost = null;
            OnHostMigrated = null;
            OnRPCSent = null;
            OnRPCReceived = null;
            OnRPCIgnored = null;
            OnRPCDiscarded = null;
            OnNetEventSent = null;
            OnNetEventReceived = null;
            OnMsgBuffer = null;
            OnPacketLost = null;
            OnNetSimActive = null;

            OnStateCurrent = null;
            OnStateInvalid = null;
            OnStateTransition = null;
            OnStateBlocked = null;
            OnStateSynced = null;

            OnPlayerReady = null;
            OnPlayerReadyTimeout = null;
            OnPlayerSceneLoaded = null;
            OnSpawnRequested = null;
            OnSpawnConfirmed = null;
            OnSpawnDenied = null;
            OnInputReceived = null;
            OnInputDiscarded = null;
            OnAvatarAssigned = null;
            OnPlayerDisconnected = null;
            OnPlayerReconnected = null;

            OnWeaponEquipped = null;
            OnShootStart = null;
            OnShootNoAmmo = null;
            OnShootConfirmed = null;
            OnProjectileCreated = null;
            OnImpactClient = null;
            OnImpactDiscarded = null;
            OnImpactHost = null;
            OnReloadStart = null;
            OnReloadComplete = null;
            OnReloadCancel = null;
            OnAmmoUpdated = null;

            OnDamageApplied = null;
            OnHealthSynced = null;
            OnPlayerDeath = null;
            OnPlayerDeathCause = null;
            OnRespawnRequested = null;
            OnRespawnExecuted = null;
            OnInvulnerability = null;
            OnHealReceived = null;

            OnWinCondition = null;
            OnTeamWin = null;
            OnPlayerWin = null;
            OnMatchFinished = null;
            OnTotalTime = null;
            OnFinalScore = null;
            OnMVP = null;
            OnMatchRestartReq = null;
            OnReturnToLobby = null;

            OnGlobalNotif = null;
            OnPersonalNotif = null;
            OnAlertShow = null;
            OnAlertHide = null;
            OnStreakAnnounce = null;
            OnTimeWarning = null;
            OnObjectiveUpdate = null;

            OnAnimTrigger = null;
            OnAnimDamage = null;
            OnAnimDeath = null;
            OnAnimReload = null;
            OnVFX = null;
            OnSFX = null;
            OnCameraShake = null;
            OnDamageUI = null;
            OnWeaponAnimBool = null;

            OnSnapshotSent = null;
            OnSnapshotReceived = null;
            OnHashDesync = null;
            OnDiffActor = null;
            OnCorrectionActor = null;
            OnForceResync = null;

            OnReconnectStart = null;
            OnReconnectSuccess = null;
            OnReconnectDenied = null;
            OnStateRecoverHost = null;
            OnHostLost = null;
            OnNewHost = null;
            OnMatchAborted = null;

            OnTestSeed = null;
            OnTestBotCreate = null;
            OnTestBotRemove = null;
            OnTestCmd = null;
            OnTestOverrideState = null;
            OnTestSimNet = null;
            OnTestPerf = null;
            OnTestDumpRoom = null;
        }
    }
}
