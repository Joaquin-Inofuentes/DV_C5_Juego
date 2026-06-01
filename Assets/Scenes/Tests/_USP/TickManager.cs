using UnityEngine;
using System;

/// <summary>
/// Singleton de ticks temporales. Reparte tres frecuencias de actualización para evitar
/// correr lógica costosa cada frame. Asignarse en OnEnable del GameObject que lo contenga.
/// 
/// USO:
///   OnEnable  → TickManager.Instance.OnTick_0_5s += MiMetodo;
///   OnDisable → TickManager.Instance.OnTick_0_5s -= MiMetodo;
/// </summary>
public class TickManager : MonoBehaviour
{
    public static TickManager Instance;

    /// Tick rápido — cada 0.1 segundos (lógica de ataque/rotación)
    public event Action OnTick_0_1s;
    /// Tick medio — cada 0.5 segundos (pathfinding, persecución)
    public event Action OnTick_0_5s;
    /// Tick lento — cada 1 segundo (patrulla, detección lejana)
    public event Action OnTick_1s;

    private float t01, t05, t1;

    private void OnEnable()
    {
        Instance = this;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        t01 += dt;
        t05 += dt;
        t1  += dt;

        if (t01 >= 0.1f) { t01 -= 0.1f; OnTick_0_1s?.Invoke(); }
        if (t05 >= 0.5f) { t05 -= 0.5f; OnTick_0_5s?.Invoke(); }
        if (t1  >= 1.0f) { t1  -= 1.0f; OnTick_1s?.Invoke();   }
    }
}
