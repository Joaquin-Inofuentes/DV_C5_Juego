# LeaderManager

- Archivo: Scenes/Tests/_USP/LeaderManager.cs
- Lineas: 94
- Clase(s): LeaderManager (Singleton)
- Namespace: global

## Descripcion
Gestiona el ciclo de liderazgo del escuadrón. Singleton via `Instance`. Mantiene lista de `UnitController` y permite ciclar líder con Q/E (via `GEN_Inputs.OnCycleLeader`). El líder anterior pasa a `SeguirFormacionState`, el nuevo entra en `LiderandoState`. Salta unidades muertas con wrap-around.

## Metodos Publicos Clave
- CambiarLider(int index)

## Campos
- unidades: List<UnitController> (configurado en Inspector)
- indiceInicial: int

## Dependencias (using)
- UnityEngine
- System.Collections.Generic
- Game.Squad
- Game.Core
