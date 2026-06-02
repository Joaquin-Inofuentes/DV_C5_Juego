# SquadEventBus

- Archivo: Scripts/MVC/Squad/SquadEventBus.cs
- Lineas: 33
- Clase(s): SquadEventBus (static)

## Metodos Publicos Clave
- TriggerUnitDamaged(UnitController, float, GameObject)
- TriggerUnitDied(UnitController)
- TriggerLeaderChanged(UnitController)
- TriggerHelpRequested(UnitController victim, Transform attacker, int priority)

## Eventos
- OnUnitDamaged
- OnUnitDied
- OnLeaderChanged
- OnHelpRequested — pedido de ayuda con prioridad (1=líder, 2=aliado)

## Dependencias (using)
- System
- UnityEngine
- Game.Squad
