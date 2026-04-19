using UnityEngine;

/// <summary>
/// Gestiona la recolección de ítems por parte del jugador.
/// Este script interactúa con el sistema de Información de Personaje ya existente.
/// </summary>
public class RecogerItem : MonoBehaviour
{
    public CambioDeArma cambioDeArma;

    // Duración en segundos antes de recoger el ítem
    public float tiempoDeRecogida = 3f; // Tiempo total para recoger el ítem
    private float temporizador; // Temporizador para la recogida

    // Referencia al script de Información de Personaje (debe estar en el jugador)
    public InformacionPersonaje infoPersonaje; // Visible para asignar en el Inspector

    // Referencia al Gestor de Texto
    public GestorTexto gestorTexto; // Asegúrate de arrastrar el objeto GestorTexto en el Inspector

    // Tipos de ítems que se pueden recoger
    public enum TipoItem { KitMedico, Recarga, Granada, Adrenalina }
    public TipoItem tipoItemActual;

    // Indica si el jugador está en la zona del ítem
    public bool jugadorEnRango = false;

    // Estado de recogida
    public bool recogiendo = false;

    private void Start()
    {
        if (gestorTexto == null)
        {
            gestorTexto = GameObject.Find("Texto_Conseguiste").GetComponent<GestorTexto>();
        }

        // Si no se asignó la referencia de Información de Personaje en el Inspector, buscarla
        if (infoPersonaje == null)
        {
            infoPersonaje = GameObject.Find("Soldado_Jugador").GetComponent<InformacionPersonaje>();
        }

        if (infoPersonaje == null)
        {
            Debug.LogError("No se encontró el script Información de Personaje en el jugador.");
        }

        // Reiniciar temporizador
        temporizador = 0;
    }

    // Cuando el jugador entra en la zona del ítem
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("Jugador"))
        {
            Debug.Log("Jugador en la zona del ítem.");
            jugadorEnRango = true;
            temporizador = tiempoDeRecogida; // Reiniciar el temporizador
            recogiendo = true; // Iniciar el proceso de recogida

            // Cambiar el color del ítem a rojo
            gameObject.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    // Cuando el jugador sale de la zona del ítem
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("Jugador"))
        {
            //Debug.Log("Jugador salió de la zona del ítem.");
            jugadorEnRango = false;

            // Detener la recogida
            recogiendo = false;
            temporizador = 0; // Detener el temporizador

            // Restaurar el color del ítem a blanco
            gameObject.GetComponent<Renderer>().material.color = Color.white;

            // Restaurar escala del ítem
            gameObject.transform.localScale = new Vector3(2f, 2f, 2f);
        }
    }

    private void Update()
    {
        if (gestorTexto == null)
        {
            gestorTexto = GameObject.Find("Texto_Conseguiste").GetComponent<GestorTexto>();
        }
        // Verificar si el jugador está en rango y el temporizador es mayor que cero
        if (jugadorEnRango && recogiendo)
        {
            // Reducir el temporizador según el tiempo transcurrido
            temporizador -= Time.deltaTime;

            // Actualizar la escala del ítem según el tiempo restante
            float escalaProporcional = Mathf.Lerp(1.5f, 1f, (tiempoDeRecogida - temporizador) / tiempoDeRecogida);
            gameObject.transform.localScale = new Vector3(escalaProporcional, escalaProporcional, escalaProporcional);

            // Si el temporizador llega a cero, recoger el ítem
            if (temporizador <= 0)
            {
                RecogerItemYAsignar();
            }
        }
    }

    // Recoger el ítem y asignarlo al jugador
    private void RecogerItemYAsignar()
    {
        recogiendo = false; // Cambiar el estado de recogida

        switch (tipoItemActual)
        {
            case TipoItem.KitMedico:
                Debug.Log("Recogido: Kit Médico");
                infoPersonaje.AniadirKitMedico(); // Aumentar el contador de kits médicos
                gestorTexto.MostrarTexto("¡Kit Médico Recogido!"); // Mostrar texto
                break;

            case TipoItem.Recarga:
                Debug.Log("Recogido: Recarga");
                int numeroDeRecarga = Random.Range(0, cambioDeArma.tiposDeArmas.Length);
                infoPersonaje.AnadirRecargas(numeroDeRecarga); // Aumentar el contador de recargas
                gestorTexto.MostrarTexto("¡Recarga de " + cambioDeArma.tiposDeArmas[numeroDeRecarga] + " Recogida!"); // Mostrar texto
                break;

            case TipoItem.Granada:
                Debug.Log("Recogido: Granada");
                infoPersonaje.AniadirGranadas(); // Aumentar el contador de granadas
                gestorTexto.MostrarTexto("¡Granada Recogida!"); // Mostrar texto
                break;

            case TipoItem.Adrenalina:
                Debug.Log("Recogido: Adrenalina");
                infoPersonaje.AnadirAdrenalina(); // Aumentar el contador de granadas
                gestorTexto.MostrarTexto("¡Adrenalina Recogida!"); // Mostrar texto
                break;
        }

        // Destruir el ítem del escenario
        Destroy(gameObject);
    }
}