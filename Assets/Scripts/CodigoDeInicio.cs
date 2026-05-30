using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodigoDeInicio : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hola mundo");
        BD_Audios.ReproducirAudioUnaVez("Musica de intro");
        Time.timeScale = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
