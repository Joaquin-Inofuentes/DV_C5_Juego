using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BD_Audios : MonoBehaviour
{
    // Diccionario estático público para almacenar los sonidos, clave es el nombre del archivo y el valor es el AudioSource
    public static Dictionary<string, AudioSource> Sonidos = new Dictionary<string, AudioSource>();



    private void Start()
    {
        // Cargar los audios al iniciar
        CargarAudios();
        ReproducirBucleConVolumen("fondo", false, 0.3f);
        if (CoroutineHelper.Instance == null)
        {
            GameObject helper = new GameObject("CoroutineHelper");
            helper.AddComponent<CoroutineHelper>();
        }
    }

    private void Update()
    {
        if (Sonidos.Count == 0)
        {
            CargarAudios();
        }
        // Recargar los sonidos si se presiona F5
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("Recargando sonidos...");
            CargarAudios();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log(Sonidos.Count);
        }
    }

    public static void ReproducirAudioUnaVez(string nombre)
    {
        List<AudioSource> Audios = Sonidos
    .Where(pair => pair.Key.Contains(nombre)) // Filtra las entradas por la clave
    .Select(pair => pair.Value) // Obtén el AudioSource asociado a la clave
    .ToList();

        bool SeEstaReproduciendo = true;
        // Verificar si el nombre del sonido existe en el diccionario
        foreach (var audio in Audios)
        {
            // Reproducir solo una vez, sin interferir con otros sonidos
            if (audio.isPlaying)
            {
                SeEstaReproduciendo = false;
                break;
            }
            if (SeEstaReproduciendo == true)
            {
                audio.PlayOneShot(audio.clip);
            }
        }
    }

    /// <summary>
    /// Detiene la reproducción de un sonido específico basado en su nombre.
    /// </summary>
    /// <param name="nombre">El nombre del audio a detener.</param>
    public static void DetenerAudio(string nombre)
    {
        List<AudioSource> Audios = Sonidos
    .Where(pair => pair.Key.Contains(nombre)) // Filtra las entradas por la clave
    .Select(pair => pair.Value) // Obtén el AudioSource asociado a la clave
    .ToList();
        // Verificar si el nombre del sonido existe en el diccionario
        foreach (var audio in Audios)
        {

            Debug.Log(audio.ToString());
            // Detener el sonido si está en reproducción
            if (audio.isPlaying)
            {
                audio.Stop();
                Debug.Log($"El audio {nombre} ha sido detenido.");
            }
            else
            {
                Debug.Log($"El audio {nombre} no está reproduciéndose.");
            }
        }
    }

    public static bool ReproducirConSolapamiento(string palabraClave)
    {
        // Filtrar los sonidos que contienen la palabra clave
        var sonidosFiltrados = Sonidos.Keys.Where(nombre => nombre.Contains(palabraClave)).ToList();

        // Verificar si hay sonidos que coinciden
        if (sonidosFiltrados.Count > 0)
        {
            // Seleccionar un sonido al azar
            string sonidoAleatorio = sonidosFiltrados[Random.Range(0, sonidosFiltrados.Count)];

            // Obtener el AudioSource correspondiente
            AudioSource audioSource = Sonidos[sonidoAleatorio];

            // Reproducir el audio sin detener el actual
            audioSource.PlayOneShot(audioSource.clip);

            //Debug.Log($"Reproduciendo audio: {sonidoAleatorio}");
            return true; // Indicar éxito
        }
        else
        {
            Debug.LogError($"No se encontró ningún audio que contenga la palabra clave: {palabraClave}");
            return false; // Indicar fallo
        }
    }



    /// <summary>
    /// Carga todos los audios de la carpeta Resources/Audios y los agrega al diccionario de sonidos.
    /// </summary>
    public static void CargarAudios()
    {
        Sonidos.Clear(); // Limpiar el diccionario antes de recargar

        // Cargar todos los clips de la carpeta Resources/Audios
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audios");

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                // Crear un nuevo GameObject para el AudioSource
                GameObject nuevoSonidoGO = new GameObject(clip.name);
                AudioSource nuevoAudioSource = nuevoSonidoGO.AddComponent<AudioSource>();
                nuevoAudioSource.clip = clip;

                // Añadir el AudioSource al diccionario con el nombre del archivo como clave
                Sonidos.Add(clip.name, nuevoAudioSource);
                //Debug.Log($"Se cargó el sonido: {clip.name}");
            }
        }
    }

    /// <summary>
    /// Busca un AudioSource basado en su nombre.
    /// </summary>
    public static AudioSource ObtenerSonidoPorNombre(string nombre)
    {
        // Buscar en el diccionario el AudioSource por nombre
        if (Sonidos.ContainsKey(nombre))
        {
            return Sonidos[nombre];
        }
        return null;
    }

    public static bool ReproducirBucleConVolumen(string palabraClave, bool esCancionFondo, float volumen)
    {
        // Filtrar los sonidos que contienen la palabra clave
        var sonidosFiltrados = Sonidos.Keys.Where(nombre => nombre.Contains(palabraClave)).ToList();

        // Verificar si hay sonidos que coinciden
        if (sonidosFiltrados.Count > 0)
        {
            // Seleccionar un sonido al azar
            string sonidoAleatorio = sonidosFiltrados[Random.Range(0, sonidosFiltrados.Count)];

            // Obtener el AudioSource correspondiente
            AudioSource audioSource = Sonidos[sonidoAleatorio];

            // Si es una canción de fondo, configurar el AudioSource para que esté en bucle
            if (esCancionFondo)
            {
                audioSource.loop = true; // Activar el bucle
            }

            // Ajustar el volumen del audio
            audioSource.volume = Mathf.Clamp(volumen, 0f, 1f); // Asegurarse de que el volumen esté entre 0 y 1

            // Reproducir el audio en bucle
            audioSource.Play();  // Usamos Play() en lugar de PlayOneShot para mantener la canción en bucle

            //Debug.Log($"Reproduciendo audio en bucle: {sonidoAleatorio} con volumen: {volumen}");
            return true; // Indicar éxito
        }
        else
        {
            Debug.LogError($"No se encontró ningún audio que contenga la palabra clave: {palabraClave}");
            return false; // Indicar fallo
        }
    }



    // Clase auxiliar para ejecutar coroutines desde métodos estáticos
    private class CoroutineHelper : MonoBehaviour
    {
        public static CoroutineHelper Instance;

        private void Awake()
        {
            // Asegurarse de que solo haya una instancia de CoroutineHelper
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    // Método estático para reproducir audio escalonadamente
    public static void ReproducirEscalonadamente(string palabraClave, float intervalo)
    {
        // Filtrar los sonidos que contienen la palabra clave
        var sonidosFiltrados = Sonidos.Keys.Where(nombre => nombre.Contains(palabraClave)).ToList();

        // Verificar si hay sonidos que coinciden
        if (sonidosFiltrados.Count > 0)
        {
            // Seleccionar un sonido al azar
            string sonidoAleatorio = sonidosFiltrados[Random.Range(0, sonidosFiltrados.Count)];

            // Obtener el AudioSource correspondiente
            AudioSource audioSource = Sonidos[sonidoAleatorio];

            // Iniciar la reproducción escalonada llamando a la Coroutine del helper
            if (CoroutineHelper.Instance != null)
            {
                CoroutineHelper.Instance.StartCoroutine(ReproducirEscalonadamenteCoroutine(audioSource, intervalo));
            }
        }
        else
        {
            Debug.LogError($"No se encontró ningún audio que contenga la palabra clave: {palabraClave}");
        }
    }

    // Coroutine para reproducir el audio escalonadamente
    private static IEnumerator ReproducirEscalonadamenteCoroutine(AudioSource audioSource, float intervalo)
    {
        while (!audioSource.isPlaying)
        {
            audioSource.PlayOneShot(audioSource.clip);
            yield return new WaitForSeconds(intervalo);
        }
    }


}
