# LiderandoState (+ todos los estados Unit)

- Archivo: Scenes/Tests/_USP/LiderandoState.cs
- Lineas: 243
- Clase(s): LiderandoState, SeguirFormacionState, AtacarState, PerseguirState, EsperandoState, HuirDetrasLiderState, IrADestinoState
- Namespace: Game.Squad

## Descripcion
Contiene todas las implementaciones de `IUnitState` para la FSM de unidades. Reemplaza a `SoldierStates.cs`.

### Estados:
- **LiderandoState**: control manual del jugador. Lee input de GEN_Inputs (WASD+mouse), disparo sostenido, rotación gráfica directa.
- **SeguirFormacionState**: seguir al slot de formación asignado (`currentSlot`). Si no tiene slot → Esperando.
- **AtacarState**: detenerse y disparar al target. Rotación suave hacia enemigo. Línea roja. Si fuera de rango → Perseguir.
- **PerseguirState**: moverse hacia el target via agente. Si en rango → Atacar. Si target null → Formación.
- **EsperandoState**: detenerse. Puede ser temporizado (5s post-orden) o indefinido.
- **HuirDetrasLiderState**: calcular punto de cobertura detrás del líder respecto al enemigo. Si HP > 50% → Formación.
- **IrADestinoState**: ir a punto de orden manual. Línea verde al destino. Al llegar → Esperando 5s.

## Dependencias (using)
- UnityEngine
- Game.Core
- Game.Squad
