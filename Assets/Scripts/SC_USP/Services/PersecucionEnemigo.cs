using USP.Entities;
using UnityEngine;

/// <summary>
/// Gestiona la persecuci�n del enemigo hacia el jugador.
/// El enemigo se mover� hacia el jugador y se detendr� cuando est� a una distancia espec�fica.
/// </summary>
public class PersecucionEnemigo : MonoBehaviour
{
    public Transform objetivo; // Referencia al transform del jugador
    public float velocidadMovimiento = 3f; // Velocidad de movimiento del enemigo
    public float distanciaDetenerse = 2.5f; // Distancia a la que el enemigo se detendr� del jugador
    public Enemigo enemigoScript; // Referencia al script Enemigo para que pueda mirar al jugador y disparar
    public float distanciaVisible = 10f; // Distancia m�xima a la que el enemigo puede ver al jugador

    private void Start()
    {
        enemigoScript = GetComponent<Enemigo>();

        if (objetivo == null)
        {
            GameObject jugador = GameObject.Find("Soldado_Jugador");
            if (jugador != null) objetivo = jugador.transform;
        }
    }

    private void Update()
    {
        if (objetivo != null)
        {
            PerseguirJugador();
        }
    }

    /// <summary>
    /// Persigue al jugador y se detiene cuando est� lo suficientemente cerca.
    /// </summary>
    private void PerseguirJugador()
    {
        // Calcula la distancia al jugador
        float distanciaAlObjetivo = Vector2.Distance(transform.position, objetivo.position);

        // Solo persigue al jugador si est� dentro de la distancia visible
        if (distanciaAlObjetivo <= distanciaVisible)
        {
            // Contin�a la l�gica de persecuci�n
            if (distanciaAlObjetivo > distanciaDetenerse)
            {
                Vector2 direccion = (objetivo.position - transform.position).normalized;
                transform.position += (Vector3)direccion * velocidadMovimiento * Time.deltaTime;

                enemigoScript.puedeDisparar = false; // No puede disparar mientras se mueve
                MirarJugador();
            }
            else
            {
                enemigoScript.puedeDisparar = true; // Puede disparar si est� lo suficientemente cerca
                MirarJugador(); // Llama a la funci�n para que el enemigo mire al jugador
            }
        }
        else
        {
            // Si el jugador est� fuera de la distancia visible, el enemigo deja de moverse y mirar
            enemigoScript.puedeDisparar = false; // No puede disparar si el jugador no est� visible
        }
    }

    /// <summary>
    /// Hace que el enemigo mire al jugador.
    /// </summary>
    public void MirarJugador()
    {
        Vector2 direccion = (objetivo.position - transform.position).normalized;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angulo));
    }
}

