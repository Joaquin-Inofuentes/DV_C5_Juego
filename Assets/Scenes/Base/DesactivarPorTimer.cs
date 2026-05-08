using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesactivarPorTimer : MonoBehaviour
{
    public void OnEnable()
    {
        Invoke(nameof(Desactivar), 2f);
    }

    // Update is called once per frame
    void Desactivar()
    {
        gameObject.SetActive(false);
    }
}
