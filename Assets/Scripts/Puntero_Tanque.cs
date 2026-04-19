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
        // El puntero sigue al jugador durante el tiempo establecido
        while (tiempoTranscurrido < tiempoSeguir)
        {
            transform.position = GameObject.Find("Soldado_Jugador").transform.position;
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Después de 4 segundos, el puntero se destruye
        Destroy(gameObject); // Destruimos el puntero
        tanque.PunteroDestruido(); // Notificamos al tanque que el puntero ha terminado su ciclo
    }
}