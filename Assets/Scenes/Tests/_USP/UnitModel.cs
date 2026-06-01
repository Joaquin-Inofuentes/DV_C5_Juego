using Game.Core;
using UnityEngine;
using USP.Core;

public class UnitModel : MonoBehaviour, IHealth
{
    [Header("Identidad")]
    public string unitName = "Unit";
    public UnitTeam team;
    public bool isPlayerControlled = false; // Define si puede ser Líder del pelotón

    [Header("Salud")]
    public float healthMax = 100f;
    public float healthActual = 100f;

    [Header("Combate")]
    public float damage = 10f;
    public float fireRate = 0.5f;
    public int ammoActual = 300;
    public int ammoMax = 300;
    public float attackRange = 7f;
    public float detectionRange = 12f;

    [Header("Movimiento")]
    public float speedPatrol = 3.5f;
    public float speedChase = 5f;

    public bool IsDead => healthActual <= 0;
    public bool IsLeader { get; set; } = false;

    private void Start() { healthActual = healthMax; }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (IsDead) return;
        healthActual -= amount;
        if (healthActual <= 0) healthActual = 0;
    }

    public void AddHealth(float amount) => healthActual = Mathf.Min(healthActual + amount, healthMax);
    public bool CanFire() => ammoActual > 0;
    public void ConsumeAmmo() => ammoActual--;

    // Implementación IHealth
    public float CurrentHealth => healthActual;
    public float MaxHealth => healthMax;
}