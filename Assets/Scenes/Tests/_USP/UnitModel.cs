using Game.Core;
using UnityEngine;
using USP.Core;

public enum UnitSpecialization { Flancotirador, Apoyo, Medico, Asalto, EnemigoSimple }

public class UnitModel : MonoBehaviour, IHealth
{
    [Header("Identidad")]
    public string unitName = "Unit";
    public UnitTeam team;
    public bool isPlayerControlled = false;

    [Header("Especialidad")]
    public UnitSpecialization specialization = UnitSpecialization.Asalto;

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

    [Header("Estamina")]
    public float maxStamina = 3f;
    public float currentStamina = 3f;

    /// <summary>
    /// True cuando la salud llego a 0. El soldado esta caido pero puede ser revivido.
    /// </summary>
    public bool IsDown => healthActual <= 0;

    /// <summary>Alias de IsDown para compatibilidad con codigo existente.</summary>
    public bool IsDead => IsDown;

    public bool IsLeader { get; set; } = false;

    [SerializeField, HideInInspector]
    private UnitSpecialization lastSpecialization;
    [SerializeField, HideInInspector]
    private float lastDamage;
    [SerializeField, HideInInspector]
    private float lastFireRate;
    [SerializeField, HideInInspector]
    private float lastHealthMax;

    private void OnValidate()
    {
        // Auto-assign specialization or enforce team rules if desired
        if (team == UnitTeam.BandoB && specialization != UnitSpecialization.EnemigoSimple)
        {
            specialization = UnitSpecialization.EnemigoSimple;
        }
        else if (team == UnitTeam.BandoA && specialization == UnitSpecialization.EnemigoSimple)
        {
            // If they are on BandoA, they shouldn't be EnemigoSimple, default to Asalto
            specialization = UnitSpecialization.Asalto;
        }

        if (specialization != lastSpecialization)
        {
            UpdateStatsToDefaults();
            lastSpecialization = specialization;
            lastDamage = damage;
            lastFireRate = fireRate;
            lastHealthMax = healthMax;
        }
        else
        {
            // Keep manually changed values in inspector
            lastDamage = damage;
            lastFireRate = fireRate;
            lastHealthMax = healthMax;
        }

        if (!Application.isPlaying)
        {
            healthActual = healthMax;
            ammoActual = ammoMax;
        }
    }

    public void UpdateStatsToDefaults()
    {
        switch (specialization)
        {
            case UnitSpecialization.Flancotirador:
                damage = 50f;
                fireRate = 1.2f;
                healthMax = 100f;
                break;
            case UnitSpecialization.Apoyo:
                damage = 5f;
                fireRate = 0.08f;
                healthMax = 200f;
                break;
            case UnitSpecialization.Medico:
                damage = 5f;
                fireRate = 0.1f;
                healthMax = 100f;
                break;
            case UnitSpecialization.Asalto:
                damage = 5f;
                fireRate = 0.1f;
                healthMax = 100f;
                break;
            case UnitSpecialization.EnemigoSimple:
                damage = 5f;
                fireRate = 0.1f;
                healthMax = 100f;
                break;
        }
    }

    private void Awake()
    {
        // Reducir velocidad base a la mitad globalmente al iniciar
        speedChase *= 0.5f;
        speedPatrol *= 0.5f;
    }

    private void Start() 
    { 
        // Ya no asignamos salud en Start, se usa el valor serializado de OnValidate
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (IsDown) return;
        healthActual -= amount;
        if (healthActual < 0) healthActual = 0;
    }

    public void TakeDamage(int amount, GameObject attacker) => TakeDamage((float)amount, attacker);

    public void AddHealth(float amount) => healthActual = Mathf.Min(healthActual + amount, healthMax);
    public bool CanFire() => ammoActual > 0 && !IsDown;
    public void ConsumeAmmo()
    {
        ammoActual--;
        if (ammoActual <= 0)
        {
            if (this.gameObject.activeInHierarchy)
            {
                StartCoroutine(AutoReload());
            }
        }
    }

    private System.Collections.IEnumerator AutoReload()
    {
        Debug.Log($"[UnitModel] {name} sin munición, recargando...");
        yield return new WaitForSeconds(2.0f); // 2 segundos de recarga
        ammoActual = ammoMax;
        Debug.Log($"[UnitModel] {name} recarga completada. Munición: {ammoActual}/{ammoMax}");
    }

    /// <summary>Restaura HP al porcentaje de revive configurado.</summary>
    public void ReviveHealth()
    {
        healthActual = healthMax * reviveHealthPercent;
    }

    // Implementacion IHealth
    public float CurrentHealth => healthActual;
    public float MaxHealth => healthMax;
}
