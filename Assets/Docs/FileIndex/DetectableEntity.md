# DetectableEntity

- Archivo: Scripts/MVC/Sensors/DetectableEntity.cs
- Lineas: 17
- Clase(s): DetectableEntity
- Namespace: Game.Sensors

## Descripcion
Componente MonoBehaviour que implementa `IDetectable`. Permite hacer cualquier GameObject detectable por `GenericDetector` sin implementar la interfaz directamente. Se configura nombre y tipo via Inspector o `Initialize()`.

## Metodos Publicos Clave
- Initialize(string name, DetectableType type)
- GetName() → string
- GetDetectableType() → DetectableType
- GetTransform() → Transform

## Dependencias (using)
- UnityEngine
- Game.Sensors
