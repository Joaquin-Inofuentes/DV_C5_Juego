# UnitIndicatorSetup

- Archivo: Scenes/Tests/_USP/UnitIndicatorSetup.cs
- Lineas: 20
- Clase(s): IndicatorEntry, IndicatorType (enum)
- Namespace: global

## Descripcion
Define la estructura `IndicatorEntry` (nombre, GameObject indicador, tiempos on/off, duración) y el enum `IndicatorType` (Heal, Combat, Moving). Usado por `UnitView` para el sistema de indicadores con titileo (blink).

## IndicatorEntry campos
- name: string
- indicator: GameObject
- onTime/offTime: float (tiempos de titileo)
- duration: float (-1 = infinito)

## IndicatorType valores
- Heal
- Combat
- Moving
