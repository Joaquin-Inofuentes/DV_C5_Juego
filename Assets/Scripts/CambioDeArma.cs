using System.Collections;
using UnityEngine;

public class CambioDeArma : MonoBehaviour
{
    [Header("Referencias")]
    public Proyectil proyectil;
    public InformacionPersonaje infoPersonaje;
    public CambiarOpacidad cambiarOpacidad;
    public Transform origenDisparo;
    public GameObject prefabBala;

    [Header("UI")]
    public string IndicadorDeBalas;

    [Header("Visuales de Armas (Asignar 3 en Inspector)")]
    public GameObject[] modelosArmas;
    public GameObject[] modelosArmas1;

    [Header("Configuración de Armas")]
    public string[] tiposDeArmas = { "Pistola", "Metralleta", "Escopeta" };
    public float[] danoArmas = { 10f, 4f, 40f };
    public float[] cadenciaArmas = { 0.2f, 0.08f, 1.0f };

    // NUEVO: Fieltro para definir si el arma dispara manteniendo o por click
    // true = Metralleta (Mantener presionado), false = Pistola/Escopeta (Click por click)
    [Header("Modo de Disparo (true = Automática, false = Semiautomática)")]
    public bool[] armasAutomáticas = { false, true, false };

    [Header("Munición (Estilo COD)")]
    public int[] cargadorMaximo = { 15, 30, 6 };
    public int[] balasEnCargador;
    public int[] reservaTotal;

    public int NumeroDeArmaActual = 0;
    private float tiempoDesdeUltimoDisparo;

    void Start()
    {
        balasEnCargador = new int[3];
        reservaTotal = new int[] { 45, 90, 18 };

        for (int i = 0; i < cargadorMaximo.Length; i++)
        {
            balasEnCargador[i] = cargadorMaximo[i];
        }

        ActualizarArma();
    }

    void Update()
    {
        ManejarCambioTeclado();
        tiempoDesdeUltimoDisparo += Time.deltaTime;

        // NUEVO: Detectar el tipo de click según el arma actual
        if (DetectarIntentoDisparo() && PuedeDisparar())
        {
            Disparar();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            IntentarRecargar();
        }
    }

    // Segun si es true o false se usa disparo continuo uo por click
    // string[] tiposDeArmas = { "Pistola", "Metralleta", "Escopeta" };
    // bool[] armasAutomáticas = { false, true, false };
    bool DetectarIntentoDisparo()
    {
        if (armasAutomáticas[NumeroDeArmaActual])
        {
            return Input.GetButton("Fire1"); // Mantenido (Metralleta)
        }
        else
        {
            return Input.GetButtonDown("Fire1"); // Un solo click (Pistola y Escopeta)
        }
    }

    void ManejarCambioTeclado()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SeleccionarArma(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SeleccionarArma(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SeleccionarArma(2);
    }

    void SeleccionarArma(int indice)
    {
        if (indice >= 0 && indice < tiposDeArmas.Length && indice != NumeroDeArmaActual)
        {
            NumeroDeArmaActual = indice;
            ActualizarArma();
        }
    }

    void ActualizarArma()
    {
        for (int i = 0; i < modelosArmas.Length; i++)
        {
            if (modelosArmas[i] != null)
                modelosArmas[i].SetActive(i == NumeroDeArmaActual);
        }
        for (int i = 0; i < modelosArmas.Length; i++)
        {
            if (modelosArmas1[i] != null)
                modelosArmas1[i].SetActive(i == NumeroDeArmaActual);
        }

        if (proyectil != null) proyectil.dano = danoArmas[NumeroDeArmaActual];

        ActualizarTextoMunicion();
        if (infoPersonaje != null) infoPersonaje.ActualizarUI();
    }

    bool PuedeDisparar()
    {
        return tiempoDesdeUltimoDisparo >= cadenciaArmas[NumeroDeArmaActual] && balasEnCargador[NumeroDeArmaActual] > 0;
    }

    void Disparar()
    {
        tiempoDesdeUltimoDisparo = 0f;
        balasEnCargador[NumeroDeArmaActual]--;

        Instantiate(prefabBala, origenDisparo.position, origenDisparo.rotation);

        BD_Audios.ReproducirConSolapamiento($"Disparo de {tiposDeArmas[NumeroDeArmaActual]}");

        if (cambiarOpacidad != null)
        {
            StopAllCoroutines();
            StartCoroutine(EfectoDisparoFlash());
        }

        ActualizarTextoMunicion();
        if (infoPersonaje != null) infoPersonaje.ActualizarUI();

        if (balasEnCargador[NumeroDeArmaActual] <= 0) IntentarRecargar();
    }

    IEnumerator EfectoDisparoFlash()
    {
        cambiarOpacidad.esTransparente = false;
        yield return new WaitForSeconds(0.1f);
        cambiarOpacidad.esTransparente = true;
    }

    void IntentarRecargar()
    {
        int actual = NumeroDeArmaActual;
        if (balasEnCargador[actual] == cargadorMaximo[actual] || reservaTotal[actual] <= 0) return;

        int balasNecesarias = cargadorMaximo[actual] - balasEnCargador[actual];
        int aRecargar = Mathf.Min(balasNecesarias, reservaTotal[actual]);

        balasEnCargador[actual] += aRecargar;
        reservaTotal[actual] -= aRecargar;

        BD_Audios.ReproducirConSolapamiento($"Recarga de {tiposDeArmas[actual]}");

        ActualizarTextoMunicion();
        if (infoPersonaje != null) infoPersonaje.ActualizarUI();
    }

    void ActualizarTextoMunicion()
    {
        IndicadorDeBalas = $"{balasEnCargador[NumeroDeArmaActual]} / {reservaTotal[NumeroDeArmaActual]}";
    }
}