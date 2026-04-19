using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambioDeLider : MonoBehaviour
{
    public List<GameObject> Soldados = new List<GameObject>();
    public GameObject LiderActual;
    public Transform CabezaDeSeguidores;
    public Action<GameObject> OnLiderCambiado;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            CambiarLider(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CambiarLider(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            CambiarLider(3);
        }
    }
    public void CambiarLider(int numeroDeSoldado)
    {
        if(numeroDeSoldado > Soldados.Count || numeroDeSoldado < 1)
        {
            Debug.Log("Numero de soldado no valido");
            return;
        }
        LiderActual = Soldados[numeroDeSoldado - 1];
        CabezaDeSeguidores.position = LiderActual.transform.position;
        OnLiderCambiado?.Invoke(LiderActual);
        Debug.Log("Lider cambiado a: " + LiderActual.name);
    }
}
