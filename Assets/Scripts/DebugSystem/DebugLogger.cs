using System.Collections.Generic;
using UnityEngine;

namespace DebugSystem
{
    public enum LogCategory
    {
        SYSTEM = 1,
        AUTH = 2,
        LOBBY = 3,
        MATCHMAKING = 4,
        NETWORK = 5,
        STATE_MACHINE = 6,
        PLAYER_SYNC = 7,
        WEAPON = 8,
        DAMAGE = 9,
        WIN_LOSE = 10,
        NOTIFICATIONS = 11,
        ANIMATIONS = 12,
        DESYNC = 13,
        FALLBACKS = 14,
        TEST = 15
    }

    public struct LogEntry
    {
        public LogCategory Category;
        public string EventKey;
        public string FormattedMessage;
        public float TimeStamp;
    }

    public class DebugLogger : MonoBehaviour
    {
        public static DebugLogger Instance { get; private set; }

        public bool IsTestMode = false;
        public List<LogEntry> LoggedEvents = new List<LogEntry>();

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
                SubscribeEvents();
            }
            else
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ClearLogs()
        {
            LoggedEvents.Clear();
        }

        private void Log(LogCategory category, string eventKey, string formattedMsg)
        {
            LogEntry entry = new LogEntry
            {
                Category = category,
                EventKey = eventKey,
                FormattedMessage = formattedMsg,
                TimeStamp = Time.time
            };

            if (IsTestMode)
            {
                LoggedEvents.Add(entry);
            }

            Debug.Log($"[{category}] {formattedMsg}");
        }

        private void SubscribeEvents()
        {
            // 🔩 SYSTEM
            EventBus.OnGameStarted += (v, s) => Log(LogCategory.SYSTEM, "GAME_STARTED", $"Juego iniciado (versión {v} / seed {s})");
            EventBus.OnGameClosed += () => Log(LogCategory.SYSTEM, "GAME_CLOSED", "Juego cerrado correctamente");
            EventBus.OnSceneLoaded += (s) => Log(LogCategory.SYSTEM, "SCENE_LOADED", $"Escena cargada: {s}");
            EventBus.OnSceneLoadError += (s) => Log(LogCategory.SYSTEM, "SCENE_LOAD_ERROR", $"Error al cargar escena: {s}");
            EventBus.OnGameMode += (s, i) => Log(LogCategory.SYSTEM, "GAME_MODE", $"Modo de juego: {s} (players: {i})");
            EventBus.OnRegion += (s) => Log(LogCategory.SYSTEM, "REGION", $"Región: {s}");
            EventBus.OnTargetFrameRate += (i) => Log(LogCategory.SYSTEM, "TARGET_FRAMERATE", $"TargetFrameRate: {i}");
            EventBus.OnVSync += (b) => Log(LogCategory.SYSTEM, "VSYNC", $"VSync: {b}");
            EventBus.OnEditor += (b) => Log(LogCategory.SYSTEM, "EDITOR", $"Editor: {b}");
            EventBus.OnPlatform += (s) => Log(LogCategory.SYSTEM, "PLATFORM", $"Plataforma: {s}");
            EventBus.OnTimeScale += (f) => Log(LogCategory.SYSTEM, "TIME_SCALE", $"Time.timeScale: {f}");
            EventBus.OnTestMode += (b) => Log(LogCategory.SYSTEM, "TEST_MODE", $"Modo test activado: {b}");

            // 🔐 AUTH / IDENTIDAD
            EventBus.OnNameEntered += (s) => Log(LogCategory.AUTH, "NAME_ENTERED", $"Nombre ingresado: {s}");
            EventBus.OnNameEmptyOrInvalid += () => Log(LogCategory.AUTH, "NAME_INVALID", "Nombre vacío o inválido");
            EventBus.OnLocalIDAssigned += (s) => Log(LogCategory.AUTH, "LOCAL_ID", $"ID local asignado: {s} (UID)");
            EventBus.OnExternalAuthStarted += (s) => Log(LogCategory.AUTH, "AUTH_EXTERNAL_START", $"Autenticación externa iniciada: {s}");
            EventBus.OnExternalAuthError += (s) => Log(LogCategory.AUTH, "AUTH_EXTERNAL_ERROR", $"Error: {s}");

            // 🏠 LOBBY / SALAS
            EventBus.OnRoomCreated += (s) => Log(LogCategory.LOBBY, "ROOM_CREATED", $"Sala creada: {s} (ID)");
            EventBus.OnRoomCreateError += (s) => Log(LogCategory.LOBBY, "ROOM_CREATE_ERROR", $"Error al crear sala: {s}");
            EventBus.OnRoomListReceived += (i) => Log(LogCategory.LOBBY, "ROOM_LIST", $"Lista de salas recibida: {i} salas");
            EventBus.OnRoomJoin += (s) => Log(LogCategory.LOBBY, "ROOM_JOIN", $"Unirse a sala: {s}");
            EventBus.OnRoomJoinError += (s) => Log(LogCategory.LOBBY, "ROOM_JOIN_ERROR", $"Error al unirse: {s}");
            EventBus.OnPlayerEntered += (i, s) => Log(LogCategory.LOBBY, "PLAYER_ENTERED", $"Jugador entró: {i} (actor), nombre {s}");
            EventBus.OnPlayerLeft += (i) => Log(LogCategory.LOBBY, "PLAYER_LEFT", $"Jugador salió: {i}");
            EventBus.OnRoomPropUpdated += (s1, s2) => Log(LogCategory.LOBBY, "ROOM_PROP_UPDATED", $"Propiedad de sala actualizada: {s1} = {s2}");
            EventBus.OnPlayerPropUpdated += (i, s1, s2) => Log(LogCategory.LOBBY, "PLAYER_PROP_UPDATED", $"Propiedad de jugador actualizada: actor {i}, {s1} = {s2}");

            // 🔄 MATCHMAKING
            EventBus.OnMatchmakingSearching += (s) => Log(LogCategory.MATCHMAKING, "MATCHMAKING_SEARCHING", $"Buscando partida (tipo: {s})");
            EventBus.OnMatchmakingCancelled += () => Log(LogCategory.MATCHMAKING, "MATCHMAKING_CANCELLED", "Matchmaking cancelado");
            EventBus.OnMatchFound += (s) => Log(LogCategory.MATCHMAKING, "MATCH_FOUND", $"Partida encontrada: sala {s}");
            EventBus.OnMatchmakingTimeout += () => Log(LogCategory.MATCHMAKING, "MATCHMAKING_TIMEOUT", "Timeout de búsqueda");
            EventBus.OnEnteringMatch += () => Log(LogCategory.MATCHMAKING, "ENTERING_MATCH", "Entrando a partida...");
            EventBus.OnMatchmakingError += (s) => Log(LogCategory.MATCHMAKING, "MATCHMAKING_ERROR", $"Error entrada: {s}");

            // 🌐 NETWORK / PHOTON (crítico)
            EventBus.OnConnectingMaster += () => Log(LogCategory.NETWORK, "CONNECTING_MASTER", "Conectando a Master Server…");
            EventBus.OnConnectedMaster += () => Log(LogCategory.NETWORK, "CONNECTED_MASTER", "Conectado");
            EventBus.OnConnectMasterError += (s) => Log(LogCategory.NETWORK, "CONNECT_MASTER_ERROR", $"Error: {s}");
            EventBus.OnPing += (i) => Log(LogCategory.NETWORK, "PING", $"Ping: {i} ms");
            EventBus.OnPingHigh += (i) => Log(LogCategory.NETWORK, "PING_HIGH", $"Ping alto (> {i} ms)");
            EventBus.OnNetworkRegion += (s) => Log(LogCategory.NETWORK, "NET_REGION", $"Región: {s}");
            EventBus.OnNetworkRegionAutoChanged += (s) => Log(LogCategory.NETWORK, "NET_REGION_AUTO", $"Cambio automático a: {s}");
            EventBus.OnDisconnected += (s) => Log(LogCategory.NETWORK, "DISCONNECTED", $"Desconectado: {s}");
            EventBus.OnReconnecting += (i) => Log(LogCategory.NETWORK, "RECONNECTING", $"Reconectando (intento {i})");
            EventBus.OnActorIDAssigned += (i) => Log(LogCategory.NETWORK, "ACTOR_ASSIGNED", $"ActorID asignado: {i}");
            EventBus.OnActorIDRemoved += (i) => Log(LogCategory.NETWORK, "ACTOR_REMOVED", $"ActorID removido: {i}");
            EventBus.OnIsHost += (b) => Log(LogCategory.NETWORK, "IS_HOST", $"Soy host: {b}");
            EventBus.OnHostMigrated += (i1, i2) => Log(LogCategory.NETWORK, "HOST_MIGRATED", $"Host migrado: nuevo {i1}, anterior {i2}");
            EventBus.OnRPCSent += (s1, s2) => Log(LogCategory.NETWORK, "RPC_SENT", $"RPC enviado: {s1} (destino: {s2})");
            EventBus.OnRPCReceived += (s, i) => Log(LogCategory.NETWORK, "RPC_RECEIVED", $"RPC recibido: {s} de {i}");
            EventBus.OnRPCIgnored += (s) => Log(LogCategory.NETWORK, "RPC_IGNORED", $"RPC ignorado: {s}");
            EventBus.OnRPCDiscarded += () => Log(LogCategory.NETWORK, "RPC_DISCARDED", "RPC descartado (objeto null)");
            EventBus.OnNetEventSent += (i) => Log(LogCategory.NETWORK, "NET_EVENT_SENT", $"Evento de red enviado: código {i}");
            EventBus.OnNetEventReceived += (i1, i2) => Log(LogCategory.NETWORK, "NET_EVENT_RECEIVED", $"Evento recibido: código {i1} de {i2}");
            EventBus.OnMsgBuffer += (i) => Log(LogCategory.NETWORK, "MSG_BUFFER", $"Buffer de mensajes: tamaño {i}");
            EventBus.OnPacketLost += (i) => Log(LogCategory.NETWORK, "PACKET_LOST", $"Paquete perdido (seq: {i})");
            EventBus.OnNetSimActive += (i1, i2) => Log(LogCategory.NETWORK, "NET_SIM_ACTIVE", $"Simulación activa: latencia {i1} ms, pérdida {i2}%");

            // 🧠 STATE MACHINE
            EventBus.OnStateCurrent += (s) => Log(LogCategory.STATE_MACHINE, "STATE_CURRENT", $"Estado actual: {s}");
            EventBus.OnStateInvalid += (s) => Log(LogCategory.STATE_MACHINE, "STATE_INVALID", $"Estado inválido: {s}");
            EventBus.OnStateTransition += (s1, s2) => Log(LogCategory.STATE_MACHINE, "STATE_TRANSITION", $"Transición: {s1} → {s2}");
            EventBus.OnStateBlocked += (s1, s2, s3) => Log(LogCategory.STATE_MACHINE, "STATE_BLOCKED", $"Bloqueada (condición: {s3})"); // s1/s2 omitted in UI string requirement
            EventBus.OnStateSynced += (s) => Log(LogCategory.STATE_MACHINE, "STATE_SYNCED", $"Estado sincronizado en red: {s}");

            // 🧍 PLAYER SYNC
            EventBus.OnPlayerReady += (i, b) => Log(LogCategory.PLAYER_SYNC, "PLAYER_READY", $"Jugador listo: actor {i}, listo={b}");
            EventBus.OnPlayerReadyTimeout += (i) => Log(LogCategory.PLAYER_SYNC, "PLAYER_READY_TIMEOUT", $"Timeout listo actor {i}");
            EventBus.OnPlayerSceneLoaded += (i, f) => Log(LogCategory.PLAYER_SYNC, "PLAYER_SCENE_LOADED", $"Escena cargada: actor {i}, progreso {f}%");
            EventBus.OnSpawnRequested += () => Log(LogCategory.PLAYER_SYNC, "SPAWN_REQ", "Spawn solicitado (local)");
            EventBus.OnSpawnConfirmed += (i) => Log(LogCategory.PLAYER_SYNC, "SPAWN_CONF", $"Spawn confirmado: actor {i}");
            EventBus.OnSpawnDenied += () => Log(LogCategory.PLAYER_SYNC, "SPAWN_DENIED", "Denegado");
            EventBus.OnInputReceived += (s, i) => {
                if (s != "Move")
                {
                    Log(LogCategory.PLAYER_SYNC, "INPUT_RECEIVED", $"Input recibido: tipo {s}, frame {i}");
                }
            };
            EventBus.OnInputDiscarded += () => Log(LogCategory.PLAYER_SYNC, "INPUT_DISCARDED", "Input descartado");
            EventBus.OnAvatarAssigned += (i, s) => Log(LogCategory.PLAYER_SYNC, "AVATAR_ASSIGNED", $"Avatar asignado: actor {i}, prefab {s}");
            EventBus.OnPlayerDisconnected += (i) => Log(LogCategory.PLAYER_SYNC, "PLAYER_DISCONNECTED", $"Jugador desconectado: actor {i} (timeout)");
            EventBus.OnPlayerReconnected += () => Log(LogCategory.PLAYER_SYNC, "PLAYER_RECONNECTED", "Reconectado");

            // 🔫 ARMAS / DISPARO / RECARGA
            EventBus.OnWeaponEquipped += (i1, s, i2) => Log(LogCategory.WEAPON, "WEAPON_EQUIP", $"Arma equipada: actor {i1}, arma {s} (índice {i2})");
            EventBus.OnShootStart += (i1, s, i2) => Log(LogCategory.WEAPON, "SHOOT_START", $"Disparo iniciado: actor {i1}, arma {s}, balas restantes {i2}");
            EventBus.OnShootNoAmmo += () => Log(LogCategory.WEAPON, "SHOOT_NO_AMMO", "Sin munición");
            EventBus.OnShootConfirmed += (i, f) => Log(LogCategory.WEAPON, "SHOOT_CONF", $"Disparo confirmado (servidor): actor {i}, timestamp {f}");
            EventBus.OnProjectileCreated += (i1, i2, s, f1, f2, f3) => Log(LogCategory.WEAPON, "PROJ_CREATED", $"Proyectil creado: ID {i1}, actor {i2}, tipo {s}, posición ({f1},{f2},{f3})");
            EventBus.OnImpactClient += (i1, i2, f) => Log(LogCategory.WEAPON, "IMPACT_CLIENT", $"Impacto detectado (cliente): proyectil {i1}, objetivo {i2}, daño base {f}");
            EventBus.OnImpactDiscarded += () => Log(LogCategory.WEAPON, "IMPACT_DISCARDED", "Impacto descartado (fuera de rango)");
            EventBus.OnImpactHost += (i1, i2, f) => Log(LogCategory.WEAPON, "IMPACT_HOST", $"Impacto validado (host): proyectil {i1}, objetivo {i2}, daño final {f}");
            EventBus.OnReloadStart += (i, s) => Log(LogCategory.WEAPON, "RELOAD_START", $"Recarga iniciada: actor {i}, arma {s}");
            EventBus.OnReloadComplete += () => Log(LogCategory.WEAPON, "RELOAD_COMP", "Recarga completada");
            EventBus.OnReloadCancel += (i, s) => Log(LogCategory.WEAPON, "RELOAD_CANCEL", $"Recarga cancelada: actor {i}, arma {s}");
            EventBus.OnAmmoUpdated += (i1, s, i2, i3, i4) => Log(LogCategory.WEAPON, "AMMO_UPDATED", $"Munición actualizada: actor {i1}, arma {s}, cargador {i2}/{i3}, reserva {i4}");

            // 💥 DAÑO / VIDA / MUERTE
            EventBus.OnDamageApplied += (i1, f1, f2, f3, i2) => Log(LogCategory.DAMAGE, "DAMAGE_APPLIED", $"Daño aplicado: objetivo {i1}, cantidad {f1}, HP {f2} → {f3}, causante {i2}");
            EventBus.OnHealthSynced += (i, f1, f2) => Log(LogCategory.DAMAGE, "HEALTH_SYNCED", $"Vida sincronizada: actor {i}, HP {f1}, escudo {f2}");
            EventBus.OnPlayerDeath += (i1, i2, s) => Log(LogCategory.DAMAGE, "PLAYER_DEATH", $"Muerte de jugador: actor {i1}, asesino {i2}, arma {s}");
            EventBus.OnPlayerDeathCause += (s) => Log(LogCategory.DAMAGE, "PLAYER_DEATH_CAUSE", $"Causa: {s}");
            EventBus.OnRespawnRequested += (i, f) => Log(LogCategory.DAMAGE, "RESPAWN_REQ", $"Respawn solicitado: actor {i} en {f}s");
            EventBus.OnRespawnExecuted += () => Log(LogCategory.DAMAGE, "RESPAWN_EXEC", "Respawn ejecutado");
            EventBus.OnInvulnerability += (i, f) => Log(LogCategory.DAMAGE, "INVULNERABILITY", $"Invulnerabilidad activada: actor {i}, duración {f}s");
            EventBus.OnHealReceived += (i1, f, i2) => Log(LogCategory.DAMAGE, "HEAL_RECEIVED", $"Curación recibida: actor {i1}, cantidad {f}, fuente {i2}");

            // 🎯 GANAR / PERDER / FIN DE PARTIDA
            EventBus.OnWinCondition += (s) => Log(LogCategory.WIN_LOSE, "WIN_COND", $"Condición de victoria alcanzada: {s}");
            EventBus.OnTeamWin += (i) => Log(LogCategory.WIN_LOSE, "TEAM_WIN", $"Equipo ganador: {i}");
            EventBus.OnPlayerWin += (i) => Log(LogCategory.WIN_LOSE, "PLAYER_WIN", $"Jugador ganador: {i}");
            EventBus.OnMatchFinished += (s, i) => Log(LogCategory.WIN_LOSE, "MATCH_FINISHED", $"Partida finalizada: resultado {s} para jugador {i}");
            EventBus.OnTotalTime += (f) => Log(LogCategory.WIN_LOSE, "TOTAL_TIME", $"Tiempo total de partida: {f}s");
            EventBus.OnFinalScore += (i1, i2) => Log(LogCategory.WIN_LOSE, "FINAL_SCORE", $"Marcador final: equipo {i1} = {i2} puntos");
            EventBus.OnMVP += (i, s, f) => Log(LogCategory.WIN_LOSE, "MVP", $"MVP: actor {i}, estadística {s} = {f}");
            EventBus.OnMatchRestartReq += () => Log(LogCategory.WIN_LOSE, "RESTART_REQ", "Reinicio de partida solicitado");
            EventBus.OnReturnToLobby += () => Log(LogCategory.WIN_LOSE, "RETURN_LOBBY", "Retorno al lobby");

            // 🧩 NOTIFICACIONES / EVENTOS DE UI
            EventBus.OnGlobalNotif += (s1, s2) => Log(LogCategory.NOTIFICATIONS, "GLOBAL_NOTIF", $"Notificación global: mensaje \"{s1}\" (tipo {s2})");
            EventBus.OnPersonalNotif += (i, s) => Log(LogCategory.NOTIFICATIONS, "PERSONAL_NOTIF", $"Notificación personal: actor {i}, mensaje \"{s}\"");
            EventBus.OnAlertShow += (s1, s2) => Log(LogCategory.NOTIFICATIONS, "ALERT_SHOW", $"Alerta mostrada: {s1} (icono {s2})");
            EventBus.OnAlertHide += () => Log(LogCategory.NOTIFICATIONS, "ALERT_HIDE", "Ocultada");
            EventBus.OnStreakAnnounce += (i, s) => Log(LogCategory.NOTIFICATIONS, "STREAK", $"Anuncio de racha: actor {i}, racha {s}");
            EventBus.OnTimeWarning += (f, s) => Log(LogCategory.NOTIFICATIONS, "TIME_WARN", $"Tiempo restante aviso: {f}s para {s}");
            EventBus.OnObjectiveUpdate += (s, i1, i2) => Log(LogCategory.NOTIFICATIONS, "OBJECTIVE", $"Objetivo de misión actualizado: {s} = {i1}/{i2}");

            // 🎞️ ANIMACIONES / ACTIVACIONES VISUALES
            EventBus.OnAnimTrigger += (i, s) => Log(LogCategory.ANIMATIONS, "ANIM_TRIGGER", $"Animación disparada: actor {i}, trigger \"{s}\"");
            EventBus.OnAnimDamage += (i, s, f1, f2, f3) => Log(LogCategory.ANIMATIONS, "ANIM_DAMAGE", $"Animación de daño: actor {i}, tipo {s}, dirección ({f1},{f2},{f3})");
            EventBus.OnAnimDeath += (i, s) => Log(LogCategory.ANIMATIONS, "ANIM_DEATH", $"Animación de muerte: actor {i}, variante {s}");
            EventBus.OnAnimReload += (i, s) => Log(LogCategory.ANIMATIONS, "ANIM_RELOAD", $"Animación de recarga: actor {i}, tipo {s}");
            EventBus.OnVFX += (s, f1, f2, f3, i) => Log(LogCategory.ANIMATIONS, "VFX", $"Efecto visual activado: prefab {s}, posición ({f1},{f2},{f3}), actor {i}");
            EventBus.OnSFX += (s, i, f) => Log(LogCategory.ANIMATIONS, "SFX", $"Efecto de sonido: clip {s}, actor {i}, volumen {f}");
            EventBus.OnCameraShake += (f1, f2) => Log(LogCategory.ANIMATIONS, "CAM_SHAKE", $"Cámara sacudida: intensidad {f1}, duración {f2}");
            EventBus.OnDamageUI += (i, s) => Log(LogCategory.ANIMATIONS, "DAMAGE_UI", $"Interfaz de daño activada: actor {i}, dirección {s}");
            EventBus.OnWeaponAnimBool += (i, s, b) => Log(LogCategory.ANIMATIONS, "WEAPON_ANIM_BOOL", $"Estado de arma en animación: actor {i}, bool {s} = {b}");

            // 🔍 DESYNC / DEBUG AVANZADO
            EventBus.OnSnapshotSent += (i1, i2) => Log(LogCategory.DESYNC, "SNAPSHOT_SENT", $"Snapshot enviado (frame {i1}, tick {i2})");
            EventBus.OnSnapshotReceived += () => Log(LogCategory.DESYNC, "SNAPSHOT_RECV", "Snapshot recibido");
            EventBus.OnHashDesync += (i1, i2) => Log(LogCategory.DESYNC, "HASH_DESYNC", $"Hash de estado: local {i1} vs remoto {i2} / DESYNC DETECTED");
            EventBus.OnDiffActor += (i, s1, s2, s3) => Log(LogCategory.DESYNC, "DIFF_ACTOR", $"Diff en actor {i}: propiedad {s1}, local={s2} vs remoto={s3}");
            EventBus.OnCorrectionActor += (i, s1, s2) => Log(LogCategory.DESYNC, "CORRECTION", $"Corrección aplicada: actor {i}, {s1} ajustado a {s2}");
            EventBus.OnForceResync += (s) => Log(LogCategory.DESYNC, "FORCE_RESYNC", $"Resincronización forzada de objeto {s}");

            // 🔁 FALLBACKS / RECONEXIÓN
            EventBus.OnReconnectStart += () => Log(LogCategory.FALLBACKS, "RECONNECT_START", "Iniciando reconexión…");
            EventBus.OnReconnectSuccess += (s) => Log(LogCategory.FALLBACKS, "RECONNECT_SUCCESS", $"Éxito en sala {s}");
            EventBus.OnReconnectDenied += () => Log(LogCategory.FALLBACKS, "RECONNECT_DENIED", "Denegado");
            EventBus.OnStateRecoverHost += (i) => Log(LogCategory.FALLBACKS, "STATE_RECOVER", $"Estado recuperado de host: actor {i}");
            EventBus.OnHostLost += (f) => Log(LogCategory.FALLBACKS, "HOST_LOST", $"Host perdido (timeout {f}s)");
            EventBus.OnNewHost += (i) => Log(LogCategory.FALLBACKS, "NEW_HOST", $"Nuevo host: actor {i}");
            EventBus.OnMatchAborted += (s) => Log(LogCategory.FALLBACKS, "MATCH_ABORTED", $"Partida abortada por: {s}");

            // 🧪 TEST / DEBUG TOOLS
            EventBus.OnTestSeed += (i) => Log(LogCategory.TEST, "TEST_SEED", $"Seed de partida: {i}");
            EventBus.OnTestBotCreate += (s, i) => Log(LogCategory.TEST, "BOT_CREATE", $"Bot creado: nombre {s}, dificultad {i}");
            EventBus.OnTestBotRemove += () => Log(LogCategory.TEST, "BOT_REMOVE", "Bot eliminado");
            EventBus.OnTestCmd += (s1, s2) => Log(LogCategory.TEST, "TEST_CMD", $"Comando debug: \"{s1}\" ejecutado, parámetro {s2}");
            EventBus.OnTestOverrideState += (s) => Log(LogCategory.TEST, "TEST_OVERRIDE", $"Override de estado: {s} forzado");
            EventBus.OnTestSimNet += (i1, i2) => Log(LogCategory.TEST, "TEST_SIM_NET", $"Simulación red: latencia {i1}ms, pérdida {i2}%");
            EventBus.OnTestPerf += (f1, f2, f3) => Log(LogCategory.TEST, "TEST_PERF", $"Log rendimiento: FPS={f1}, FrameTime={f2}ms, GC={f3}KB");
            EventBus.OnTestDumpRoom += (i, s) => Log(LogCategory.TEST, "TEST_DUMP_ROOM", $"Dump de sala: {i} jugadores, propiedades {s}");
        }
    }
}
