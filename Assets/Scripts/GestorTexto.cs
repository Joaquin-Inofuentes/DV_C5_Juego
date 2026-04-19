using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Este script gestiona la visualización del texto en la UI.
/// </summary>
public class GestorTexto : MonoBehaviour
{
    // Referencia al objeto de texto en la UI
    private Text textoRecogido; // Se inicializa automáticamente
    private float temporizadorOcultarTexto; // Temporizador para ocultar el texto
    public float tiempoDeVisibilidad = 3f; // Tiempo que el texto será visible
    private bool textoVisible = false; // Estado de visibilidad del texto

    private void Start()
    {
        // Buscar el objeto de texto al inicio
        BuscarTextoRecogido();

        // Asegurarse de que el texto esté oculto al inicio
        if (textoRecogido != null)
        {
            textoRecogido.gameObject.transform.localScale = Vector3.zero; // Inicialmente oculto
        }
    }

    private void Update()
    {
        // Buscar el objeto de texto si no está asignado
        if (textoRecogido == null)
        {
            BuscarTextoRecogido();
        }

        // Manejar el temporizador para ocultar el texto
        if (textoVisible)
        {
            temporizadorOcultarTexto -= Time.deltaTime; // Reducir el tiempo para ocultar texto
            if (temporizadorOcultarTexto <= 0)
            {
                OcultarTexto(); // Ocultar el texto
            }
        }
    }

    /// <summary>
    /// Busca el objeto de texto en la escena por su nombre.
    /// </summary>
    private void BuscarTextoRecogido()
    {
        GameObject objetoTexto = GameObject.Find("Interfaz_Soldado"); // Buscar el objeto en la escena
        if (objetoTexto != null)
        {
            textoRecogido = objetoTexto.GetComponent<Text>(); // Obtener el componente Text
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'Interfaz_Soldado'."); // Avisar si no se encontró
        }
    }

    /// <summary>
    /// Muestra el texto durante un tiempo específico.
    /// </summary>
    /// <param name="mensaje">El mensaje que se mostrará.</param>
    public void MostrarTexto(string mensaje)
    {
        if (textoRecogido != null)
        {
            textoRecogido.text = mensaje; // Asignar el mensaje al texto
            textoRecogido.gameObject.transform.localScale = Vector3.one; // Ajustar la escala a (1, 1, 1)
            temporizadorOcultarTexto = tiempoDeVisibilidad; // Reiniciar temporizador para ocultar texto
            textoVisible = true; // Marcar texto como visible
        }
        else
        {
            Debug.LogWarning("No se puede mostrar el texto porque 'Texto_Conseguiste' no está asignado."); // Avisar si no se puede mostrar el texto
        }
    }

    /// <summary>
    /// Oculta el texto ajustando su escala a (0, 0, 0).
    /// </summary>
    private void OcultarTexto()
    {
        if (textoRecogido != null)
        {
            textoRecogido.gameObject.transform.localScale = Vector3.zero; // Ajustar la escala a (0, 0, 0)
            textoVisible = false; // Marcar texto como no visible
        }
    }
}
