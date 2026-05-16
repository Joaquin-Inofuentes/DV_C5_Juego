using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalInputs : MonoBehaviour
{
    private NetworkInputData _networkInputData;

    //crear variable que chequea si salto
    //crear variable que chequea si disparo

    
    void Start()
    {
        //creo y guardo un NetworkInputData
    }

    void Update()
    {
        //setear movimiento a mi network input

        //chequear si salto
        
        //chequear si disparo
    }

    public NetworkInputData GetLocalInputs()
    {
        //seteo el disparo de mi network input
        //reinicio el disparo local;

        //seteo el salto de mi network input
        //reinicio el salto local
        
        return _networkInputData;
    }
}
