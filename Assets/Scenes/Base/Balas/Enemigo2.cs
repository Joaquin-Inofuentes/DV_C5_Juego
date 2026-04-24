using UnityEngine;

public class Enemigo2 : MonoBehaviour, IDaniable
{
    public int vida = 100;

    public void RecibirDano(int cantidad)
    {
        vida -= cantidad;
        Debug.Log("Vida restante: " + vida);
        if (vida <= 0) Destroy(gameObject);
    }
}