using USP.Entities;
using USP.Core;
using UnityEngine;
using Game.MVC;

/// <summary>
/// Script simple de Recogible (PickUp) para ítems de Munición, Vida y Boost de Velocidad.
/// </summary>
public class PickUp : MonoBehaviour
{
    public enum TipoRecogible { Municion, Vida, Velocidad }

    [Header("Configuración del Ítem")]
    [Tooltip("Tipo de recogible que representa este objeto.")]
    public TipoRecogible tipoItem;

    [Tooltip("Valor de curación directa (si es tipo Vida) o cantidad de munición a sumar (si es tipo Munición).")]
    public float valorItem = 30f;

    [Tooltip("Multiplicador de velocidad de movimiento temporal (si es tipo Velocidad).")]
    public float multiplicadorVelocidad = 1.8f;

    [Tooltip("Duración en segundos del boost de velocidad.")]
    public float duracionVelocidad = 3f;

    [Header("Efecto de Sonido")]
    [Tooltip("Nombre del clip de audio a reproducir al recoger el objeto.")]
    public string sonidoAlRecoger = "Caminar"; // Cambia al nombre de sonido adecuado en tu BD_Audios

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Detectar si el que colisiona es un soldado (tiene SoldierController)
        SoldierController soldado = collision.GetComponent<SoldierController>();
        if (soldado != null && soldado.model != null && !soldado.model.IsDead)
        {
            EfectuarRecogida(soldado);
        }
    }

    private void EfectuarRecogida(SoldierController soldado)
    {
        SoldierModel sModel = soldado.model;

        switch (tipoItem)
        {
            case TipoRecogible.Vida:
                sModel.Curar(valorItem);
                Debug.Log($"[PICKUP] Vida de {soldado.name} curada en {valorItem}. Vida actual: {sModel.vidaActual}/{sModel.vidaMaxima}");
                break;

            case TipoRecogible.Municion:
                sModel.AgregarMunicion((int)valorItem);
                Debug.Log($"[PICKUP] Munición agregada a {soldado.name}: {(int)valorItem}. Balas actuales: {sModel.balasActuales}/{sModel.maxBalas}");
                break;

            case TipoRecogible.Velocidad:
                sModel.ActivarBoostVelocidadTemporal(multiplicadorVelocidad, duracionVelocidad);
                Debug.Log($"[PICKUP] Boost de velocidad temporal activado para {soldado.name} (x{multiplicadorVelocidad}) por {duracionVelocidad}s.");
                break;
        }

        // Reproducir sonido de recogida
        if (!string.IsNullOrEmpty(sonidoAlRecoger))
        {
            BD_Audios.ReproducirConSolapamiento(sonidoAlRecoger);
        }

        // Destruir el objeto recogido de la escena
        Destroy(gameObject);
    }
}

