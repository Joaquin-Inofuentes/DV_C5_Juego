# SISTEMA DE CAÍDO Y REVIVIMIENTO - DOCUMENTACIÓN COMPLETA

## 📋 Resumen Ejecutivo

Implementación completa del sistema de **"modo caído"** similar a Raven Squad donde:
- Cuando la vida de un soldado llega a 0, entra en estado **CAÍDO** (no muere destructivamente)
- El soldado caído está **INDETECTABLE** por enemigos
- Solo el **líder** puede revivir si mantiene **Barra Espaciadora por 3 segundos**
- **Aliados IA** pueden ir a revivir si están cerca y no están en combate
- **Líneas de debug** entre caído y aliados (azul normal, amarillo si puede revivir)
- **Barra visual** de revivimiento con Lerp (sube lento, baja rápido)

---

## 🎯 Flujo del Usuario

### 1. **Soldado es derrotado**
```
Soldado recibe daño y HP → 0
[RecibirDano] Llamando a model.TakeDamage()
[UnitController] Soldado caído activando view de caído
[EnterDamagedState] Soldado {NAME} entrando en estado CAIDO
[DamagedState] Vista actualizada para modo caído
[DamagedState] Soldado {NAME} completamente en estado CAIDO - INDETECTABLE
```

### 2. **Líder se acerca y comienza a revivir**
```
[HandleRevivalInput] Líder comenzando a revivir a {NAME}
[HandleRevivalInput] Reviviendo a {NAME}. Progreso: 0.50/3.00s
[HandleRevivalInput] Reviviendo a {NAME}. Progreso: 1.50/3.00s
[HandleRevivalInput] Reviviendo a {NAME}. Progreso: 2.50/3.00s
[HandleRevivalInput] Reviviendo a {NAME}. Progreso: 3.00/3.00s
[CompleteLeaderRevival] Líder {NAME} ha revivido a {NAME}
[CompleteLeaderRevival] Reviviendo al soldado caído. Espere 3 segundos - COMPLETO
[CompleteLeaderRevival] Soldado {NAME} revivido por {LIDER_NAME}
```

### 3. **Líder suelta la tecla o se aleja**
```
[HandleRevivalInput] Líder soltó barra espaciadora. Revivimiento CANCELADO
```

### 4. **Aliado IA detecta caído y va a revivir**
```
[CanReviveAlly] {NAME} PUEDE revivir a {CAIDO_NAME} - distancia: 2.50m
[StartRevivingAlly] {NAME} comenzando a revivir a {CAIDO_NAME}
[RevivingState] Reviviendo a {CAIDO_NAME}. Progreso: 1.25/3.00s
[CompleteRevival] ¡Revivimiento COMPLETADO! {NAME} ha revivido a {CAIDO_NAME}
```

---

## 📁 Archivos Creados/Modificados

### **NUEVOS**

#### 1. `DamagedState.cs`
- **Clase Principal**: `DamagedState : IUnitState`
- **Responsabilidades**:
  - Desactivar GameObjects "Vivo" y activar "Caido"
  - Marcar como indetectable para enemigos (tag: "Undetectable")
  - Iniciar visualización de líneas de debug
  - Bloquear movimiento y disparo completamente

- **Clase Auxiliar**: `DamagedStateHandler : MonoBehaviour`
  - Dibuja `Debug.DrawLine` a todos los aliados vivos
  - Color azul: aliado normal
  - Color amarillo: aliado lo suficientemente cerca para revivir (3m)

#### 2. `RevivalBarView.cs`
- **Propósito**: Visualización de barra de revivimiento
- **Características**:
  - Barra se carga en **amarillo** (Lerp hacia 1)
  - Si se suelta o se aleja: se reduce en **rojo** al **doble de velocidad**
  - Busca canvas hijo llamado "RevivalBarCanvas"
  - Métodos públicos:
    - `StartRevival()`: Comienza a cargar
    - `ContinueRevival()`: Mantiene cargando
    - `PauseRevival()`: Comienza a bajar rápido
    - `CompleteRevival()`: Revivimiento completado

#### 3. `RevivingState.cs`
- **Propósito**: Estado cuando un aliado está reviviendo a otro
- **Lógica**:
  - Detecta si el objetivo sigue cercano
  - Si se aleja: cancela revivimiento
  - Si mantiene Spacebar: carga el timer (solo si es el líder)
  - A los 3 segundos: reviva al soldado y restaura su HP
  - Logs claros en cada punto crítico

#### 4. `UnitController_Revival.cs`
- **Extensión parcial** de UnitController
- **Métodos principales**:
  - `EnterDamagedState()`: Entrada a estado caído
  - `ExitDamagedState()`: Salida del estado caído
  - `UpdateLeaderRevivalInput()`: Revisa input de Spacebar del líder
  - `HandleRevivalInput(UnitController)`: Procesa el revivimiento
  - `FindClosestDamagedAlly()`: Detecta aliados caídos
  - `CanReviveAlly(UnitController)`: Valida si puede revivir
  - `StartRevivingAlly(UnitController)`: Inicia revivimiento de IA

### **MODIFICADOS**

#### 1. `UnitController.cs`
- **Cambio en `RecibirDano()`**:
  ```csharp
  if (model.IsDead)
  {
      if (!isDown)
      {
          EnterDamagedState();
      }
  }
  ```
  En lugar de `Morir()`, ahora entra en estado caído.

- **Cambio en `Update()`**:
  ```csharp
  if (model.IsLeader && _currentStateLogic is LiderandoState)
  {
      UpdateLeaderRevivalInput();
  }
  ```
  Revisa input de revivimiento solo si es líder y está en control.

- **Agregado**: Método `LogMethodEntry()` para logs consistentes.

#### 2. `GEN_Inputs.cs`
- **Agregada propiedad**:
  ```csharp
  public bool RavivicionInput { get; private set; } // Barra espaciadora
  ```

- **Agregada línea en `Update()`**:
  ```csharp
  RavivicionInput = Input.GetKey(KeyCode.Space);
  ```

---

## 🎮 Flujo de Estados FSM

### Transiciones Nuevas

```
SeguirFormacionState
    ↓ (detecta caído cercano, no en combate)
RevivingState (si es aliado IA)
    ↓ (3 segundos)
SeguirFormacionState

LiderandoState
    → (input de revivimiento)
DamagedState (para el soldado caído)
    ↓ (3 segundos con Spacebar)
SeguirFormacionState
```

### Estados Relacionados

- **DamagedState**: El soldado derrotado
- **RevivingState**: Aliado reviviendo (espera 3 segundos)
- **LiderandoState**: Ya tiene lógica de revivimiento integrada

---

## 🔧 Configuración Necesaria en Escena

### Jerarquía de GameObjects

```
Soldado (UnitController)
├── Vivo (GameObject - activo normalmente)
│   ├── SpriteRenderer
│   ├── Collider
│   └── ... otros componentes visuales
├── Caido (GameObject - desactivado, se activa cuando HP → 0)
│   ├── SpriteRenderer (versión caída)
│   ├── Collider
│   └── ... visualización de caído
├── RevivalBarCanvas (UI Canvas)
│   └── RevivalBar (Image - para llenar con Lerp)
├── UnitModel
├── UnitView
├── IA_P2_AgentIA
├── Disparador (hijo)
└── GenericDetector (hijo)
```

### Tags Requeridos

- `"Undetectable"`: Para marcar soldados caídos (ignorados por detectores)
- `"Detectable"`: Para soldados normales

### Layers Requeridos

Mantener los que ya existen, agregar si falta:
- `"Player"`: Soldados aliados
- `"Enemy"`: Enemigos

---

## 📊 Diagrama de Logs por Evento

```
EVENTO: Soldado recibe daño letal
├─ [RecibirDano] {NAME} recibió {CANTIDAD} de {ATACANTE}. HP: {ACTUAL}/{MAX}
├─ [RecibirDano] {NAME} fue derrotado. Entrando en estado caído
├─ [EnterDamagedState] {NAME} entrando en estado CAIDO
├─ [SwapGameObjects] Desactivado GameObject 'Vivo'
├─ [SwapGameObjects] Activado GameObject 'Caido'
├─ [SetUndetectable] Soldado {NAME} es indetectable: True
└─ [DrawLinesToAllies] Dibujando líneas de debug a aliados

EVENTO: Líder mantiene Spacebar sobre caído
├─ [HandleRevivalInput] Líder comenzando a revivir a {CAIDO}
├─ [HandleRevivalInput] Reviviendo a {CAIDO}. Progreso: X.XX/3.00s
└─ (repite cada frame)

EVENTO: Revivimiento completado
├─ [CompleteLeaderRevival] Líder {LIDER} ha revivido a {CAIDO}
├─ [CompleteLeaderRevival] Reviviendo al soldado caído. Espere 3 segundos - COMPLETO
├─ [CompleteLeaderRevival] Salud de {CAIDO} restaurada: X/Y
├─ [CompleteLeaderRevival] Soldado {CAIDO} revivido por {LIDER}
├─ [Exit] Soldado {CAIDO} saliendo de estado CAIDO
├─ [RestoreGameObjects] Desactivado GameObject 'Caido'
├─ [RestoreGameObjects] Activado GameObject 'Vivo'
└─ [SetUndetectable] Soldado {CAIDO} es indetectable: False
```

---

## ✅ Testing Checklist

### 1. **Entrada a Caído**
- [ ] Soldado recibe daño total
- [ ] Entra en estado DamagedState
- [ ] GameObject "Vivo" se desactiva
- [ ] GameObject "Caido" se activa
- [ ] Log: "[DamagedState] Soldado completamente en estado CAIDO"
- [ ] Enemies NO pueden atacar al caído (indetectable)
- [ ] Caído NO puede moverse
- [ ] Caído NO puede disparar

### 2. **Detección Visual**
- [ ] Líneas azules a aliados lejanos
- [ ] Líneas amarillas a aliados dentro de 3m
- [ ] Las líneas se dibujan cada frame

### 3. **Líder Revive**
- [ ] Líder se acerca al caído
- [ ] Presiona y mantiene Spacebar
- [ ] Barra de revivimiento aparece (amarilla)
- [ ] Barra sube progresivamente
- [ ] A los 3 segundos: revivimiento completo
- [ ] Salud restaurada al 30% (configurable)
- [ ] Logs claros: "[CompleteLeaderRevival] Soldado revivido por..."

### 4. **Cancelación de Revivimiento**
- [ ] Si suelta Spacebar: barra baja rápido (rojo)
- [ ] Si se aleja: revivimiento se cancela
- [ ] Log: "[HandleRevivalInput] Revivimiento CANCELADO"

### 5. **Aliados IA Reviven**
- [ ] Aliado detecta caído cercano
- [ ] Si NO está en combate: va a revivir
- [ ] Llega al caído
- [ ] Entra en RevivingState
- [ ] Después de 3 segundos: reviva (solo IA, sin input)
- [ ] Log: "[CompleteRevival] Aliado ha revivido a caído"

### 6. **Múltiples Caídos**
- [ ] Varios soldados caídos simultáneamente
- [ ] Líneas de debug se dibujan correctamente
- [ ] Cada uno puede ser revivido por separado

### 7. **Integración con Batalla**
- [ ] Los caídos NO interfieren con combate de otros
- [ ] Enemigos ignoran a caídos
- [ ] Los aliados vivos pueden seguir luchando
- [ ] Revivimiento funciona durante combate

---

## 🐛 Troubleshooting

### **No aparecen líneas de debug**
- Verificar que `DamagedStateHandler` esté agregado al soldado
- Revisar que los aliados tengan `UnitController` válido
- Ver que Game View esté mostrando Gizmos

### **Barra de revivimiento no aparece**
- Verificar que exista `RevivalBarCanvas` como hijo
- Verificar que `RevivalBarView` esté agregado al UnitController
- Revisar que la imagen tenga nombre correcto ("RevivalBar" o "FillImage")

### **Revivimiento no funciona**
- Verificar que el líder está en `LiderandoState`
- Revisar que `GEN_Inputs.RavivicionInput` retorna true con Spacebar
- Ver que el caído esté dentro del rango (3 metros)

### **Caído sigue siendo detectable**
- Verificar que el tag es "Undetectable"
- Revisar que `GenericDetector` ignora este tag
- Comprobar que `SetUndetectable()` se ejecuta

---

## 📝 Notas de Implementación

### Decisiones de Diseño

1. **No destruir al morir**: En lugar de `Destroy(gameObject)`, el soldado entra en estado caído reutilizable
2. **Solo el líder revive**: Para evitar complejidad, solo el jugador puede iniciar revivimiento en esta versión
3. **Aliados IA reviven automáticamente**: Si detectan un caído cercano y no están ocupados, van a revivir
4. **Indetectable por script**: Se usa tag/layer para marcar el caído como invisible a enemigos
5. **Una barra compartida**: Se reutiliza la barra de vida con color diferente

### Optimizaciones

- `FindObjectsOfType` se ejecuta cada frame en `DrawLinesToAllies` (considerar cachear)
- Los logs son densos para debugging (considerar compilarlos con `#if DEBUG`)
- Las líneas de debug son O(n) donde n = número de aliados

### Mejoras Futuras

1. Agregar sonidos de revivimiento
2. Animaciones de caído/revivimiento
3. Sistema de "down but not out" con sangre
4. Múltiples aliados reviviendo al mismo tiempo
5. Bonificación de daño/velocidad al ser revivido recientemente
6. Timers de revivimiento más cortos/largos según dificultad

---

## 🎬 Ejemplo de Uso Completo

```csharp
// En el inspector:
// 1. Crear hijo "Vivo" con sprite de soldado vivo
// 2. Crear hijo "Caido" con sprite de soldado caído (desactivado)
// 3. Crear hijo "RevivalBarCanvas" con Image para la barra
// 4. Asignar UnitModel con healthMax = 100, reviveHealthPercent = 0.30
// 5. Asignar UnitView

// En juego:
// 1. Soldado recibe 100 de daño → HP = 0
// 2. [UnitController] entra en DamagedState
// 3. Líder se acerca a 2 metros
// 4. Línea amarilla aparece
// 5. Líder presiona Spacebar
// 6. Barra amarilla aparece y sube
// 7. A los 3 segundos: completo
// 8. Soldado revive con 30 HP
```

---

## ✨ Conclusión

Sistema completo, modular y extensible de revivimiento. Todos los logs son claros y estructurados con formato `[NombreMetodo] Mensaje`. El flujo es intuitivo y se integra perfectamente con el FSM existente.

**Estado**: ✅ IMPLEMENTADO Y DOCUMENTADO
