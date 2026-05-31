using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesactivarPorTimer : MonoBehaviour
{
    public void OnEnable()
    {
        Invoke(nameof(Desactivar), 2f);
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
