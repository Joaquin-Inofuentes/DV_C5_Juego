using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesactivarPorTimer : MonoBehaviour
{
    public float tiempoDesactivacion = 2f; // Tiempo en segundos antes de desactivar el objeto
    public void OnEnable()
    {
        Invoke(nameof(Desactivar), tiempoDesactivacion);
    }

    private void OnDisable()
    {
        // Cancelar el Invoke pendiente para que no se acumulen al reactivar el objeto
        CancelInvoke(nameof(Desactivar));
    }

    void Desactivar()
    {
        gameObject.SetActive(false);
    }
}
