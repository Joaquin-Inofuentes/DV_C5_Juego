# GEN_Inputs

- Archivo: Scenes/Tests/_USP/GEN_Inputs.cs
- Lineas: 100
- Clase(s): GEN_Inputs (Singleton)
- Namespace: global

## Descripcion
Administrador central de entradas físicas (teclado/mouse). Singleton via `Instance`. Lee WASD, mouse position (world), click izq (disparo), click der (órdenes), Q/E (ciclar líder), 1/2/3 (órdenes directas a unidad), Z (regresar a formación).

## Propiedades Públicas
- MovimientoInput → Vector2
- MouseWorldPosition → Vector3
- DisparoSostenido → bool (GetMouseButton 0)
- DisparoPresionado → bool (GetMouseButtonDown 0)
- OrdenPresionada → bool (GetMouseButtonDown 1)
- RegresarAFormacion → bool (Z)

## Eventos
- OnCycleLeader(bool derecha) — Q=true, E=false
- OnOrdenDirecta(int index) — 0/1/2 para teclas 1/2/3

## Dependencias (using)
- UnityEngine
- System
