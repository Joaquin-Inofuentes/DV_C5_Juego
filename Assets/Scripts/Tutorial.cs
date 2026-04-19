using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Importar el módulo para gestionar escenas

public class Tutorial : MonoBehaviour
{
    public Text mensajeTutorial; // Texto que muestra los mensajes del tutorial
    public GameObject objetoColisionado; // Objeto con el que colisionó el jugador
    public string nombreEscenaVictoria; // Nombre de la escena a la que se cambiará al colisionar con "Victoria"

    private bool wPresionado = false;
    private bool aPresionado = false;
    private bool dPresionado = false;

    private bool etapaMovimientoCompletada = false;
    private bool etapaRecogidaCompletada = false;
    private bool etapaLuchaCompletada = false;

    private float tiempoInicio;

    public GameObject TeclasDeTutorial;

    public bool Indicaciondada = false;

    void Start()
    {
        BD_Audios.ReproducirConSolapamiento("Bienvenido");
        tiempoInicio = Time.time;
        StartCoroutine(MostrarMensajesIniciales());
    }

    void Update()
    {
        if (Time.time > tiempoInicio + 3f && !etapaMovimientoCompletada) // Solo ejecuta si no está completada la etapa
        {
            if (Indicaciondada == false)
            {
                Indicaciondada = true;
                BD_Audios.ReproducirAudioUnaVez("usa WASD");
                ActualizarTutorialMovimiento();
            }
        }
    }

    private IEnumerator MostrarMensajesIniciales()
    {
        mensajeTutorial.text = "Bienvenido a WarZone";
        yield return new WaitForSeconds(2f);
        mensajeTutorial.text = "Comencemos...";
        yield return new WaitForSeconds(1f);
        mensajeTutorial.text = "Ahora ve adelante y continúa";
        // Inicia la etapa de lucha después de recoger el objeto
        StartCoroutine(IniciarEtapaLucha());
    }

    private void ActualizarTutorialMovimiento()
    {

        if (!wPresionado && Input.GetKeyDown(KeyCode.W))
        {
            wPresionado = true;
        }
        if (!aPresionado && Input.GetKeyDown(KeyCode.A))
        {
            aPresionado = true;
        }
        if (!dPresionado && Input.GetKeyDown(KeyCode.D))
        {
            dPresionado = true;
        }

        // Actualiza el texto del tutorial según las teclas presionadas
        mensajeTutorial.text = "Presiona: " +
            (wPresionado ? "" : "W ") +
            (aPresionado ? "" : "A ") +
            (dPresionado ? "" : "D ");

        // Si todas las teclas han sido presionadas
        if (wPresionado && aPresionado && dPresionado)
        {
            BD_Audios.DetenerAudio("usa WASD");
            BD_Audios.ReproducirAudioUnaVez("Voz_Recoje la municion");
            mensajeTutorial.text = "";
            etapaMovimientoCompletada = true; // Marca la etapa como completada
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {


        Debug.Log(other.name);
        // Cambia a la escena de Victoria al colisionar con el objeto "Victoria"
        if (other.name.Contains("Victoria"))
        {
            SceneManager.LoadScene(nombreEscenaVictoria); // Cambia a la escena con el nombre especificado
        }
    }

    private IEnumerator IniciarEtapaLucha()
    {
        // Pausa antes de mostrar los controles de granadas y curación
        yield return new WaitForSeconds(2f);
        TeclasDeTutorial.SetActive(true);
        mensajeTutorial.text = ""; // Elimina el texto

        // Pausa antes de eliminar el mensaje final
        yield return new WaitForSeconds(6f);
        TeclasDeTutorial.SetActive(false);

    }
}
