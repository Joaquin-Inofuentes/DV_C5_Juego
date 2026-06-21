# 🧪 Sistema de Eventos de Logueo Estructurado & Escena de Test Automatizada

Hemos implementado un sistema de eventos de logueo estructurado profesional basado en un `EventBus` y un `DebugLogger` tipados, cubriendo todas las primitivas y casos de juego (movimiento, disparo, recarga, daño, muerte, red/Photon y transiciones de estado) de acuerdo con los principios SOLID, MVC y patrones de Pool de objetos.

## 📁 Archivos creados
1. [EventBus.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/EventBus.cs) - El bus de comunicación global y estático desacoplado.
2. [DebugLogger.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/DebugLogger.cs) - Suscriptor de eventos y formateador de consola/test.
3. [PlayerModel.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/PlayerModel.cs) - Modelo de vida, daño y lógica pura.
4. [PlayerView.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/PlayerView.cs) - Vista que reacciona a eventos y dispara animaciones y efectos (SFX/VFX).
5. [PlayerController.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/PlayerController.cs) - Entrada de usuario, disparo de armas y movimiento/rotación hacia el cursor.
6. [BulletPool.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/BulletPool.cs) - Pool de objetos reutilizable para las balas.
7. [Bullet.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/Bullet.cs) - Comportamiento de movimiento y colisión de balas.
8. [Weapon.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/Weapon.cs) - Lógica de munición, disparo y recarga de armas.
9. [GameManager.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/GameManager.cs) - Simulador de ciclos de vida de partida y red.
10. [SimpleEnemyAI.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/SimpleEnemyAI.cs) - IA simple que persigue y dispara al jugador.
11. [GameLoopReferee.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Scripts/DebugSystem/GameLoopReferee.cs) - Árbitro que comprueba las condiciones de victoria/derrota.
12. [CreateTestSceneMenu.cs](file:///c:/Users/PC_JOACO/Documents/DV_C5_Juego/Assets/Editor/CreateTestSceneMenu.cs) - Menú del Editor Unity para crear la escena con un clic.

---

## 🛠️ Cómo Probar la Escena
1. Abre tu proyecto de Unity.
2. Haz clic en el menú superior: **Tools** > **Pruebas** > **CrearEscenaDeTests**.
3. Se generará automáticamente la escena `Scene_DebugTests` con toda la jerarquía de GameObjects, scripts y referencias vinculadas.
4. Presiona **Play** (Reproducir) en Unity.
5. Observa la consola de Unity; verás todos los logs secuenciales detallados, formateados y tipados, con variantes del juego (incluyendo la simulación inicial de red y matchmaking).
6. Usa las teclas `WASD` / `Flechas` para mover al jugador, el cursor para apuntar, **Clic Izquierdo** para disparar y la tecla **R** para recargar.
