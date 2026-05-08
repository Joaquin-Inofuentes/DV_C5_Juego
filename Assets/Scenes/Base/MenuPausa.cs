using UnityEngine;

public class MenuPausa : MonoBehaviour
{
    // Arrastra aquí tu panel de menú desde la jerarquía
    public GameObject objetoMenu;

    public bool estaPausado = false;

    void Update()
    {
        // Detecta si presionas la tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    // Método público para volver al juego (útil para un botón "Continuar")
    public void Reanudar()
    {
        objetoMenu.SetActive(false); // Esconde el menú
        Time.timeScale = 1f;         // Restablece el tiempo/timer
        estaPausado = false;
    }

    void Pausar()
    {
        objetoMenu.SetActive(true);  // Muestra el menú
        Time.timeScale = 0f;         // Congela todo el juego
        estaPausado = true;
    }
}