# Inventario del Proyecto

- Ultima actualizacion: 2026-06-01
- Total de scripts C# (propios): 109
- Escenas: 15
- Prefabs (propios): 80+

## Cambios respecto a auditoría 2026-05-31
- **Refactorización Soldier→Unit**: SoldierController/Model/View/States/ISoldierState ELIMINADOS, reemplazados por UnitController/UnitModel/UnitView/LiderandoState/IUnitState
- **Sistema de sensores genérico**: Nuevo namespace `Game.Sensors` con IDetectable, DetectableEntity, GenericDetector
- **SquadEventBus**: reescrito como static class con nuevo evento OnHelpRequested (prioridad)
- **Scripts movidos**: ~30 scripts migrados de `Scripts/` y `Scenes/Base/` a `Scenes/Tests/_USP/`
- **Nuevas carpetas**: `Scripts/MVC/Sensors/`, `Editor/`, `Materials/`, `Shaders/`, `Textures/`, `UI/`, `Prefabs/Otros/`, `Prefabs/_USP/`
- **Scripts eliminados**: SoldierController.cs, SoldierModel.cs, SoldierView.cs, SoldierStates.cs, ISoldierState.cs, EnemySensors.cs, EnemyController.cs

## Estructura de Carpetas (scripts propios)

```
Assets/
├── Editor/                          (2 scripts: herramientas de editor)
├── Scenes/
│   ├── Base/                        (5 scripts: colisiones, timer, shotImpactBus)
│   │   └── Balas/                   (CollisionDetector, DebugColisionesFull)
│   └── Tests/_USP/                  (38 scripts: sistema Unit principal)
├── Scripts/
│   ├── CambioDeSoldado/             (2 scripts: CambioDeLider, UnitFSM)
│   ├── MVC/
│   │   ├── Sensors/                 (2 scripts: IDetectable, DetectableEntity) ← NUEVO
│   │   ├── Squad/                   (1 script: SquadEventBus)
│   │   └── (raíz MVC)              (6 scripts: CharacterControllerMVC, inputs, weapon)
│   ├── Nuevos/                      (1 script: SelectedSoldierUIFeedback)
│   ├── SC_USP/
│   │   ├── Core/                    (5 scripts: modelos, IDaniable, interfaces)
│   │   ├── Entities/                (7 scripts: Player, Enemigo, tanques)
│   │   ├── IA/                      (12 scripts: FSM enemiga, FOV, pathfinding)
│   │   ├── Services/                (6 scripts: GameManager, VFX, pathfinding mgr)
│   │   ├── UI/                      (2 scripts: opacidad, animación)
│   │   └── Weapons/                 (5 scripts: proyectiles, weapon controller)
│   └── (raíz Scripts)              (13 scripts: cámara, audio, config, menús, misc)
```

## Lista de Scripts (109 total)

### Editor (2)
1. CentralizadorScripts.cs
2. Physics2DMigrator.cs

### Scenes/Base (5)
3. CollisionDetector.cs
4. DebugColisionesFull.cs
5. DesactivarPorTimer.cs
6. IInteractable.cs
7. ShotImpactBus.cs

### Scenes/Tests/_USP (38) — SISTEMA UNIT PRINCIPAL
8. AmmoManager.cs
9. Bala.cs
10. BalaPool.cs
11. ControlDerrota.cs
12. CursorManager.cs
13. Destruible.cs
14. Disparador.cs
15. EnemyModel.cs
16. EnemyView.cs
17. FormationRelocator.cs
18. GameManager.cs
19. GEN_Inputs.cs
20. **GenericDetector.cs** ← NUEVO
21. GlobalData.cs
22. GlobalHUD.cs
23. IA_P2_AgentIA.cs
24. IA_P2_PathfindingModel.cs
25. IA_P2_PathNode.cs
26. InteractableItem.cs
27. LeaderManager.cs
28. **LiderandoState.cs** ← NUEVO (7 estados)
29. MainGameController.cs
30. Manager_VFX.cs
31. MarkerAnim.cs
32. MenuPausa.cs
33. Municion.cs
34. PositionManager.cs
35. RehenBruto.cs
36. ShotSensor.cs
37. TickManager.cs
38. UnitCommander.cs
39. **UnitController.cs** ← NUEVO (reemplaza SoldierController)
40. **UnitIndicatorSetup.cs** ← NUEVO
41. **UnitModel.cs** ← NUEVO (reemplaza SoldierModel)
42. UnitPathRenderer.cs
43. **UnitStates.cs** ← NUEVO (IUnitState)
44. **UnitTeam.cs** ← NUEVO
45. **UnitView.cs** ← NUEVO (reemplaza SoldierView)

### Scripts/CambioDeSoldado (2)
46. CambioDeLider.cs
47. UnitFSM.cs

### Scripts/MVC (9)
48. CharacterControllerMVC.cs
49. ICharacterInput.cs
50. IMovementHandler.cs
51. IWeaponInput.cs
52. UnityCharacterInput.cs
53. UnityWeaponInput.cs
54. WeaponControllerMVC.cs
55. **DetectableEntity.cs** ← NUEVO (MVC/Sensors)
56. **IDetectable.cs** ← NUEVO (MVC/Sensors)
57. SquadEventBus.cs (MVC/Squad)

### Scripts/Nuevos (1)
58. SelectedSoldierUIFeedback.cs

### Scripts/SC_USP/Core (5)
59. CharacterModel.cs
60. IDaniable.cs
61. InformacionPersonaje.cs
62. Interfaces.cs
63. WeaponModel.cs

### Scripts/SC_USP/Entities (7)
64. CharacterView.cs
65. ControladorTanque.cs
66. Enemigo.cs
67. EntrarAlTanque.cs
68. PlayerController.cs
69. Puntero_Tanque.cs
70. Tanque.cs

### Scripts/SC_USP/IA (12)
71. IA_F_ChangeMode.cs
72. IA_F_ControllerSeguidor.cs
73. IA_F_EnemyCercanos.cs
74. IA_F_PathFanding_Theta.cs
75. IA_P2_FOV.cs
76. IA_P2_FSM.cs
77. IA_P2_INT_gentState.cs
78. IA_P2_LineOfSight3D.cs
79. IA_P2_PathfindingManager.cs
80. IA_P2_ST_ChaseState.cs
81. IA_P2_ST_PatrolState.cs
82. IA_P2_ST_ReturningToPatrolState.cs
83. IA_P2_ST_SearchingState.cs

### Scripts/SC_USP/Services (6)
84. AutoDestruccionSegura.cs
85. CrearYDestruir.cs
86. IA_P2_BusEvent_Manager.cs
87. PersecucionEnemigo.cs
88. ProxiesUSP.cs
89. Rigidbody2DMovementHandler.cs

### Scripts/SC_USP/UI (2)
90. CambiarOpacidad.cs
91. Soldado_Anim.cs

### Scripts/SC_USP/Weapons (5)
92. Cohete.cs
93. Proyectil.cs
94. Proyectil2.cs
95. WeaponController.cs
96. WeaponView.cs

### Scripts (raíz) (13)
97. BD_Audios.cs
98. Camara.cs
99. CodigoDeInicio.cs
100. ConfiguracionGlobal.cs
101. GestorTexto.cs
102. Ideas y pseudocodigos.cs
103. IndicadorEnemigos.cs
104. MenuVictoria.cs
105. Obstaculo.cs
106. PickUp.cs
107. Prueba_de_color.cs
108. SenalisacionAEnemigos.cs
109. SistemaPuntaje.cs
110. Torreta.cs
111. VibracionCamara.cs

## Escenas (15)

| Escena | Ruta |
|---|---|
| MenuInicial | Scenes/Menus/MenuInicial.unity |
| MenuDeVictoria | Scenes/Menus/MenuDeVictoria.unity |
| MenuDeVictoria Grupos USP | Scenes/Menus/MenuDeVictoria Grupos USP.unity |
| _Juego | Scenes/Tests/_Juego.unity |
| _USP | Scenes/Tests/_USP.unity |
| _USP Separada | Scenes/Tests/_USP/_USP Separada.unity |
| _USP Mejora | Scenes/Tests/_USP Mejora.unity |
| EscenaPerdiste | Scenes/Tests/EscenaPerdiste.unity |
| IA | Scenes/Tests/IA.unity |
| IA_P2 | Scenes/Tests/IA_P2.unity |
| C5_DPR_P1 | Scenes/Tests/C5_DPR_P1.unity |
| DPR_P1 | Scenes/Tests/DPR_P1.unity |
| URP2DSceneTemplate | Scenes/Tests/URP2DSceneTemplate.unity |
| CI_DemoScene | Plugins/Custom Inspector/Demo/CI_DemoScene.unity |
| VFXPlayerScene | Plugins/VFXPACK.../VFXPlayerScene.unity |

## Prefabs propios (principales)

### Prefabs/Otros/ (27)
Bala, Boid, Botiquin, Cajon, Cohete, Enemy, Flecha, Food, GranadaImpactoNormal, Hojas, ImpactoNormal, ImpactoSangre, ItemRecogible, Main menu, map, Obs_Auto, Obs_Barril, Obs_Muro, Overload, PoolDeBalas, Proyectil_Bala, Proyectil_Granada, Proyectil_TanqueTripulado, Puntero_Tanque, Sangre, Soldado_Enemigo, Soldado_Jugador, S_Enemigo, S_Propio, Tanque Variant, TanqueAccesible, T_TileMap1, _Agente, _Nodos, _Obstaculos

### Prefabs/_USP/ (1)
_Muro

### Scenes/Base/ (5)
Bala, PoolDeBalas, Botiquin, Enemy, Enemy2

### Scenes/Prefabs/ (4)
Enemy2, Jugador, Proyectil, _Nodos 1, _Obstaculos 1

### Scenes/Tests/_USP/ (4)
_J1, __Cobertura, __Muro, Esquema

### Otros (4)
Textures/Nuevo: Muro, Obstaculo_1, Suelo; Scripts/Nuevos: VFX prefabs
