using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Gestiona el comportamiento del enemigo.
/// El enemigo dispara, recibe da�o y tiene una barra de vida.
/// Desaparece al recibir un disparo.
/// </summary>
public class Enemigo : MonoBehaviour
{
    public Text DialogoEnemigo;

    public Transform objetivo; // Referencia al transform del jugador
    public GameObject prefabBala; // Prefab de la bala que disparar� el enemigo
    public Transform puntoDisparo; // Punto desde donde se disparar�n las balas
    public float velocidadBala = 10f; // Velocidad de la bala
    public float vidaActual = 100f; // Vida actual del enemigo
    public float vidaMaxima = 100f; // Vida m�xima del enemigo
    public float armadura = 2f; // Armadura del enemigo
    public int puntosPorEliminar = 10; // Puntos que se sumar�n al eliminar al enemigo

    public GameManager gameManager; // Referencia al GameManager
    public bool puedeDisparar = false; // Controla si el enemigo puede disparar
    public bool estaDisparando = false; // Evita m�ltiples corrutinas de disparo

    public float tiempoEntreDisparos = 2f; // Tiempo entre disparos
    public float temporizadorDisparo = 0f; // Contador para el disparo

    
    private void Start()
    {
        // Validar que las referencias necesarias est�n asignadas
        if (objetivo == null)
        {
            objetivo = GameObject.Find("Soldado_Jugador").transform;
        }

        // Obtener la referencia al GameManager
        gameManager = GameManager.instance;

        // Asegurar que la vida actual no exceda la m�xima al inicio
        vidaActual = Mathf.Min(vidaActual, vidaMaxima);
    }

    private void Update()
    {
        if (objetivo == null)
        {
            objetivo = GameObject.Find("Soldado_Jugador").transform;
        }

        // Actualiza el temporizador de disparo
        if (puedeDisparar)
        {
            temporizadorDisparo += Time.deltaTime; // Incrementa el temporizador con el tiempo desde el �ltimo frame

            // Verifica si el temporizador ha superado el tiempo de espera
            if (temporizadorDisparo >= tiempoEntreDisparos)
            {
                // Lanza un raycast hacia el jugador
                if (RaycastHaciaJugador())
                {
                    Disparar(); // Dispara si no hay obst�culos
                }

                // Reinicia el temporizador
                temporizadorDisparo = 0f;
            }
        }

        if (vidaActual <= 0)
        {
            Debug.Log("Eliminado");
            //<<<<<<< HEAD
            GameObject.Destroy(gameObject);

            if (DialogoEnemigo != null) DialogoEnemigo.text = $"AAAAHHH !";
            //=======
            Morir();
            //>>>>>>> 82baa7e99e94ff59a79ca76c907c8c2b9c2bd736
        }
    }



    // M�todo que lanza un raycast hacia el jugador
    // M�todo que lanza un raycast hacia el jugador
    private bool RaycastHaciaJugador()
    {
        // Calcula la direcci�n hacia el jugador
        Vector2 direccion = (objetivo.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direccion, Mathf.Infinity); // Lanza el raycast

        // Verifica si el raycast golpea un objeto
        if (hit.collider != null)
        {
            // Se asegura de que el objeto golpeado no sea el jugador ni a s� mismo
            if (hit.collider.gameObject != objetivo.gameObject && hit.collider.gameObject != gameObject)
            {
                Debug.Log("Hay un obst�culo en el camino: " + hit.collider.gameObject.name); // Mensaje de depuraci�n para obst�culos
                return false; // No dispara si hay un obst�culo
            }
            else
            {
                
                //Debug.Log("El jugador est� en la l�nea de visi�n."); // Mensaje de depuraci�n si el jugador est� en la l�nea de visi�n
            }
        }
        else
        {
            Debug.Log("No hay nada en la l�nea de visi�n."); // Mensaje de depuraci�n si no hay obst�culos
        }

        return true; // Dispara si no hay obst�culos
    }









    /// <summary>
    /// Gestiona el disparo del enemigo.
    /// </summary>
    private void Disparar()
    {
        if (prefabBala == null)
        {
            Debug.LogError("Error: El prefab de la bala no est� asignado."); // Mensaje de error si el prefab de la bala no est� asignado
            return; // No dispara si el prefab no est� disponible
        }

        if (puntoDisparo == null)
        {
            Debug.LogError("Error: El punto de disparo no est� asignado."); // Mensaje de error si el punto de disparo no est� asignado
            return; // No dispara si el punto de disparo no est� disponible
        }

        //Debug.Log("Disparando..."); // Agrega este mensaje para ver si el m�todo se llama
        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, puntoDisparo.rotation);
        Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();
        if (rbBala != null)
        {
            rbBala.velocity = puntoDisparo.right * velocidadBala;
            //Debug.Log("Bala disparada con velocidad: " + velocidadBala); // Mensaje de depuraci�n para verificar la velocidad de la bala
        }
        else
        {
            Debug.LogError("Error: No se encontr� Rigidbody2D en la bala."); // Mensaje de error si el Rigidbody2D no se encuentra
        }
    }




    /// <summary>
    /// Aplica da�o al enemigo.
    /// </summary>
    public void RecibirDanio(float danio)
    {
        vidaActual -= danio;

        if (vidaActual < 0) vidaActual = 0;
        if (vidaActual <= 0) Morir();
    }

    /// <summary>
    /// Destruye al enemigo cuando su vida llega a cero.
    /// </summary>
    void Morir()
    {
        if (gameManager != null)
        {
            gameManager.AñadirPuntos(puntosPorEliminar);
        }

        Destroy(gameObject);
    }

    // M�todo para destruir el enemigo manualmente
    public void DestruirEnemigo()
    {
        Destroy(gameObject);
    }

    public Texture2D texturaVida; // Textura de vida del enemigo
    public Texture2D texturaFondoVida; // Textura de fondo para la barra de vida
    private void OnGUI()
    {
        float ancho = 250;
        float alto = 30; // Aumentar la altura de la barra de vida

        // Obtener la posici�n en pantalla del objeto
        Vector3 posicionEnPantalla = Camera.main.WorldToScreenPoint(transform.position);

        float posX = posicionEnPantalla.x - ancho / 2;
        float posY = posicionEnPantalla.y - alto - 5 - 35; // Ajustar la posici�n vertical

        // Dibujar el fondo de la barra de vida en gris
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Color gris
        GUI.Box(new Rect(posX, Screen.height - posY, ancho, alto), "");

        // Dibujar la barra de vida en verde
        GUI.color = new Color(0f, 1f, 0f, 1f); // Color verde
        GUI.Box(new Rect(posX, Screen.height - posY, ancho * (vidaActual / vidaMaxima), alto), "");

        // Establecer el tama�o de la fuente y centrar el texto debajo de la barra de vida
        GUIStyle estiloTexto = new GUIStyle(GUI.skin.label);
        estiloTexto.fontSize = 30; // Aumentar el tama�o del texto
        estiloTexto.alignment = TextAnchor.MiddleCenter; // Centrar el texto

        // Mostrar la cantidad de vida actual y m�xima
        GUI.color = Color.white; // Restaurar color blanco para el texto
        GUI.Label(new Rect(posX, Screen.height - posY + alto + 5, ancho, 30), $"{vidaActual}/{vidaMaxima}", estiloTexto); // Aumentar la altura del rect�ngulo del texto

        // Restaurar color blanco al final
        GUI.color = Color.white;
    }



}
