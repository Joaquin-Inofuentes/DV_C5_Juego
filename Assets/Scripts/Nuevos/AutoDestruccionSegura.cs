using UnityEngine;

public class AutoDestruccionSegura : MonoBehaviour
{
    public float tiempoDeVidaEfecto = 1.0f;
    private static bool appIsQuitting = false;

    private void Start()
    {
        // Se destruye solo después del tiempo asignado
        Destroy(gameObject, tiempoDeVidaEfecto);
    }

    private void OnApplicationQuit()
    {
        appIsQuitting = true;
    }

    private void OnDestroy()
    {
        // Aquí podrías poner lógica extra, 
        // pero la protección appIsQuitting evita que 
        // este objeto intente spawnear otros al cerrarse.
        if (appIsQuitting) return;

        //Debug.Log("Efecto destruido limpiamente");
    }
}