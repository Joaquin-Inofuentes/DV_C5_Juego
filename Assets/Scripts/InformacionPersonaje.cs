using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la información del personaje, como salud, munición y recargas.
/// Debe estar asociado al objeto principal del jugador.
/// </summary>
public class InformacionPersonaje : MonoBehaviour
{
    public CambioDeArma cambioDeArma;

    // Salud y armadura
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;
    public float armadura = 50f;

    // Inventario
    public int balasPorCargador = 30;
    public int balasRestantes;
    public int kitsMedicos = 2;
    public int granadas = 5;
    public int adrenalina = 2;

    // Referencias a los textos del UI
    public Text textoVida;
    public Text textoArmadura;
    public Text textoMunicion;
    public Text textoRecargas;
    public Text textoKitsMedicos;
    public Text textoGranadas;
    public Text textoAdrenalina;
    public Text textoContadorMuerte; // Texto para mostrar el contador de muerte
    public Transform barraVida;

    // Variables para el contador de reinicio
    public float tiempoRestanteParaReiniciar = 3f;
    public bool juegoEnPausa = false;

    public GameObject EfectoViñetaNegra;

    

    private void Start()
    {
        InicializarValores();
        ActualizarUI();
    }

    private void InicializarValores()
    {
        // Inicializar valores de vida y munición
        vidaActual = vidaMaxima;
        balasRestantes = balasPorCargador;
    }

    public void RecibirDanio(float danio)
    {
        vidaActual -= danio;

        // Asegura que la vida no sea menor a 0
        if (vidaActual < 0) vidaActual = 0;

        ActualizarUI();

        if (vidaActual <= 0) Morir();
    }

    private void Morir()
    {
        // Desactivar la lógica del juego al morir
        juegoEnPausa = true;

        // Mostrar el texto del contador
        textoContadorMuerte?.gameObject.SetActive(true);

        // Iniciar el contador para reiniciar la escena
        StartCoroutine(ContadorReinicio());
    }

    private IEnumerator ContadorReinicio()
    {
        while (tiempoRestanteParaReiniciar > 0)
        {
            // Actualizar el texto del contador
            textoContadorMuerte.text = $"Reiniciando en: {Mathf.Ceil(tiempoRestanteParaReiniciar)}";
            yield return new WaitForSeconds(1f);
            tiempoRestanteParaReiniciar--;
        }
        // Reiniciar la escena
        GameManager.instance.GetComponent<GameManager>().ReiniciarEscena();
    }

    public void ReAparecer()
    {
        // Falta implementar la mecánica de reaparición
        Debug.Log("Re Apareci");
    }

    public void UsarKitMedico()
    {
        if (kitsMedicos > 0)
        {
            kitsMedicos--;
            vidaActual = Mathf.Min(vidaActual + 30f, vidaMaxima);
            ActualizarUI();
        }
    }

    public void ActualizarUI()
    {
        // Verificar que el GameObject está asignado
        if (EfectoViñetaNegra != null)
        {
            // Obtener el componente Image del GameObject
            Image componenteImagen = EfectoViñetaNegra.GetComponent<Image>();

            // Asegurarse de que el GameObject tiene un componente Image
            if (componenteImagen != null)
            {
                // Calcular el porcentaje de vida restante
                float porcentajeVida = vidaActual / vidaMaxima;

                // Ajustar el alpha de la viñeta en base al porcentaje de vida (invertido)
                Color colorViñeta = componenteImagen.color;
                colorViñeta.a = Mathf.Lerp(1f, 0.1f, porcentajeVida); // Transparencia: 100% cuando casi muere, 10% con vida máxima
                componenteImagen.color = colorViñeta;
            }
            else
            {
                Debug.LogWarning("El GameObject 'EfectoViñetaNegra' no tiene un componente Image.");
            }
        }
        else
        {
            Debug.LogWarning("No se ha asignado el GameObject 'EfectoViñetaNegra' en el Inspector.");
        }



        int NumeroDeArmaActual = cambioDeArma.NumeroDeArmaActual;
        if (textoArmadura != null) textoArmadura.text = $"Armadura: {armadura}";
        if (textoMunicion != null) textoMunicion.text = $"{cambioDeArma.IndicadorDeBalas}";
        if (textoRecargas != null) textoRecargas.text = $"{cambioDeArma.recargas[NumeroDeArmaActual]}";
        if (textoKitsMedicos != null) textoKitsMedicos.text = $"{kitsMedicos}";
        if (textoGranadas != null) textoGranadas.text = $"{granadas}";
        if (textoAdrenalina != null) textoAdrenalina.text = $"{adrenalina}";

        // Llama al método para actualizar la barra de vida
        ActualizarBarraVida(barraVida);
    }

    public void CurarAlMaximo()
    {
        // Cura al personaje hasta su vida máxima, si tiene kits médicos disponibles
        if (kitsMedicos > 0)
        {
            // Reduce el número de kits médicos en uno
            kitsMedicos--;

            // Restablece la vida actual al valor máximo
            vidaActual = vidaMaxima;

            // Actualiza la interfaz de usuario
            ActualizarUI();
        }
    }

    public void AniadirKitMedico()
    {
        kitsMedicos++;
        ActualizarUI();
    }

    public void AnadirRecargas(int NumeroDeArma)
    {
        cambioDeArma.recargas[NumeroDeArma]++;
        ActualizarUI();
    }

    public void AniadirGranadas()
    {
        granadas++;
        ActualizarUI();
    }

    public void AnadirAdrenalina()
    {
        adrenalina++;
        ActualizarUI();
        Debug.Log(1);
        StartCoroutine(CambiarValorTemporalmente());
    }

    public void ActualizarBarraVida(Transform barraVida)
    {
        // Calcula el porcentaje de vida actual
        float porcentajeVida = vidaActual / vidaMaxima;

        // Asegúrate de que la barra de vida tenga un rango entre 0 y 1
        porcentajeVida = Mathf.Clamp01(porcentajeVida);

        // Ajusta la escala de la barra de vida en el eje X
        barraVida.localScale = new Vector3(porcentajeVida, barraVida.localScale.y, barraVida.localScale.z);
    }


    // Variable a la que se le cambiará el valor
    public float miVariable = 10f;

    // Variable para almacenar el valor original
    private float valorOriginal;

    // Corutina que cambia el valor temporalmente
    IEnumerator CambiarValorTemporalmente()
    {
        valorOriginal = gameObject.GetComponent<ControladorPersonaje>().velocidadMovimiento;
        // Doblamos el valor de la variable
        gameObject.GetComponent<ControladorPersonaje>().velocidadMovimiento = 10;
        Debug.Log("Algo");

        // Esperamos 3 segundos
        yield return new WaitForSeconds(3f);

        // Restauramos el valor original
        gameObject.GetComponent<ControladorPersonaje>().velocidadMovimiento = valorOriginal;

    }




    private void Update()
    {
        // Aquí puedes controlar la lógica del juego para pausar si es necesario
        if (juegoEnPausa)
        {
            // Desactivar el comportamiento de juego aquí, por ejemplo:
            // Desactivar controles del jugador
            // Desactivar enemigos, etc.
        }
    }
}