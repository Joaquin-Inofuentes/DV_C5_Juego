# Informe de Auditoría y Correcciones — DV_C5_Juego

- Fecha: 2026-05-31
- Alcance: escaneo completo de los 110 scripts C# propios + compilación batch (Unity 2022.3.5f1).
- Resultado de compilación tras correcciones: **0 errores, 0 warnings, return code 0**.

---

## 0. Hallazgo bloqueante (pre-existente)

**`IDaniable` no estaba definida en ningún archivo.** La interfaz era implementada por
`Destruible`, `SoldierController` y `EnemyController`, y usada por `Bala`, `Proyectil`,
`Cohete` y `CursorManager`, pero su definición se perdió durante la reorganización del
proyecto. Esto rompía la compilación (CS0246 / CS0400).

**Fix:** se recreó `Assets/Scripts/SC_USP/Core/IDaniable.cs` en el namespace global con la
firma esperada: `void RecibirDano(int cantidad, GameObject atacante)`.

---

## 1. Críticos corregidos

| Archivo | Problema | Corrección |
|---|---|---|
| Enemigo.cs | Merge conflict comentado + doble destrucción + `Find` sin null check | Resuelto a un único camino de muerte con guard `muriendo`; null-checks en Start/Update |
| EnemyController.cs | `AlertarRuidoDisparo()` lanzaba `NotImplementedException` | Implementado: investiga el ruido si no está en combate y hay LOS |
| GameManager.cs | Singleton invertido (`Destroy(instance)` mataba el original) | `Destroy(gameObject)` sobre el duplicado + `return` |
| IA_P2_FSM.cs | Suscripción a evento sin null-check; sin `OnDisable` (memory leak) | Null-check + `OnDisable` que desuscribe los 3 eventos; guard de `agent` en Update |

## 2. Altos corregidos

| Archivo | Problema | Corrección |
|---|---|---|
| BD_Audios.cs | Acumulación infinita de GameObjects `DontDestroyOnLoad` | Destruye los audios previos antes de recargar |
| Puntero_Tanque.cs | `GameObject.Find` cada frame en coroutine | Cacheado una sola vez + null-checks |
| Tanque.cs | `Find` sin null-check en Start | Null-check + guard en FixedUpdate |
| PersecucionEnemigo.cs | `Find` sin null-check | Null-check |
| IA_F_ControllerSeguidor.cs | Devolvía lista sin ordenar (`Enemigos[0]`) y remove por índice corruptible | Devuelve `enemigosVisibles[0]`; remove en reversa; guards de null |
| IndicadorEnemigos.cs | `Destroy`+`Instantiate` de UI cada frame | Pool reutilizable de indicadores |
| Bala.cs | `FindObjectOfType<CursorManager>()` por impacto | Cache estático |
| SoldierStates.cs (HuirDetrasLider) | `FindGameObjectsWithTag` cada frame | Throttle cada 0.25s con cache |
| CambiarOpacidad.cs | `.material.color` cada frame (instancia material) | Material cacheado; solo escribe al cambiar estado |
| Torreta.cs | `GameManager.player.transform` sin null-check | Null-checks en Start y Update |

## 3. Medios corregidos

| Archivo | Problema | Corrección |
|---|---|---|
| IA_P2_FOV.cs | `Reset()`/`OnValidate()` usaban `BoxCollider` (3D) con triggers 2D; `using UnityEditor` sin guard; Debug.Log por frame | `BoxCollider2D`; `using` bajo `#if UNITY_EDITOR`; log comentado |
| EnemyDetector.cs | `Physics.Raycast` + triggers 3D en juego 2D | `Physics2D.Raycast` + `OnTriggerEnter2D/Exit2D` |
| FormationRelocator.cs | `Physics.SphereCast` (3D) | `Physics2D.CircleCast` |
| ControladorTanque.cs | `Destroy` cada FixedUpdate; `GetMouseButtonDown` en FixedUpdate | Guard `muriendo`; click capturado en Update |
| LeaderManager.cs / UnitCommander.cs | Subscribe/unsubscribe de eventos cada frame | Flag de suscripción única |
| Camara.cs | `target` sin null-check en LateUpdate | Null-check |
| IA_P2_AgentIA.cs | Color sin null-check; `IsOnFinalPathSegment` mal; método muerto | Null-check; `>= Count-1`; método y comentario muertos eliminados |
| IA_P2_PathfindingManager.cs | Dicts estáticos compartidos en `RunAStar` | Diccionarios locales |
| IA_P2_PathNode.cs | `using UnityEditor` sin guard | Bajo `#if UNITY_EDITOR` |

## 4. Bajos corregidos

| Archivo | Problema | Corrección |
|---|---|---|
| Proyectil2.cs | `Time.deltaTime` en FixedUpdate | `Time.fixedDeltaTime` |
| DesactivarPorTimer.cs | Invoke no cancelado al desactivar | `CancelInvoke` en `OnDisable` |
| InformacionPersonaje.cs | `GetComponent<GameManager>()` redundante | Llamada directa + null-check |
| IA_P2_BusEvent_Manager.cs | `Update()` vacío | Eliminado |
| IA_F_ChangeMode.cs | `Start()` vacío + `agentIA` sin null-check | Eliminado + guard |
| WeaponModel.cs | Posible IndexOutOfRange si arrays difieren | Índice acotado por array (`Idx`) |
| SoldierModel.cs / EnemyModel.cs | `IsDead` irreversible | Método `Revivir()` añadido |
| Manager_VFX.cs | Variable `relativePath` sin usar (warning CS0219) | Eliminada |
| Soldado_Anim.cs | Campo `estadoAnteriorTeclasMovimiento` sin usar (warning CS0414) | Eliminado |

---

## 5. Deuda pendiente (no corregida a propósito)

- **Clases vacías** `Obstaculo.cs`, `Prueba_de_color.cs`, `SistemaPuntaje.cs`: son dead code,
  pero podrían estar referenciadas como componentes en prefabs/escenas. Borrarlas generaría
  "missing script". Se dejan hasta verificar referencias en el editor.
- **`IA_P2_FSM.IsPlayerVisible()`** devuelve siempre `false` (la lógica de FOV se movió a
  `IA_P2_FOV`). La detección por visión depende ahora del evento `OnTargetDetected`. Revisar
  si se desea reactivar detección directa.
- **`RunAStar`** en `IA_P2_PathfindingManager` no se invoca (el flujo usa Theta*). Candidato a
  eliminar si se confirma que no se usará.
