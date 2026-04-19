using UnityEngine;

public class CambiarOpacidad : MonoBehaviour
{
    public bool esTransparente; // Bool para controlar si el objeto es transparente
    public float nivelOpacidad = 0f; // Nivel de opacidad deseado (0 a 1)
    public float tiempoDeOpacidad = 0.08f; //Tiempo que dura la visibilidad del efecto

    private Renderer renderizador; // Renderer del GameObject
    private Color colorOriginal; // Color original del material

    void Start()
    {
        // Obtener el Renderer del objeto al que está asignado el script
        renderizador = GetComponent<Renderer>();

        esTransparente = true; //Inicializar al principio que es transparente al comienzo del juego

        // Guardar el color original del material
        if (renderizador != null && renderizador.material.HasProperty("_Color"))
        {
            colorOriginal = renderizador.material.color;
        }
    }

    void Update()
    {
        // Modificar la opacidad según el estado del bool
        if (renderizador != null && renderizador.material.HasProperty("_Color"))
        {
            Color nuevoColor = colorOriginal;

            if (esTransparente)
            {
                nuevoColor.a = nivelOpacidad; // Cambiar a la opacidad definida
            }
            else
            {
                nuevoColor.a = 1.0f; // Opacidad completa
            }

            renderizador.material.color = nuevoColor;
        }
    }
}