using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Necesario para trabajar con escenas

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static GameObject player;

    public int puntajeActual = 0;

    // Referencia al texto donde se muestra el puntaje
    public Text textoPuntaje;

    // Lista de posiciones posibles donde aparecerán los enemigos
    public List<Transform> posicionesEnemigos; // Debe asignarse desde el Inspector
    public GameObject prefabEnemigo; // Prefab del enemigo que se generará
    public int maxEnemigos = 5; // Máximo número de enemigos en el juego
    public List<GameObject> enemigosGenerados = new List<GameObject>(); // Lista de enemigos generados

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(instance);
        }
        player = GameObject.Find("Soldado_Jugador");
        ActualizarPuntajeUI();

        // Inicia la generación de enemigos
        StartCoroutine(GenerarEnemigos());
        StartCoroutine(VerificarEnemigosNulos()); // Inicia la verificación de enemigos nulos
    }

    private void Update()
    {
        
        if (instance == null)
        {
            instance = this;
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            CambiarEscena("MenuInicial");
        }
        // Verifica y elimina enemigos nulos cada frame
        LimpiarEnemigosNulos();
    }

    /// <summary>
    /// Método para verificar y eliminar enemigos nulos de la lista.
    /// </summary>
    private void LimpiarEnemigosNulos()
    {
        // Usamos una lista temporal para almacenar enemigos a eliminar
        List<GameObject> enemigosAEliminar = new List<GameObject>();

        // Iteramos sobre la lista de enemigos generados
        foreach (GameObject enemigo in enemigosGenerados)
        {
            // Si el enemigo es nulo, lo añadimos a la lista de eliminación
            if (enemigo == null)
            {
                enemigosAEliminar.Add(enemigo);
            }
        }

        // Eliminamos todos los enemigos nulos de la lista original
        foreach (GameObject enemigo in enemigosAEliminar)
        {
            enemigosGenerados.Remove(enemigo);
        }
    }

    // Método para añadir puntos
    public void AñadirPuntos(int puntos)
    {
        puntajeActual += puntos;
        ActualizarPuntajeUI();
    }

    // Método para actualizar el texto del puntaje en la UI
    private void ActualizarPuntajeUI()
    {
        if (textoPuntaje != null)
        {
            textoPuntaje.text = $"Puntaje: {puntajeActual}";
        }
    }

    // Método para reiniciar la escena actual
    public void ReiniciarEscena()
    {
        Debug.Log("2");
        // Obtiene el nombre de la escena actual y la vuelve a cargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Corrutina para generar enemigos cada 5 segundos.
    /// </summary>
    private IEnumerator GenerarEnemigos()
    {
        while (true)
        {
            // Verifica si hay espacio para más enemigos
            if (enemigosGenerados.Count < maxEnemigos)
            {
                GenerarEnemigo();
            }
            yield return new WaitForSeconds(5f); // Espera 5 segundos
        }
    }

    /// <summary>
    /// Método para generar un nuevo enemigo en una posición aleatoria.
    /// </summary>
    private void GenerarEnemigo()
    {
        // Selecciona una posición aleatoria de la lista de posiciones
        int posicionIndex = Random.Range(0, posicionesEnemigos.Count);
        Transform posicionGeneracion = posicionesEnemigos[posicionIndex];

        // Instancia el enemigo y añade a la lista de enemigos generados
        GameObject nuevoEnemigo = Instantiate(prefabEnemigo, posicionGeneracion.position, Quaternion.identity);
        enemigosGenerados.Add(nuevoEnemigo);

        // Reinicia el script del enemigo para que funcione como nuevo
        nuevoEnemigo.GetComponent<Enemigo>().enabled = true; // Reinicia el comportamiento del enemigo
    }

    // Método para eliminar un enemigo de la lista
    public void EliminarEnemigo(GameObject enemigo)
    {
        // Asegura que el enemigo no sea nulo antes de eliminarlo
        if (enemigo != null)
        {
            enemigosGenerados.Remove(enemigo); // Elimina el enemigo de la lista
        }
    }

    /// <summary>
    /// Corrutina para verificar y eliminar enemigos nulos de la lista.
    /// </summary>
    private IEnumerator VerificarEnemigosNulos()
    {
        while (true)
        {
            // Itera sobre la lista de enemigos generados
            for (int i = enemigosGenerados.Count - 1; i >= 0; i--)
            {
                // Si el enemigo es nulo, lo elimina de la lista
                if (enemigosGenerados[i] == null)
                {
                    enemigosGenerados.RemoveAt(i); // Elimina el enemigo nulo
                }
            }
            yield return new WaitForSeconds(1f); // Espera 1 segundo antes de volver a verificar
        }
    }

    // Método para cargar una escena según el nombre
    public void CambiarEscena(string nombreEscena)
    {
        // Verifica si la escena existe en el build
        if (Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            // Carga la escena especificada por su nombre
            SceneManager.LoadScene(nombreEscena);
            Debug.Log($"Cambiando a la escena: {nombreEscena}");
        }
        else
        {
            Debug.LogError($"La escena {nombreEscena} no se encuentra en el build de escenas.");
        }
    }
    // Método para salir del juego
    public void SalirDelJuego()
    {
        // Si estamos en el editor de Unity, detiene la reproducción
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Si estamos en una compilación, cierra la aplicación
            Application.Quit();
#endif

        Debug.Log("Saliendo del juego...");
    }

}
