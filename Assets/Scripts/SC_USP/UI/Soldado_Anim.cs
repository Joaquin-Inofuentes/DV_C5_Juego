namespace USP.UI {
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soldado_Anim : MonoBehaviour
{
    Animator anim;
    public bool EstaCaminando;
    //public GameObject SoldadoReal;


    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Obtener los valores de los ejes de movimiento
        float movimientoHorizontal = Input.GetAxis("Horizontal");
        float movimientoVertical = Input.GetAxis("Vertical");

        // Verificar si el jugador est� intentando moverse
        bool estaMoviendose = Mathf.Abs(movimientoHorizontal) > 0.1f || Mathf.Abs(movimientoVertical) > 0.1f;

        // Activar o desactivar animaci�n seg�n el movimiento
        anim.SetBool("EstaCaminando", estaMoviendose);

        // Acciones adicionales si el jugador est� movi�ndose
        if (estaMoviendose)
        {
            //Debug.Log("El jugador se est� moviendo");
            BD_Audios.ReproducirAudioUnaVez("Caminar");
            //SoldadoReal.SetActive(false);
        }
        else
        {
            //SoldadoReal.SetActive(true);
        }
    }


}

}
