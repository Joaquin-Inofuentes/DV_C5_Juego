using UnityEngine;

/// <summary>
/// Gestiona la persecución del enemigo hacia el jugador.
/// El enemigo se moverá hacia el jugador y se detendrá cuando esté a una distancia específica.
/// </summary>
public class PersecucionEnemigo : MonoBehaviour
{
    public Transform objetivo; // Referencia al transform del jugador
    public float velocidadMovimiento = 3f; // Velocidad de movimiento del enemigo
    public float distanciaDetenerse = 2.5f; // Distancia a la que el enemigo se detendrá del jugador
    public Enemigo enemigoScript; // Referencia al script Enemigo para que pueda mirar al jugador y disparar
    public float distanciaVisible = 10f; // Distancia máxima a la que el enemigo puede ver al jugador

    private void Start()
    {
        enemigoScript = GetComponent<Enemigo>();

        if (objetivo == null)
        {
            objetivo = GameObject.Find("Soldado_Jugador").transform;
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
    /// Persigue al jugador y se detiene cuando está lo suficientemente cerca.
    /// </summary>
    private void PerseguirJugador()
    {
        // Calcula la distancia al jugador
        float distanciaAlObjetivo = Vector2.Distance(transform.position, objetivo.position);

        // Solo persigue al jugador si está dentro de la distancia visible
        if (distanciaAlObjetivo <= distanciaVisible)
        {
            // Continúa la lógica de persecución
            if (distanciaAlObjetivo > distanciaDetenerse)
            {
                Vector2 direccion = (objetivo.position - transform.position).normalized;
                transform.position += (Vector3)direccion * velocidadMovimiento * Time.deltaTime;

                enemigoScript.puedeDisparar = false; // No puede disparar mientras se mueve
                MirarJugador();
            }
            else
            {
                enemigoScript.puedeDisparar = true; // Puede disparar si está lo suficientemente cerca
                MirarJugador(); // Llama a la función para que el enemigo mire al jugador
            }
        }
        else
        {
            // Si el jugador está fuera de la distancia visible, el enemigo deja de moverse y mirar
            enemigoScript.puedeDisparar = false; // No puede disparar si el jugador no está visible
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
