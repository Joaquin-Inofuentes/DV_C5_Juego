using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas

public class MenuVictoria : MonoBehaviour
{
    public string NombreDeLaEscenaDeJuego = "Juego V4";
    // Función para salir del juego
    public void SalirDelJuego()
    {
        // Cierra la aplicación
        Application.Quit();
        // Para pruebas en el editor, también puedes parar la ejecución
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Función para continuar jugando
    public void ContinuarJugando()
    {
        // Asumiendo que deseas cargar la escena principal, cambia "NombreDeLaEscena" por el nombre de tu escena
        SceneManager.LoadScene(NombreDeLaEscenaDeJuego); // Reemplaza con el nombre de tu escena
    }
}
