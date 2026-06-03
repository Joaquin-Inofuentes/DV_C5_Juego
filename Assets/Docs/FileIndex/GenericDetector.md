# GenericDetector

- Archivo: Scenes/Tests/_USP/GenericDetector.cs
- Lineas: 119
- Clase(s): GenericDetector
- Namespace: Game.Sensors

## Descripcion
Detector genérico basado en CircleCollider2D trigger + raycast 2D. Detecta entidades que implementen `IDetectable` según tipos configurados (`DetectableType`). Escanea línea de visión cada 0.15s con throttle. Dispara eventos al detectar/perder visión de objetivos.

## Metodos Publicos Clave
- GetVisibleTargets() → List<IDetectable>
- GetTargetsInRange() → List<IDetectable>

## Eventos
- OnTargetDetected(IDetectable)
- OnTargetLost(IDetectable)

## Dependencias (using)
- System
- System.Collections.Generic
- UnityEngine
- Game.Sensors (IDetectable, DetectableType)

## Requiere
- CircleCollider2D (trigger) en el mismo GameObject
- Entidades detectadas deben implementar IDetectable
