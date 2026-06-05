using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BD_Audios : MonoBehaviour
{
    // Diccionario estático público para almacenar los sonidos, clave es el nombre del archivo y el valor es el AudioSource
    public static Dictionary<string, AudioSource> Sonidos = new Dictionary<string, AudioSource>();

    private void OnEnable()
    {
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
        ValidarYLimpiarSonidos();

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

    private static void ValidarYLimpiarSonidos()
    {
        // Si hay entradas en el diccionario y al menos una es nula (destruida por cambio de escena), recargar audios
        bool necesitaRecarga = false;
        foreach (var pair in Sonidos)
        {
            if (pair.Value == null)
            {
                necesitaRecarga = true;
                break;
            }
        }

        if (necesitaRecarga || Sonidos.Count == 0)
        {
            CargarAudios();
        }
    }

    public static void ReproducirAudioUnaVez(string nombre)
    {
        ValidarYLimpiarSonidos();

        List<AudioSource> Audios = Sonidos
            .Where(pair => pair.Key.Contains(nombre) && pair.Value != null)
            .Select(pair => pair.Value)
            .ToList();

        bool SeEstaReproduciendo = true;
        foreach (var audio in Audios)
        {
            if (audio == null) continue;

            if (audio.isPlaying)
            {
                SeEstaReproduciendo = false;
                break;
            }
            if (SeEstaReproduciendo)
            {
                audio.PlayOneShot(audio.clip);
            }
        }
    }

    public static void DetenerAudio(string nombre)
    {
        ValidarYLimpiarSonidos();

        List<AudioSource> Audios = Sonidos
            .Where(pair => pair.Key.Contains(nombre) && pair.Value != null)
            .Select(pair => pair.Value)
            .ToList();

        foreach (var audio in Audios)
        {
            if (audio == null) continue;

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
        ValidarYLimpiarSonidos();

        var sonidosFiltrados = Sonidos.Keys.Where(nombre => nombre.Contains(palabraClave)).ToList();

        if (sonidosFiltrados.Count > 0)
        {
            string sonidoAleatorio = sonidosFiltrados[Random.Range(0, sonidosFiltrados.Count)];
            AudioSource audioSource = Sonidos[sonidoAleatorio];

            if (audioSource != null)
            {
                audioSource.PlayOneShot(audioSource.clip);
                return true;
            }
            return false;
        }
        else
        {
            Debug.LogError($"No se encontró ningún audio que contenga la palabra clave: {palabraClave}");
            return false;
        }
    }

    public static void CargarAudios()
    {
        // Destruir los GameObjects de audio previos para evitar acumulación infinita
        // de objetos DontDestroyOnLoad en cada recarga / cambio de escena.
        foreach (var pair in Sonidos)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }
        Sonidos.Clear();

        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audios");

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                GameObject nuevoSonidoGO = new GameObject($"Audio_{clip.name}");
                DontDestroyOnLoad(nuevoSonidoGO);
                
                AudioSource nuevoAudioSource = nuevoSonidoGO.AddComponent<AudioSource>();
                nuevoAudioSource.clip = clip;

                Sonidos.Add(clip.name, nuevoAudioSource);
            }
        }
    }

    public static AudioSource ObtenerSonidoPorNombre(string nombre)
    {
        ValidarYLimpiarSonidos();

        if (Sonidos.ContainsKey(nombre))
        {
            return Sonidos[nombre];
        }
        return null;
    }

    public static bool ReproducirBucleConVolumen(string palabraClave, bool esCancionFondo, float volumen)
    {
        ValidarYLimpiarSonidos();

        var sonidosFiltrados = Sonidos.Keys.Where(nombre => nombre.Contains(palabraClave)).ToList();

        if (sonidosFiltrados.Count > 0)
        {
            string sonidoAleatorio = sonidosFiltrados[Random.Range(0, sonidosFiltrados.Count)];
            AudioSource audioSource = Sonidos[sonidoAleatorio];

            if (audioSource != null)
            {
                if (esCancionFondo)
                {
                    audioSource.loop = true;
                }

                audioSource.volume = Mathf.Clamp(volumen, 0f, 1f);
                audioSource.Play();
                return true;
            }
            return false;
        }
        else
        {
            Debug.LogError($"No se encontró ningún audio que contenga la palabra clave: {palabraClave}");
            return false;
        }
    }
}

/// <summary>
/// Singleton helper to run coroutines from non-MonoBehaviour classes or static methods.
/// </summary>
public class CoroutineHelper : MonoBehaviour
{
    private static CoroutineHelper _instance;

    public static CoroutineHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CoroutineHelper");
                _instance = go.AddComponent<CoroutineHelper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(gameObject);
    }
}
