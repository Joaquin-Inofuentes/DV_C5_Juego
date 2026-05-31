using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puntero_Tanque : MonoBehaviour
{
    // Variables
    public float tiempoSeguir = 2f; // Tiempo en segundos que sigue al jugador
    public Tanque tanque; // Referencia al script del tanque

    void Start()
    {
        tanque = GameObject.FindObjectOfType<Tanque>(); // Buscar el tanque en la escena
        StartCoroutine(SeguirJugador());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log(3);
            StartCoroutine(SeguirJugador());
        }
    }

    public IEnumerator SeguirJugador()
    {
        float tiempoTranscurrido = 0f;
        //Debug.Log(1);

        // Cacheamos el transform del jugador una sola vez (evita GameObject.Find cada frame)
        GameObject jugador = GameObject.Find("Soldado_Jugador");
        Transform jugadorTransform = jugador != null ? jugador.transform : null;

        // El puntero sigue al jugador durante el tiempo establecido
        while (tiempoTranscurrido < tiempoSeguir)
        {
            if (jugadorTransform != null)
                transform.position = jugadorTransform.position;
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Despu�s de 4 segundos, el puntero se destruye
        Destroy(gameObject); // Destruimos el puntero
        if (tanque != null)
            tanque.PunteroDestruido(); // Notificamos al tanque que el puntero ha terminado su ciclo
    }
}