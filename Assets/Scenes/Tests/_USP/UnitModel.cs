using Game.Core;
using UnityEngine;
using USP.Core;

public enum UnitSpecialization { Flancotirador, Apoyo, Medico }

public class UnitModel : MonoBehaviour, IHealth
{
    [Header("Identidad")]
    public string unitName = "Unit";
    public UnitTeam team;
    public bool isPlayerControlled = false;

    [Header("Especialidad")]
    public UnitSpecialization specialization = UnitSpecialization.Apoyo;

    [Header("Salud")]
    public float healthMax = 100f;
    public float healthActual = 100f;

    [Header("Estado Caido")]
    [Tooltip("Porcentaje de HP con el que vuelve al ser revivido (0.0-1.0)")]
    public float reviveHealthPercent = 0.30f;

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

    /// <summary>
    /// True cuando la salud llego a 0. El soldado esta caido pero puede ser revivido.
    /// </summary>
    public bool IsDown => healthActual <= 0;

    /// <summary>Alias de IsDown para compatibilidad con codigo existente.</summary>
    public bool IsDead => IsDown;

    public bool IsLeader { get; set; } = false;

    private void Awake()
    {
        if (specialization == UnitSpecialization.Apoyo)
            healthMax *= 2f;
    }

    private void Start() { healthActual = healthMax; }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (IsDown) return;
        healthActual -= amount;
        if (healthActual < 0) healthActual = 0;
    }

    public void TakeDamage(int amount, GameObject attacker) => TakeDamage((float)amount, attacker);

    public void AddHealth(float amount) => healthActual = Mathf.Min(healthActual + amount, healthMax);
    public bool CanFire() => ammoActual > 0 && !IsDown;
    public void ConsumeAmmo() => ammoActual--;

    /// <summary>Restaura HP al porcentaje de revive configurado.</summary>
    public void ReviveHealth()
    {
        healthActual = healthMax * reviveHealthPercent;
    }

    // Implementacion IHealth
    public float CurrentHealth => healthActual;
    public float MaxHealth => healthMax;
}
