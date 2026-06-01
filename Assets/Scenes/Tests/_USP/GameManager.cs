using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using USP.Entities;

namespace USP.Services
{
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
        else if (instance != this)
        {
            // Ya existe un GameManager: destruir este duplicado, no el original
            Destroy(gameObject);
            return;
        }
        player = GameObject.Find("Soldado_Jugador");
        ActualizarPuntajeUI();

        // Inicia la generación de enemigos
        StartCoroutine(GenerarEnemigos());
        StartCoroutine(VerificarEnemigosNulos());
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
        LimpiarEnemigosNulos();
    }

    private void LimpiarEnemigosNulos()
    {
        List<GameObject> enemigosAEliminar = new List<GameObject>();

        foreach (GameObject enemigo in enemigosGenerados)
        {
            if (enemigo == null)
            {
                enemigosAEliminar.Add(enemigo);
            }
        }

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator GenerarEnemigos()
    {
        while (true)
        {
            if (enemigosGenerados.Count < maxEnemigos)
            {
                GenerarEnemigo();
            }
            yield return new WaitForSeconds(5f);
        }
    }

    private void GenerarEnemigo()
    {
        // Validación de lista de posiciones vacía o nula para evitar ArgumentOutOfRangeException
        if (posicionesEnemigos == null || posicionesEnemigos.Count == 0)
        {
            Debug.LogWarning("[GameManager] No hay posiciones de generación de enemigos asignadas en la lista 'posicionesEnemigos' del Inspector.");
            return;
        }

        int posicionIndex = Random.Range(0, posicionesEnemigos.Count);
        Transform posicionGeneracion = posicionesEnemigos[posicionIndex];

        if (posicionGeneracion == null) return;

        if (prefabEnemigo == null)
        {
            Debug.LogError("[GameManager] ¡Falta prefabEnemigo! Asígnalo en el Inspector para poder instanciar enemigos.");
            return;
        }

        GameObject nuevoEnemigo = Instantiate(prefabEnemigo, posicionGeneracion.position, Quaternion.identity);
        enemigosGenerados.Add(nuevoEnemigo);

        var enemigoComp = nuevoEnemigo.GetComponent<Enemigo>();
        if (enemigoComp != null)
        {
            enemigoComp.enabled = true;
        }
    }

    public void EliminarEnemigo(GameObject enemigo)
    {
        if (enemigo != null)
        {
            enemigosGenerados.Remove(enemigo);
        }
    }

    private IEnumerator VerificarEnemigosNulos()
    {
        while (true)
        {
            for (int i = enemigosGenerados.Count - 1; i >= 0; i--)
            {
                if (enemigosGenerados[i] == null)
                {
                    enemigosGenerados.RemoveAt(i);
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    public void CambiarEscena(string nombreEscena)
    {
        if (Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            SceneManager.LoadScene(nombreEscena);
            Debug.Log($"Cambiando a la escena: {nombreEscena}");
        }
        else
        {
            Debug.LogError($"La escena {nombreEscena} no se encuentra en el build de escenas.");
        }
    }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("Saliendo del juego...");
    }
}
}
