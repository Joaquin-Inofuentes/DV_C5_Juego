# UnitModel

- Archivo: Scenes/Tests/_USP/UnitModel.cs
- Lineas: 38
- Clase(s): UnitModel
- Namespace: global (implementa Game.Core.IHealth)

## Descripcion
Modelo de datos unificado para cualquier unidad (soldado aliado o enemigo). Reemplaza a SoldierModel/EnemyModel como modelo principal del sistema Unit. Contiene stats de salud, combate (daño, fireRate, munición, rango) y movimiento (speedPatrol, speedChase). Define equipo via `UnitTeam` y si puede ser líder (`isPlayerControlled`).

## Metodos Publicos Clave
- TakeDamage(float amount, GameObject attacker)
- AddHealth(float amount)
- CanFire() → bool
- ConsumeAmmo()

## Propiedades
- IsDead → bool
- IsLeader { get; set; }
- CurrentHealth → float (IHealth)
- MaxHealth → float (IHealth)

## Dependencias (using)
- Game.Core (UnitTeam, IHealth)
- UnityEngine
- USP.Core
