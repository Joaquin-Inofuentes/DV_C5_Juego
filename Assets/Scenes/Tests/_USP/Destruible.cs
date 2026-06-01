using UnityEngine;
using Game.Squad;


public class Destruible : MonoBehaviour, IDaniable
{
    [Header("Configuración de Salud")]
    public float vida = 100f;
    public float maxVida = 100f;

    public void RecibirDano(int cantidad, GameObject atacante)
    {
        // 1. Si el objeto es una UNIDAD (Soldado o Enemigo unificado)
        UnitController unit = GetComponent<UnitController>();
        if (unit != null)
        {
            unit.RecibirDano(cantidad, atacante);
            return;
        }

        // 2. Si es un objeto del escenario (Cajas, barriles, etc.)
        vida -= cantidad;

        if (vida <= 0)
        {
            MorirObjeto();
        }
    }

    private void MorirObjeto()
    {
        // Lógica para destruir objetos que no son personas
        Destroy(gameObject);
    }
}