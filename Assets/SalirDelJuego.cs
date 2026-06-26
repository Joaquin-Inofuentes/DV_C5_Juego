using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SalirDelJuego : MonoBehaviour
{
    // Función pública para que puedas llamarla desde un botón (On Click)
    public void Salir()
    {
        // Esto imprimirá un mensaje en la consola para que sepas que funciona en el Editor
        Debug.Log("Saliendo del juego...");

        // Esto cerrará el juego en la versión final (Build)
        Application.Quit();
    }
}