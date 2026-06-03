# UnitCommander

- Archivo: Scenes/Tests/_USP/UnitCommander.cs
- Lineas: 113
- Clase(s): UnitCommander
- Namespace: global

## Descripcion
Maneja órdenes manuales del jugador a unidades individuales. Click derecho envía al aliado más cercano (no-líder) al punto. Teclas 1/2/3 envían la unidad específica al cursor (via `GEN_Inputs.OnOrdenDirecta`). Z hace regresar a formación a todas las unidades con orden. Las unidades enviadas entran en `IrADestinoState`.

## Metodos Publicos Clave
- Ninguno (toda la lógica es interna via eventos)

## Dependencias (using)
- Game.Core
- Game.Squad
- System.Linq
- UnityEngine
