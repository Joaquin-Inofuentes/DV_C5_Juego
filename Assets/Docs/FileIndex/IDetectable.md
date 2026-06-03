# IDetectable

- Archivo: Scripts/MVC/Sensors/IDetectable.cs
- Lineas: 16
- Clase(s): DetectableType (enum), IDetectable (interface)
- Namespace: Game.Sensors

## Descripcion
Interfaz del sistema de sensores genérico. Define el contrato para cualquier entidad detectable por `GenericDetector`. El enum `DetectableType` clasifica los tipos: Aliado, Enemigo, Interactuable, Proyectil.

## Metodos de la Interfaz
- GetName() → string
- GetDetectableType() → DetectableType
- GetTransform() → Transform

## Implementada por
- DetectableEntity (componente genérico)
- UnitController (directamente)
