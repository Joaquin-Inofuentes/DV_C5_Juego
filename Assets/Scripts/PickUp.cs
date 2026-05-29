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
        if (collision.gameObject.name.Contains("Jugador"))
        {
            EfectuarRecogida(collision.gameObject);
        }
    }

    private void EfectuarRecogida(GameObject jugador)
    {
        // Obtener componentes del jugador
        InformacionPersonaje infoPersonaje = jugador.GetComponent<InformacionPersonaje>();
        PlayerController playerController = jugador.GetComponent<PlayerController>();

        if (infoPersonaje == null)
        {
            Debug.LogError("No se encontró InformacionPersonaje en el Jugador.");
            return;
        }

        switch (tipoItem)
        {
            case TipoRecogible.Vida:
                infoPersonaje.vidaActual = Mathf.Min(infoPersonaje.vidaActual + valorItem, infoPersonaje.vidaMaxima);
                infoPersonaje.ActualizarUI();
                Debug.Log($"Vida curada en {valorItem}. Vida actual: {infoPersonaje.vidaActual}");
                break;

            case TipoRecogible.Municion:
                WeaponController weaponController = jugador.GetComponent<WeaponController>();
                if (weaponController != null && weaponController.model != null)
                {
                    int armaEquipada = weaponController.model.NumeroDeArmaActual;
                    weaponController.model.ReservaActual += (int)valorItem;
                    infoPersonaje.ActualizarUI();
                    Debug.Log($"Munición añadida a reserva: {(int)valorItem}.");
                }
                break;

            case TipoRecogible.Velocidad:
                if (playerController != null && playerController.model != null)
                {
                    playerController.model.ActivarBoostVelocidadTemporal(multiplicadorVelocidad, duracionVelocidad);
                    Debug.Log($"Boost de velocidad de x{multiplicadorVelocidad} activado por {duracionVelocidad}s.");
                }
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
