using UnityEngine;

public class Disparador : MonoBehaviour
{
    public float dañoBala = 10f;
    public float velocidadBala = 25f; // Velocidad alta para que se sienta el impulso

    public void Disparar()
    {
        Bala b = BalaPool.Instance.GetBala();

        // Posicionar y Orientar
        b.transform.position = transform.position;
        b.transform.rotation = transform.rotation;

        // Pasar datos (IMPORTANTE: Si velocidadBala es 0, no se moverá)
        b.daño = dañoBala;
        b.velocidad = velocidadBala;
        b.dueño = gameObject;

        Debug.Log("<color=green>Se disparó una bala desde:</color> " + gameObject.name);
    }
}