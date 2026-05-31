# Mapa de Arquitectura y Dependencias de Sistemas

Este documento describe la relacion y dependencias entre los diferentes dominios o sistemas del proyecto.

## Diagrama de Relaciones (Mermaid)

`mermaid
graph TD    MVC_Base[MVC_Base (7 scripts)]
    MVC_Squad[MVC_Squad (3 scripts)]
    MVC_Enemy[MVC_Enemy (1 scripts)]
    CambioDeSoldado[CambioDeSoldado (3 scripts)]
    SC_USP_Core[SC_USP_Core (6 scripts)]
    SC_USP_Entities[SC_USP_Entities (11 scripts)]
    SC_USP_IA[SC_USP_IA (16 scripts)]
    SC_USP_Weapons[SC_USP_Weapons (5 scripts)]
    SC_USP_Services[SC_USP_Services (8 scripts)]
    SC_USP_UI[SC_USP_UI (2 scripts)]
    Scenes_Base[Scenes_Base (29 scripts)]
    Root_Scripts[Root_Scripts (17 scripts)]
    CambioDeSoldado --> Scenes_Base
    MVC_Base --> SC_USP_Core
    MVC_Base --> SC_USP_Entities
    MVC_Base --> SC_USP_Weapons
    MVC_Enemy --> SC_USP_Entities
    MVC_Enemy --> Scenes_Base
    MVC_Squad --> Root_Scripts
    MVC_Squad --> SC_USP_Entities
    MVC_Squad --> Scenes_Base
    Root_Scripts --> MVC_Squad
    Root_Scripts --> SC_USP_Core
    Root_Scripts --> SC_USP_Entities
    Root_Scripts --> SC_USP_Services
    Root_Scripts --> SC_USP_Weapons
    Root_Scripts --> Scenes_Base
    SC_USP_Core --> Root_Scripts
    SC_USP_Core --> SC_USP_Entities
    SC_USP_Core --> SC_USP_Services
    SC_USP_Core --> SC_USP_Weapons
    SC_USP_Entities --> MVC_Base
    SC_USP_Entities --> MVC_Squad
    SC_USP_Entities --> Root_Scripts
    SC_USP_Entities --> SC_USP_Core
    SC_USP_Entities --> SC_USP_IA
    SC_USP_Entities --> SC_USP_Services
    SC_USP_Entities --> SC_USP_Weapons
    SC_USP_Entities --> Scenes_Base
    SC_USP_IA --> SC_USP_Entities
    SC_USP_IA --> SC_USP_Services
    SC_USP_Services --> MVC_Base
    SC_USP_Services --> Root_Scripts
    SC_USP_Services --> SC_USP_Entities
    SC_USP_Services --> SC_USP_IA
    SC_USP_UI --> Root_Scripts
    SC_USP_Weapons --> MVC_Base
    SC_USP_Weapons --> Root_Scripts
    SC_USP_Weapons --> SC_USP_Core
    SC_USP_Weapons --> SC_USP_Entities
    SC_USP_Weapons --> SC_USP_Services
    SC_USP_Weapons --> SC_USP_UI
    SC_USP_Weapons --> Scenes_Base
    Scenes_Base --> MVC_Squad
    Scenes_Base --> Root_Scripts
    Scenes_Base --> SC_USP_Core
    Scenes_Base --> SC_USP_Entities
    Scenes_Base --> SC_USP_IA
    Scenes_Base --> SC_USP_Services
    Scenes_Base --> SC_USP_Weapons
```

## Resumen de Sistemas

### MVC_Base
- **Ruta de busqueda**: ``Scripts/MVC/(?!Squad|Enemy)``
- **Cantidad de scripts**: 7
- **Scripts**:
  - [CharacterControllerMVC](file:///Assets/Docs/FileIndex/CharacterControllerMVC.md)
  - [ICharacterInput](file:///Assets/Docs/FileIndex/ICharacterInput.md)
  - [IMovementHandler](file:///Assets/Docs/FileIndex/IMovementHandler.md)
  - [IWeaponInput](file:///Assets/Docs/FileIndex/IWeaponInput.md)
  - [UnityCharacterInput](file:///Assets/Docs/FileIndex/UnityCharacterInput.md)
  - [UnityWeaponInput](file:///Assets/Docs/FileIndex/UnityWeaponInput.md)
  - [WeaponControllerMVC](file:///Assets/Docs/FileIndex/WeaponControllerMVC.md)

### MVC_Squad
- **Ruta de busqueda**: ``Scripts/MVC/Squad``
- **Cantidad de scripts**: 3
- **Scripts**:
  - [ISoldierState](file:///Assets/Docs/FileIndex/ISoldierState.md)
  - [SoldierStates](file:///Assets/Docs/FileIndex/SoldierStates.md)
  - [SquadEventBus](file:///Assets/Docs/FileIndex/SquadEventBus.md)

### MVC_Enemy
- **Ruta de busqueda**: ``Scripts/MVC/Enemy``
- **Cantidad de scripts**: 1
- **Scripts**:
  - [EnemySensors](file:///Assets/Docs/FileIndex/EnemySensors.md)

### CambioDeSoldado
- **Ruta de busqueda**: ``Scripts/CambioDeSoldado``
- **Cantidad de scripts**: 3
- **Scripts**:
  - [CambioDeLider](file:///Assets/Docs/FileIndex/CambioDeLider.md)
  - [UnitController](file:///Assets/Docs/FileIndex/UnitController.md)
  - [UnitFSM](file:///Assets/Docs/FileIndex/UnitFSM.md)

### SC_USP_Core
- **Ruta de busqueda**: ``Scripts/SC_USP/Core``
- **Cantidad de scripts**: 6
- **Scripts**:
  - [CharacterModel](file:///Assets/Docs/FileIndex/CharacterModel.md)
  - [EnemyModel](file:///Assets/Docs/FileIndex/EnemyModel.md)
  - [InformacionPersonaje](file:///Assets/Docs/FileIndex/InformacionPersonaje.md)
  - [Interfaces](file:///Assets/Docs/FileIndex/Interfaces.md)
  - [SoldierModel](file:///Assets/Docs/FileIndex/SoldierModel.md)
  - [WeaponModel](file:///Assets/Docs/FileIndex/WeaponModel.md)

### SC_USP_Entities
- **Ruta de busqueda**: ``Scripts/SC_USP/Entities``
- **Cantidad de scripts**: 11
- **Scripts**:
  - [CharacterView](file:///Assets/Docs/FileIndex/CharacterView.md)
  - [ControladorTanque](file:///Assets/Docs/FileIndex/ControladorTanque.md)
  - [Enemigo](file:///Assets/Docs/FileIndex/Enemigo.md)
  - [EnemyController](file:///Assets/Docs/FileIndex/EnemyController.md)
  - [EnemyView](file:///Assets/Docs/FileIndex/EnemyView.md)
  - [EntrarAlTanque](file:///Assets/Docs/FileIndex/EntrarAlTanque.md)
  - [PlayerController](file:///Assets/Docs/FileIndex/PlayerController.md)
  - [Puntero_Tanque](file:///Assets/Docs/FileIndex/Puntero_Tanque.md)
  - [SoldierController](file:///Assets/Docs/FileIndex/SoldierController.md)
  - [SoldierView](file:///Assets/Docs/FileIndex/SoldierView.md)
  - [Tanque](file:///Assets/Docs/FileIndex/Tanque.md)

### SC_USP_IA
- **Ruta de busqueda**: ``Scripts/SC_USP/IA``
- **Cantidad de scripts**: 16
- **Scripts**:
  - [IA_F_ChangeMode](file:///Assets/Docs/FileIndex/IA_F_ChangeMode.md)
  - [IA_F_ControllerSeguidor](file:///Assets/Docs/FileIndex/IA_F_ControllerSeguidor.md)
  - [IA_F_EnemyCercanos](file:///Assets/Docs/FileIndex/IA_F_EnemyCercanos.md)
  - [IA_F_PathFanding_Theta](file:///Assets/Docs/FileIndex/IA_F_PathFanding_Theta.md)
  - [IA_P2_AgentIA](file:///Assets/Docs/FileIndex/IA_P2_AgentIA.md)
  - [IA_P2_FOV](file:///Assets/Docs/FileIndex/IA_P2_FOV.md)
  - [IA_P2_FSM](file:///Assets/Docs/FileIndex/IA_P2_FSM.md)
  - [IA_P2_INT_gentState](file:///Assets/Docs/FileIndex/IA_P2_INT_gentState.md)
  - [IA_P2_LineOfSight3D](file:///Assets/Docs/FileIndex/IA_P2_LineOfSight3D.md)
  - [IA_P2_PathfindingManager](file:///Assets/Docs/FileIndex/IA_P2_PathfindingManager.md)
  - [IA_P2_PathfindingModel](file:///Assets/Docs/FileIndex/IA_P2_PathfindingModel.md)
  - [IA_P2_PathNode](file:///Assets/Docs/FileIndex/IA_P2_PathNode.md)
  - [IA_P2_ST_ChaseState](file:///Assets/Docs/FileIndex/IA_P2_ST_ChaseState.md)
  - [IA_P2_ST_PatrolState](file:///Assets/Docs/FileIndex/IA_P2_ST_PatrolState.md)
  - [IA_P2_ST_ReturningToPatrolState](file:///Assets/Docs/FileIndex/IA_P2_ST_ReturningToPatrolState.md)
  - [IA_P2_ST_SearchingState](file:///Assets/Docs/FileIndex/IA_P2_ST_SearchingState.md)

### SC_USP_Weapons
- **Ruta de busqueda**: ``Scripts/SC_USP/Weapons``
- **Cantidad de scripts**: 5
- **Scripts**:
  - [Cohete](file:///Assets/Docs/FileIndex/Cohete.md)
  - [Proyectil](file:///Assets/Docs/FileIndex/Proyectil.md)
  - [Proyectil2](file:///Assets/Docs/FileIndex/Proyectil2.md)
  - [WeaponController](file:///Assets/Docs/FileIndex/WeaponController.md)
  - [WeaponView](file:///Assets/Docs/FileIndex/WeaponView.md)

### SC_USP_Services
- **Ruta de busqueda**: ``Scripts/SC_USP/Services``
- **Cantidad de scripts**: 8
- **Scripts**:
  - [AutoDestruccionSegura](file:///Assets/Docs/FileIndex/AutoDestruccionSegura.md)
  - [CrearYDestruir](file:///Assets/Docs/FileIndex/CrearYDestruir.md)
  - [GameManager](file:///Assets/Docs/FileIndex/GameManager.md)
  - [IA_P2_BusEvent_Manager](file:///Assets/Docs/FileIndex/IA_P2_BusEvent_Manager.md)
  - [Manager_VFX](file:///Assets/Docs/FileIndex/Manager_VFX.md)
  - [PersecucionEnemigo](file:///Assets/Docs/FileIndex/PersecucionEnemigo.md)
  - [ProxiesUSP](file:///Assets/Docs/FileIndex/ProxiesUSP.md)
  - [Rigidbody2DMovementHandler](file:///Assets/Docs/FileIndex/Rigidbody2DMovementHandler.md)

### SC_USP_UI
- **Ruta de busqueda**: ``Scripts/SC_USP/UI``
- **Cantidad de scripts**: 2
- **Scripts**:
  - [CambiarOpacidad](file:///Assets/Docs/FileIndex/CambiarOpacidad.md)
  - [Soldado_Anim](file:///Assets/Docs/FileIndex/Soldado_Anim.md)

### Scenes_Base
- **Ruta de busqueda**: ``Scenes/Base``
- **Cantidad de scripts**: 29
- **Scripts**:
  - [AmmoManager](file:///Assets/Docs/FileIndex/AmmoManager.md)
  - [Bala](file:///Assets/Docs/FileIndex/Bala.md)
  - [BalaPool](file:///Assets/Docs/FileIndex/BalaPool.md)
  - [CollisionDetector](file:///Assets/Docs/FileIndex/CollisionDetector.md)
  - [ControlDerrota](file:///Assets/Docs/FileIndex/ControlDerrota.md)
  - [CursorManager](file:///Assets/Docs/FileIndex/CursorManager.md)
  - [DebugColisionesFull](file:///Assets/Docs/FileIndex/DebugColisionesFull.md)
  - [DesactivarPorTimer](file:///Assets/Docs/FileIndex/DesactivarPorTimer.md)
  - [Destruible](file:///Assets/Docs/FileIndex/Destruible.md)
  - [Disparador](file:///Assets/Docs/FileIndex/Disparador.md)
  - [EnemyDetector](file:///Assets/Docs/FileIndex/EnemyDetector.md)
  - [FormationRelocator](file:///Assets/Docs/FileIndex/FormationRelocator.md)
  - [FSMController](file:///Assets/Docs/FileIndex/FSMController.md)
  - [GlobalData](file:///Assets/Docs/FileIndex/GlobalData.md)
  - [GlobalHUD](file:///Assets/Docs/FileIndex/GlobalHUD.md)
  - [IInteractable](file:///Assets/Docs/FileIndex/IInteractable.md)
  - [InteractableItem](file:///Assets/Docs/FileIndex/InteractableItem.md)
  - [LeaderManager](file:///Assets/Docs/FileIndex/LeaderManager.md)
  - [MainGameController](file:///Assets/Docs/FileIndex/MainGameController.md)
  - [MarkerAnim](file:///Assets/Docs/FileIndex/MarkerAnim.md)
  - [MenuPausa](file:///Assets/Docs/FileIndex/MenuPausa.md)
  - [Municion](file:///Assets/Docs/FileIndex/Municion.md)
  - [PositionManager](file:///Assets/Docs/FileIndex/PositionManager.md)
  - [RehenBruto](file:///Assets/Docs/FileIndex/RehenBruto.md)
  - [ShotImpactBus](file:///Assets/Docs/FileIndex/ShotImpactBus.md)
  - [ShotSensor](file:///Assets/Docs/FileIndex/ShotSensor.md)
  - [TickManager](file:///Assets/Docs/FileIndex/TickManager.md)
  - [UnitCommander](file:///Assets/Docs/FileIndex/UnitCommander.md)
  - [UnitPathRenderer](file:///Assets/Docs/FileIndex/UnitPathRenderer.md)

### Root_Scripts
- **Ruta de busqueda**: ``Scripts/(?![MVC|SC_USP|CambioDeSoldado|Nuevos])``
- **Cantidad de scripts**: 17
- **Scripts**:
  - [BD_Audios](file:///Assets/Docs/FileIndex/BD_Audios.md)
  - [Camara](file:///Assets/Docs/FileIndex/Camara.md)
  - [CodigoDeInicio](file:///Assets/Docs/FileIndex/CodigoDeInicio.md)
  - [ConfiguracionGlobal](file:///Assets/Docs/FileIndex/ConfiguracionGlobal.md)
  - [GEN_Inputs](file:///Assets/Docs/FileIndex/GEN_Inputs.md)
  - [GestorTexto](file:///Assets/Docs/FileIndex/GestorTexto.md)
  - [Ideas y pseudocodigos](file:///Assets/Docs/FileIndex/Ideas y pseudocodigos.md)
  - [IndicadorEnemigos](file:///Assets/Docs/FileIndex/IndicadorEnemigos.md)
  - [MenuVictoria](file:///Assets/Docs/FileIndex/MenuVictoria.md)
  - [Obstaculo](file:///Assets/Docs/FileIndex/Obstaculo.md)
  - [PickUp](file:///Assets/Docs/FileIndex/PickUp.md)
  - [Prueba_de_color](file:///Assets/Docs/FileIndex/Prueba_de_color.md)
  - [SelectedSoldierUIFeedback](file:///Assets/Docs/FileIndex/SelectedSoldierUIFeedback.md)
  - [SenalisacionAEnemigos](file:///Assets/Docs/FileIndex/SenalisacionAEnemigos.md)
  - [SistemaPuntaje](file:///Assets/Docs/FileIndex/SistemaPuntaje.md)
  - [Torreta](file:///Assets/Docs/FileIndex/Torreta.md)
  - [VibracionCamara](file:///Assets/Docs/FileIndex/VibracionCamara.md)

