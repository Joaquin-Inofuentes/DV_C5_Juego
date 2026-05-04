using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrearYDestruir : MonoBehaviour
{
    public GameObject PrefabDeSangre;
    private static bool appIsQuitting = false;

    // Se ejecuta cuando cierras el juego o sales del modo Play
    private void OnApplicationQuit()
    {
        appIsQuitting = true;
    }

    public void OnDestroy()
    {
        // Solo instanciamos la sangre si el juego sigue corriendo 
        // y si el objeto no es nulo
        if (!appIsQuitting && PrefabDeSangre != null)
        {
            Vector3 NuevaPosicion = transform.position + new Vector3(0, 0, 2);
            GameObject Sangre = Instantiate(PrefabDeSangre, NuevaPosicion, Quaternion.identity);
            Destroy(Sangre, 2.0f); // Destruye la sangre después de 2 segundos
        }
    }
}