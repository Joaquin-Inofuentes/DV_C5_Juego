# UnitController

- Archivo: Scenes/Tests/_USP/UnitController.cs
- Lineas: 210
- Clase(s): UnitController
- Namespace: Game.Squad

## Descripcion
Controller MVC unificado para todas las unidades (aliados y enemigos). Implementa `IDaniable` e `IDetectable`. Maneja FSM interna (`IUnitState`), detección via `GenericDetector`, combate (disparo con cooldown), recepción de daño, sistema de ayuda con prioridad (líder=1, aliado=2) via `SquadEventBus.OnHelpRequested`, y muerte.

## Metodos Publicos Clave
- CambiarEstado(IUnitState nuevoEstado)
- GetCurrentState() → IUnitState
- FollowLeader()
- GetEnemy() → Transform
- Attack(Transform objetivo)
- ReachedDestination() → bool
- ReleaseSlot()
- MoveToPoint(Vector3 point)
- GetTargetPoint() → Vector3
- RecibirDano(int cantidad, GameObject atacante) — IDaniable
- OnHealPickup()
- ResetHelpPriority()

## Eventos (suscritos)
- GenericDetector.OnTargetDetected
- SquadEventBus.OnHelpRequested

## Dependencias (using)
- UnityEngine
- System.Collections
- Game.Sensors
- Game.Core

## Componentes requeridos
- UnitModel
- UnitView
- IA_P2_AgentIA
- Disparador (hijo)
- GenericDetector (hijo, opcional)
