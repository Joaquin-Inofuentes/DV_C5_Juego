using UnityEngine;

/// <summary>
/// Almacena configuraciones globales para el juego.
/// Este script es estático y accesible desde cualquier otro script.
/// </summary>
public static class ConfiguracionGlobal
{
    // Coeficientes de dificultad
    public static float coeficienteDificultad = 1.0f;
    public static float coeficienteDanioEnemigo = 1.0f;
    public static float coeficienteVidaJugador = 1.0f;

    // Puntaje para ganar
    public static int puntajeParaGanar = 100;
    public static float tiempoEsperaVictoria = 5.0f;

    // Configuración de vibración
    public static float duracionVibracionCamara = 0.2f;
    public static float intensidadVibracionDisparo = 0.1f;
    public static float intensidadVibracionGranada = 0.3f;

    public static long duracionVibracionMovilDisparo = 300; // ms
    public static long duracionVibracionMovilGranada = 500; // ms
}
