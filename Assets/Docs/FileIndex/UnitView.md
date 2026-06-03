# UnitView

- Archivo: Scenes/Tests/_USP/UnitView.cs
- Lineas: 179
- Clase(s): UnitView
- Namespace: global

## Descripcion
Vista unificada de unidades. Maneja rotación gráfica (graphicsRoot), selectionRing del líder, LineRenderer (líneas rojas al enemigo, verdes al destino, paths coloreados), sistema de indicadores con titileo (heal/combat/moving), flash de daño, y barra de vida dibujada con OnGUI.

## Metodos Publicos Clave
- RotateGraphics(float angle)
- RotateGraphicsSmooth(float angle, float speed)
- ShowLineToTarget(Vector3 from, Vector3 targetPos)
- ShowLineToDestination(Vector3 from, Vector3 destination)
- ShowLinePath(List<Vector3> path, Color color)
- HideLine()
- StartBlink(IndicatorType type)
- StopBlink(IndicatorType type)
- StopAllBlinks()
- TriggerFlash()
- SetSelectionRing(bool isActive)

## Dependencias (using)
- UnityEngine
- System.Collections
- System.Collections.Generic
- Game.Squad (IndicatorEntry, IndicatorType)
