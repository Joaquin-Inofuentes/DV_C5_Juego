using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUps : MonoBehaviour
{
    public ControladorPersonaje controladorPersonaje;
    public InformacionPersonaje informacionPersonaje;

    public float duracionPorVelocidadAumentada = 5f;

    public int powerUpsActivos;
    private bool activadoPowerUp1 = false;
    private bool activadoPowerUp2 = false;

    private float duracionPowerUp1 = 0f;
    private float duracionPowerUp2 = 0f;

    void Start()
    {
        powerUpsActivos = 0;
        controladorPersonaje.velocidadMovimiento = 5f; // Velocidad base
    }

    void Update()
    {
        GestionarPowerUps();
        AjustarVelocidad();
    }

    private void GestionarPowerUps()
    {
        if (Input.GetKeyDown(KeyCode.P) && informacionPersonaje.adrenalina > 0) // Cambié a GetKeyDown para evitar repetición constante
        {
            // Activa el primer power-up si no está activado
            if (!activadoPowerUp1)
            {
                activadoPowerUp1 = true;
                powerUpsActivos++;
            }
            // Activa el segundo power-up si el primero ya está activo
            else if (!activadoPowerUp2)
            {
                activadoPowerUp2 = true;
                powerUpsActivos++;
            }

            informacionPersonaje.adrenalina--;
        }

        // Manejar la duración de PowerUp1
        if (activadoPowerUp1)
        {
            duracionPowerUp1 += Time.deltaTime;
            if (duracionPowerUp1 >= duracionPorVelocidadAumentada)
            {
                activadoPowerUp1 = false;
                duracionPowerUp1 = 0f;
                powerUpsActivos--;
            }
        }

        // Manejar la duración de PowerUp2
        if (activadoPowerUp2)
        {
            duracionPowerUp2 += Time.deltaTime;
            if (duracionPowerUp2 >= duracionPorVelocidadAumentada)
            {
                activadoPowerUp2 = false;
                duracionPowerUp2 = 0f;
                powerUpsActivos--;
            }
        }
    }

    private void AjustarVelocidad()
    {

    }
}