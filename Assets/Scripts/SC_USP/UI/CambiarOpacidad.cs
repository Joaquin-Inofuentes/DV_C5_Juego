namespace USP.UI {
using UnityEngine;

public class CambiarOpacidad : MonoBehaviour
{
    public bool esTransparente; // Bool para controlar si el objeto es transparente
    public float nivelOpacidad = 0f; // Nivel de opacidad deseado (0 a 1)
    public float tiempoDeOpacidad = 0.08f; //Tiempo que dura la visibilidad del efecto

    private Renderer renderizador; // Renderer del GameObject
    private Material material;      // Instancia de material cacheada
    private Color colorOriginal;   // Color original del material
    private bool tieneColor;       // El material soporta _Color
    private bool? ultimoEstado;    // Último estado aplicado, para no escribir el color cada frame

    void Start()
    {
        // Obtener el Renderer del objeto al que est� asignado el script
        renderizador = GetComponent<Renderer>();

        esTransparente = true; //Inicializar al principio que es transparente al comienzo del juego

        // Cachear el material y el color original una sola vez
        if (renderizador != null)
        {
            material = renderizador.material; // Crea la instancia una única vez
            tieneColor = material.HasProperty("_Color");
            if (tieneColor) colorOriginal = material.color;
        }
    }

    void Update()
    {
        if (!tieneColor) return;

        // Solo escribimos el color cuando el estado cambió (evita asignaciones por frame)
        if (ultimoEstado.HasValue && ultimoEstado.Value == esTransparente) return;

        Color nuevoColor = colorOriginal;
        nuevoColor.a = esTransparente ? nivelOpacidad : 1.0f;
        material.color = nuevoColor;
        ultimoEstado = esTransparente;
    }
}
}
