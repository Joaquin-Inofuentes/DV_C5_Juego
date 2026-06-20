# Guía de implementación — Redes (Photon Fusion 2, Host) — Top-Down Shooter

> **Para el agente que implementa la lógica.**
> El scaffolding ya existe en `Assets/_Redes` (estructura, relaciones, campos serializados y todos los `Debug.Log` con banderas ya colocados). Tu trabajo es **rellenar los cuerpos marcados con `// TODO (other agent)`** sin romper la estructura ni los namespaces (`Redes.*`).
> NO cambies nombres de campos serializados (los Editor Tools dependen de ellos). NO agregues asmdef. Mantené todo en `Assembly-CSharp`.

---

## 0. Contexto técnico (leer primero)

- **Unity 2022.3.5f1**, build target **WebGL**.
- **Photon Fusion 2.0.12**. API clave: `NetworkRunner`, `NetworkBehaviour`, `[Networked]`, `FixedUpdateNetwork()`, `Render()`, `Spawned()`, `[Rpc]`, `TickTimer`, `GetInput<T>()`, `Runner.Spawn/Despawn`.
- **Arquitectura Host**: el Host es también jugador y es la **State Authority**. Solo el Host hace `Spawn/Despawn` y solo él muta el estado `[Networked]` autoritativo.
- **Sin desfase**: todo lo simulable va en `FixedUpdateNetwork()` y el estado compartido en propiedades `[Networked]`. Lo visual/interpolado va en `Render()`.
- **Gotcha de nombres**: Fusion define su propio `NetworkPlayer`. En clases que usan el nuestro, hay que desambiguar con `using NetworkPlayer = Redes.Player.NetworkPlayer;` (ya está aplicado en `PlayerController` y en el Prefab Tool con `Player.NetworkPlayer`). Repetí ese alias si lo necesitás en otra clase.

### Antes de implementar lógica (setup en el editor, una vez)
1. `Tools > Redes > 1. Create Scene`
2. `Tools > Redes > 2. Create Prefabs`
3. `Tools > Redes > 3. Link & Assign All`
4. Abrir **Fusion > Network Project Config** y registrar los prefabs `Player` y `Bullet` en la lista de NetworkPrefabs (si no, `Runner.Spawn` falla).
5. Cargar el **App Id de Fusion** (Realtime) en el config de Photon, si no está.

---

## 1. Orden de implementación recomendado (fases)

1. **Input** (struct de entrada).
2. **Conexión Host + salas** (`HostNetworkService`, `RoomSessionHandler`).
3. **Spawn de jugadores** (`PlayerSpawner`, `NetworkPlayer.Spawned`).
4. **Movimiento** (`PlayerMovement`).
5. **Munición/Recarga** (`AmmoSystem`) + **Disparo** (`PlayerShooting`, `Projectile`).
6. **Vida y daño** (`PlayerHealth` + `IDamageable`).
7. **Victoria/Derrota en red** (`MatchNetworkController` + `MatchController` + `ResultView`).
8. **MVC/UI binding** (`GameFlowController`, `PlayerController`, Views, Models).
9. **Animaciones** (`PlayerAnimationController`).
10. **Pruebas** (checklist de logs al final).

---

## 2. Input struct (crear archivo nuevo)

Crear `Scripts/Network/NetworkInputData.cs` (namespace `Redes.Network`):

```csharp
using Fusion;
using UnityEngine;

namespace Redes.Network
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 Move;          // WASD / ejes
        public Vector2 AimDirection;  // hacia dónde mira/dispara
        public NetworkButtons Buttons; // fire, reload
    }

    public enum InputButton { Fire = 0, Reload = 1 }
}
```

- En `HostNetworkService.OnInput(runner, input)`: leer teclado/mouse del jugador local y hacer `input.Set(new NetworkInputData{...})`. Marcar botones con `data.Buttons.Set((int)InputButton.Fire, Input.GetButton("Fire1"))`, etc.
- En `Awake` de `HostNetworkService` poné `_runner.ProvideInput = true;` y `_runner.AddCallbacks(this);` (ya está como TODO).

---

## 3. Conexión Host + salas (`HostNetworkService` + `RoomSessionHandler`)

### `HostNetworkService.StartAsHost()`
- Mantener el log `"Inicio el juego"` (ya está).
- Implementar:
```csharp
_runner.AddCallbacks(this);
var sceneMgr = GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
await _runner.StartGame(new StartGameArgs {
    GameMode    = GameMode.Host,
    SessionName = GameConstants.DEFAULT_ROOM_NAME,
    PlayerCount = GameConstants.MAX_PLAYERS,
    SceneInfo   = /* la escena actual */,
    SceneManager= sceneMgr
});
IsRunning = true;
OnHostStarted?.Invoke();
```
> Para “buscar salas primero y luego crear/unirse”, alternativamente usá `JoinSessionLobby(SessionLobby.Shared/ClientServer)` para recibir `OnSessionListUpdated` ANTES de `StartGame`. Esa es la vía que dispara los logs de “se encontraron X salas”. Patrón sugerido: primero `JoinSessionLobby`, en `OnSessionListUpdated` decidir crear/unirse (ver `RoomSessionHandler`), y recién ahí `StartGame` con `GameMode.Host` (crear) o `GameMode.Client` (unirse).

- Convertir `StartAsHost` y los métodos que usan `await` a `async void`/`async Task`.

### `HostNetworkService.OnSessionListUpdated`
- Ya loguea `"Se encontraron X salas"` y delega en `_sessionHandler.HandleSessionList(...)`. No tocar la estructura.

### `RoomSessionHandler.HandleSessionList`
- **0 salas (crear)**: mantener logs `"Se creo una sala llamada X"` y `"Se esta esperando al otro jugador"`. Llamar a `_service.Runner.StartGame(... GameMode.Host, SessionName = DEFAULT_ROOM_NAME ...)`.
- **X salas (unirse)**: mantener log `"Se unio a la sala de {Name} nombre y {PlayerCount}/{MaxPlayers} datos"`. Llamar a `_service.Runner.StartGame(... GameMode.Client, SessionName = sessionList[0].Name ...)`.
- Cuidado con re-entrancia: arrancar `StartGame` una sola vez (guardá un bool `_starting`).

### `HostNetworkService.RefreshPlayerCount()`
- Implementar `ConnectedPlayers = _runner.SessionInfo.PlayerCount;` (o contar `_runner.ActivePlayers`).
- Mantener el `if (ConnectedPlayers >= MIN_PLAYERS_TO_START)` con el log `"se inicio el juego por q se tienen 2 jugadores"` y `OnEnoughPlayersToStart?.Invoke();`.
- **No iniciar gameplay** (habilitar input/spawnear armas, etc.) hasta que se cumpla esto. El spawn del cuerpo del jugador puede pasar en `OnPlayerJoined`, pero la lógica de “partida activa” debe gatear en `OnEnoughPlayersToStart`.

---

## 4. Spawn de jugadores (`PlayerSpawner` + `NetworkPlayer`)

### `HostNetworkService.OnPlayerJoined`
- Ya tiene `if (runner.IsServer) { TODO spawn }`. Implementar:
```csharp
_playerSpawner.SpawnPlayer(runner, player, _playerPrefab);
```

### `PlayerSpawner.SpawnPlayer(runner, player, prefab)`
- `Vector3 pos = GetSpawnPosition(player);`
- `var obj = runner.Spawn(prefab, pos, Quaternion.identity, player);` (el 4º arg = inputAuthority).
- `_spawned[player] = obj;`
- `GetSpawnPosition`: si hay `_spawnPoints`, usar `_spawnPoints[player.PlayerId % length]`; si no, offset por índice (ej. `new Vector3(player.PlayerId * 2f, 0, 0)`).

### `PlayerSpawner.DespawnPlayer`
- `if (_spawned.TryGetValue(player, out var o)) { runner.Despawn(o); _spawned.Remove(player); }` (solo server).

### `NetworkPlayer.Spawned()`
- Mantener log `"Inicio el jugador {Object.InputAuthority}"` (P0 = A, P1 = B).
- Si `Object.HasInputAuthority` (jugador local): buscar el `PlayerController` de escena y llamar `Bind(this)`, y enganchar la cámara para que siga a este transform.
```csharp
if (Object.HasInputAuthority) {
    var pc = Object.FindFirstObjectByType<Redes.Controllers.PlayerController>();
    pc?.Bind(this);
}
```

### `NetworkPlayer.FixedUpdateNetwork()`
- `if (GetInput(out NetworkInputData data)) { _movement.SetInput(data); if (presionó Fire) _shooting.Fire(); if (presionó Reload) _ammo.StartReload(); }`
- Para detectar “se presionó” usá `data.Buttons.GetPressed(previousButtons)`; guardá `previousButtons` en un `[Networked]` o variable local del tick.

---

## 5. Movimiento (`PlayerMovement`)

- Agregar un método `public void SetInput(NetworkInputData data)` que cachee la dirección, o leer el input directamente desde `NetworkPlayer`.
- En `FixedUpdateNetwork()`:
```csharp
Vector3 dir = new Vector3(_lastMove.x, 0, _lastMove.y);
transform.position += dir.normalized * _moveSpeed * Runner.DeltaTime;
```
  (Usá `Runner.DeltaTime`, NO `Time.deltaTime`.) Si usás Rigidbody, preferí `NetworkRigidbody3D` de Fusion en vez de `_body` directo; si te quedás con `_body`, movelo por `MovePosition`.
- Rotar el cuerpo hacia `AimDirection` para un top-down.

---

## 6. Munición/Recarga (`AmmoSystem`) y Disparo (`PlayerShooting` + `Projectile`)

### `AmmoSystem` (mecánica extra)
- `Spawned()`: `if (Object.HasStateAuthority) CurrentAmmo = _magazineSize;`
- `TryConsume()`: `if (IsReloading || CurrentAmmo <= 0) return false; CurrentAmmo--; return true;` (solo en State Authority).
- `StartReload()`: mantener log de recarga. `if (IsReloading || CurrentAmmo == _magazineSize) return; IsReloading = true; ReloadTimer = TickTimer.CreateFromSeconds(Runner, _reloadTime);`
- `FixedUpdateNetwork()`: `if (IsReloading && ReloadTimer.Expired(Runner)) { CurrentAmmo = _magazineSize; IsReloading = false; RedesLog.Info(RedesLog.AMMO, "Recarga completa"); }`

### `PlayerShooting.Fire()`
- Orden: `if (!_ammo.TryConsume()) { /* sin balas: opcional auto-reload */ return; }`
- Mantener el log `"El jugador {InputAuthority} disparo"`.
- Solo el server spawnea la bala:
```csharp
if (Object.HasStateAuthority) {
    var bullet = Runner.Spawn(_projectilePrefab, _muzzle.position, _muzzle.rotation, Object.InputAuthority);
    bullet.GetComponent<Projectile>().Owner = Object.InputAuthority;
}
```
> Para evitar doble disparo, llamá `Fire()` solo en el tick autoritativo (`Runner.IsForward` y dentro de `FixedUpdateNetwork`).

### `Projectile`
- `Spawned()`: `if (Object.HasStateAuthority) Life = TickTimer.CreateFromSeconds(Runner, _lifeTime);`
- `FixedUpdateNetwork()`:
```csharp
if (!Object.HasStateAuthority) return;
transform.position += transform.forward * _speed * Runner.DeltaTime;
if (Life.Expired(Runner)) { Runner.Despawn(Object); return; }
// detección de impacto:
if (Physics.SphereCast(... ) hit y hit tiene IDamageable d && esNoOwner) {
    d.TakeDamage(_damage, Owner);
    Runner.Despawn(Object);
}
```
- Usar `LagCompensation` (`Runner.LagCompensation.Raycast`) si querés precisión server-side; para algo simple, colisión por overlap es suficiente. La bala es `isTrigger` y `isKinematic`, así que detectá por `Runner.GetPhysicsScene().OverlapSphere` o un cast manual (NO uses `OnTriggerEnter` para la autoridad).

---

## 7. Vida y daño (`PlayerHealth` + `IDamageable`)

### `PlayerHealth`
- `Spawned()`: `if (Object.HasStateAuthority) CurrentHealth = _maxHealth;`
- Resolver `_matchNetwork` en runtime (es referencia escena, no se serializa en el prefab):
```csharp
if (_matchNetwork == null) _matchNetwork = Object.FindFirstObjectByType<MatchNetworkController>();
```
- `TakeDamage(amount, attacker)`:
  - Mantener log `"El jugador {InputAuthority} recibio el impacto"`.
  - `if (!Object.HasStateAuthority) return;` (solo el Host aplica daño).
  - `CurrentHealth = Mathf.Max(0, CurrentHealth - amount);`
  - Si `!IsAlive`: mantener log `"El jugador {InputAuthority} Perdio"` y llamar `_matchNetwork.AnnounceResult(loser: Object.InputAuthority, winner: attacker);`
- Para que la HUD se actualice en todos los clientes, agregá un `OnChanged` a `CurrentHealth`:
```csharp
[Networked, OnChangedRender(nameof(OnHealthChanged))] public int CurrentHealth { get; set; }
void OnHealthChanged() { /* avisar al PlayerController/HUD del dueño local */ }
```
  (En Fusion 2 el atributo es `OnChangedRender`; alternativamente `ChangeDetector` en `Render()`.)

---

## 8. Victoria/Derrota en red (`MatchNetworkController` → `MatchController` → `ResultView`)

Este es el punto crítico de “notificar a TODOS sin desfase”.

### `MatchNetworkController.AnnounceResult(loser, winner)`
- Mantener el log de anuncio. Implementar: `RpcAnnounceResult(loser, winner);` (lo llama el Host).

### `MatchNetworkController.RpcAnnounceResult` (ya tiene `[Rpc(StateAuthority, All)]`)
- En cada cliente:
```csharp
if (_matchController == null) _matchController = Object.FindFirstObjectByType<MatchController>();
var result = (Runner.LocalPlayer == loser) ? MatchResult.Lose : MatchResult.Win;
_matchController.NotifyResult(result);
```
> Como es un RPC a `All`, cada cliente resuelve su propio resultado con el MISMO dato → no hay desfase. El perdedor ve LOSE, el resto WIN.

### `MatchController.NotifyResult(result)`
- Ya llama `_resultView.ShowResult(result)`. Suscribirse en `OnEnable` a `_resultView.OnResultNotified` para, en `HandleResultNotified`, avisar al `GameFlowController`/`GameStateModel` que la fase pasó a `Finished` (y, si querés, frenar input).

### `ResultView.ShowResult(result)`
- **No tocar**: ya muestra el texto, ya emite los logs requeridos (`"...gano con action"` / `"...perdio con action"`) y dispara `OnResultNotified` (el “con action”).

---

## 9. MVC / UI binding

### `GameFlowController`
- `OnEnable()`: suscribir
  - `NetworkService.OnHostStarted += HandleHostStarted;`
  - `NetworkService.OnPlayerCountChanged += HandlePlayerCountChanged;`
  - `NetworkService.OnEnoughPlayersToStart += HandleEnoughPlayers;`
  - `_lobbyView.HostButton.onClick.AddListener(StartHost);`
- `OnDisable()`: desuscribir todo (importante para no duplicar).
- `_model.OnPhaseChanged`: en cada fase, mostrar/ocultar views:
  - `WaitingForPlayers` → `_lobbyView.ShowStatus("Esperando jugadores...")`, `_lobbyView.SetVisible(true)`, HUD off.
  - `Playing` → `_lobbyView.SetVisible(false)`, `_gameHudView.SetVisible(true)`.
- `HandlePlayerCountChanged(count)` → `_lobbyView.ShowPlayerCount(count)`.

### `PlayerController` (escena)
- `Bind(player)`: crear/conservar `PlayerModel`, y suscribir:
  - `_model.OnHealthChanged += h => _hudView.ShowHealth(h);`
  - `_model.OnAmmoChanged  += a => _hudView.ShowAmmo(a, GameConstants.DEFAULT_MAGAZINE_SIZE);`
- Conectar el `PlayerHealth.OnHealthChanged` y `AmmoSystem` del jugador local con `_model.SetHealth/SetAmmo` (puente red → modelo → view). Mantené el alias `using NetworkPlayer = Redes.Player.NetworkPlayer;`.

### Models (`GameStateModel`, `PlayerModel`)
- Son datos puros; sus `Set*` ya disparan eventos. No requieren lógica extra salvo validaciones opcionales.

---

## 10. Animaciones (`PlayerAnimationController`)

- Crear un `AnimatorController` con parámetros `Speed (float)`, `Shoot (Trigger)`, `Dead (Bool)` y asignarlo al `Animator` del prefab Player.
- En `Render()` leer el estado networked y setear:
```csharp
_animator.SetFloat(PARAM_SPEED, velocidadActual);
if (murió) _animator.SetBool(PARAM_DEAD, true);
```
- Para el disparo, disparar el trigger desde un `ChangeDetector`/`OnChanged` de un contador de disparos networked, así se ve en todos los clientes.

---

## 11. Reglas de oro (no romper)

- Mutar `[Networked]` SOLO en la State Authority (`if (Object.HasStateAuthority)`).
- `Spawn`/`Despawn` SOLO en el server/host.
- Movimiento y simulación en `FixedUpdateNetwork()` con `Runner.DeltaTime`. Nada de `Update()` para gameplay.
- Visual/animación/cámara en `Render()`.
- No cambiar nombres de campos `[SerializeField]` (los Tools 2 y 3 los buscan por nombre).
- Mantener todos los `RedesLog.Info(...)` existentes: son la evidencia que se evalúa.

---

## 12. Checklist de prueba (correr con 2 clientes: ParrelSync o Build + Editor)

Verificá que la consola muestre, en orden, estos logs (bandera entre corchetes):

**Cliente Host (primer jugador):**
- `[REDES][NET] Inicio el juego`
- `[REDES][LOBBY] Se encontraron 0 salas`
- `[REDES][LOBBY] Se creo una sala llamada RedesRoom`
- `[REDES][LOBBY] Se esta esperando al otro jugador`

**Segundo cliente:**
- `[REDES][NET] Inicio el juego`
- `[REDES][LOBBY] Se encontraron 1 salas`
- `[REDES][LOBBY] Se unio a la sala de RedesRoom nombre y 1/4 datos`
- `[REDES][NET] se inicio el juego por q se tienen 2 jugadores`

**En partida:**
- `[REDES][PLAYER] Inicio el jugador 0` y `[REDES][PLAYER] Inicio el jugador 1`
- `[REDES][COMBAT] El jugador 1 disparo`
- `[REDES][COMBAT] El jugador 0 recibio el impacto`
- `[REDES][MATCH] El jugador 0 Perdio`
- En el cliente ganador: `[REDES][MATCH] El jugador A recibio la notitifacion de q gano con action`
- En el cliente perdedor: `[REDES][MATCH] El jugador B recibio la notificacion de q perdio con action`

Si todos esos logs aparecen y la UI (Text legacy) muestra Vida/Munición y el cartel WIN/LOSE, la lógica está completa.

---

## 13. Entrega (recordatorio de la consigna)

- Generar una **Build funcional** (WebGL) y entregarla junto al proyecto (si falta, −6 puntos).
- Confirmar: 2+ jugadores, Network Runner, Network Object, control de animaciones, arquitectura Host, no arranca con <2 jugadores, condición de victoria/derrota notificada a todos, sin desfase, y la mecánica extra (Munición/Recarga) distinta de Movimiento/Disparo/Salto.
