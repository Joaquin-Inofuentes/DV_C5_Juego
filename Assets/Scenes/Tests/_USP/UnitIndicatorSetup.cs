using UnityEngine;
using System;

[Serializable]
public class IndicatorEntry
{
    public string name;
    public GameObject indicator;
    [Tooltip("Tiempo encendido en segundos")]
    public float onTime = 0.15f;
    [Tooltip("Tiempo apagado en segundos")]
    public float offTime = 0.15f;
    [Tooltip("Duración total del titileo (-1 = infinito hasta que se detenga)")]
    public float duration = -1f;
}

public enum IndicatorType
{
    Heal,
    Combat,
    Moving
}
