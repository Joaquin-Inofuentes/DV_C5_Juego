# UnitStates (IUnitState)

- Archivo: Scenes/Tests/_USP/UnitStates.cs
- Lineas: 10
- Clase(s): IUnitState (interface)
- Namespace: Game.Squad

## Descripcion
Interfaz base para la FSM del sistema Unit. Reemplaza a `ISoldierState`. Define los 4 métodos del ciclo de estado: Enter, Update, FixedUpdate, Exit. Cada uno recibe el `UnitController` como parámetro.

## Metodos de la Interfaz
- Enter(UnitController unit)
- Update(UnitController unit)
- FixedUpdate(UnitController unit)
- Exit(UnitController unit)

## Implementada por
Los estados en LiderandoState.cs: LiderandoState, SeguirFormacionState, AtacarState, PerseguirState, EsperandoState, HuirDetrasLiderState, IrADestinoState
