using UnityEngine;
using Game.Squad;

public class Disparador : MonoBehaviour
{
    public float dañoBala = 10f;
    public float velocidadBala = 25f;

    public void Disparar()
    {
        Bala b = BalaPool.Instance.GetBala();
        if (b == null)
        {
            Debug.LogError("[Disparador] ¡Falta el prefab de Bala en BalaPool o BalaPool no está instanciado!");
            return;
        }

        b.transform.position = transform.position;
        b.transform.rotation = transform.rotation;

        b.daño = dañoBala;
        b.velocidad = velocidadBala;
        b.dueño = transform.root.gameObject;
    }
}
