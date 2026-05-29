using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la información del personaje, como salud, munición y recargas.
/// Debe estar asociado al objeto principal del jugador.
/// </summary>
public class InformacionPersonaje : MonoBehaviour
{
    [Header("Referencias del Sistema de Armas")]
    public WeaponController cambioDeArma;

    [Header("UI - Munición")]
    public string IndicadorDeBalas;

    [Header("Salud y Armadura")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;
    public float armadura = 50f;

    [Header("Inventario")]
    public int balasPorCargador = 30;
    public int balasRestantes;
    public int kitsMedicos = 2;
    public int granadas = 5;
    public int adrenalina = 2;

    [Header("Referencias de UI en Pantalla")]
    public Text textoVida;
    public Text textoArmadura;
    public Text textoMunicion;
    public Text textoRecargas;
    public Text textoKitsMedicos;
    public Text textoGranadas;
    public Text textoAdrenalina;
    public Text textoContadorMuerte;
    public Transform barraVida;

    [Header("Efectos Visuales")]
    public GameObject EfectoViñetaNegra;

    [Header("Estado del Juego")]
    public float tiempoRestanteParaReiniciar = 3f;
    public bool juegoEnPausa = false;

    private void Start()
    {
        InicializarValores();
        ActualizarUI();
    }

    private void InicializarValores()
    {
        vidaActual = vidaMaxima;
        balasRestantes = balasPorCargador;
    }

    public void RecibirDanio(float danio)
    {
        vidaActual -= danio;
        if (vidaActual < 0) vidaActual = 0;

        ActualizarUI();

        if (vidaActual <= 0) Morir();
    }

    private void Morir()
    {
        juegoEnPausa = true;
        if (textoContadorMuerte != null) textoContadorMuerte.gameObject.SetActive(true);
        StartCoroutine(ContadorReinicio());
    }

    private IEnumerator ContadorReinicio()
    {
        while (tiempoRestanteParaReiniciar > 0)
        {
            if (textoContadorMuerte != null)
            {
                textoContadorMuerte.text = $"Reiniciando en: {Mathf.Ceil(tiempoRestanteParaReiniciar)}";
            }
            yield return new WaitForSeconds(1f);
            tiempoRestanteParaReiniciar--;
        }
        GameManager.instance.GetComponent<GameManager>().ReiniciarEscena();
    }

    public void ReAparecer()
    {
        Debug.Log("Re Aparecí");
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
        // Viñeta de daño por porcentaje de vida
        if (EfectoViñetaNegra != null)
        {
            Image componenteImagen = EfectoViñetaNegra.GetComponent<Image>();
            if (componenteImagen != null)
            {
                float porcentajeVida = vidaActual / vidaMaxima;
                Color colorViñeta = componenteImagen.color;
                colorViñeta.a = Mathf.Clamp01((1f - porcentajeVida) * 10f);
                componenteImagen.color = colorViñeta;
            }
        }

        // Textos del HUD
        if (textoVida != null) textoVida.text = $"Vida: {vidaActual}";
        if (textoArmadura != null) textoArmadura.text = $"Armadura: {armadura}";

        // Obtener texto de munición desde el WeaponController
        if (cambioDeArma != null)
        {
            if (textoMunicion != null) textoMunicion.text = cambioDeArma.IndicadorDeBalas;

            if (cambioDeArma.model != null)
            {
                int actual = cambioDeArma.model.NumeroDeArmaActual;
                if (textoRecargas != null && cambioDeArma.model.reservaTotal != null && cambioDeArma.model.reservaTotal.Length > actual)
                {
                    textoRecargas.text = $"Reserva: {cambioDeArma.model.reservaTotal[actual]}";
                }
            }
        }

        if (textoKitsMedicos != null) textoKitsMedicos.text = $"{kitsMedicos}";
        if (textoGranadas != null) textoGranadas.text = $"{granadas}";
        if (textoAdrenalina != null) textoAdrenalina.text = $"{adrenalina}";

        ActualizarBarraVida(barraVida);
    }

    public void CurarAlMaximo()
    {
        if (kitsMedicos > 0)
        {
            kitsMedicos--;
            vidaActual = vidaMaxima;
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
        if (cambioDeArma != null && cambioDeArma.model != null)
        {
            int cantidadAñadir = cambioDeArma.model.cargadorMaximo[NumeroDeArma];
            cambioDeArma.model.reservaTotal[NumeroDeArma] += cantidadAñadir;
            Debug.Log($"Añadidas {cantidadAñadir} balas a la reserva del arma {NumeroDeArma}");
            ActualizarUI();
        }
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
        StartCoroutine(CambiarValorTemporalmente());
    }

    private IEnumerator CambiarValorTemporalmente()
    {
        // Puedes agregar comportamiento temporal de adrenalina extra aquí si lo requieres
        yield return null;
    }

    public void ActualizarBarraVida(Transform barraVida)
    {
        if (barraVida != null)
        {
            float porcentajeVida = vidaActual / vidaMaxima;
            barraVida.localScale = new Vector3(porcentajeVida, 1f, 1f);
        }
    }
}
