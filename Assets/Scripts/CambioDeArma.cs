using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambioDeArma : MonoBehaviour
{
    public Proyectil proyectil; //Referencia al Script de Proyectil
    public InformacionPersonaje infoPersonaje; //Referencia al Script de InformacionPersonaje
    public CambiarOpacidad cambiarOpacidad;

    public Transform origenDisparo; // Origen desde donde se disparan las balas
    public GameObject prefabBala; // Prefab de la bala que se disparará

    public float[] danoDelArma = new float[] { 10f, 4f, 40f };
    public int[] cantidadDeBalasPorTipoDeArma = new int[] { 15, 40, 4 }; // Cantidad de disparos disponibles para cada tipo de arma
    public int[] cantidadDeBalasActuales = new int[3];
    // Definir recargas como una lista de enteros
    public int[] recargas;

    public string[] tiposDeArmas = new string[] { "Pistola", "Metralleta", "Escopeta" };
    public GameObject[] SimboloDeArmas = new GameObject[] { };


    public int NumeroDeArmaActual;

    public string armaActual;
    public float danoArmaActual;
    public string IndicadorDeBalas;


    public float[] DisparosPorSegundoTipoDeArma = new float[] { 0.1f, 0.01f, 1.5f };
    private float tiempoDesdeElUltimoDisparo;

    void Start()
    {
        SeteoDeBalas();

        if (origenDisparo == null) Debug.LogError("OrigenDisparo no está asignado en el Inspector.");
        if (prefabBala == null) Debug.LogError("PrefabBala no está asignado en el Inspector.");

        NumeroDeArmaActual = 0;
        ActualizarArma();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CambiarArma();
        }


        // Cambia de arma con las teclas del 1 al 9 o al maximo de armas disponibles existentes
        for (int i = 1; i <= 9; i++)
        {
            // Verificar si la tecla correspondiente ha sido presionada
            if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), "Alpha" + i)))
            {
                // Verificar si el número presionado es menor o igual a la cantidad de armas disponibles
                if (i <= tiposDeArmas.Length)
                {
                    NumeroDeArmaActual = i - 1;
                    ActualizarArma();
                }
                else
                {
                    Debug.Log("Número de arma fuera de rango. No hay arma " + i);
                }
                break; // Salir del bucle una vez detectada una tecla
            }
        }


        tiempoDesdeElUltimoDisparo += Time.deltaTime;

        if (PuedeDisparar())
        {
            if (Input.GetButton("Fire1") && cantidadDeBalasActuales[NumeroDeArmaActual] > 0)
            {
                Disparar();

                if (cantidadDeBalasActuales[NumeroDeArmaActual] == 0)
                {
                    Recargar();
                }

                ControlarOpacidadDelEfectoAlDisparar();
            }
        }

        //Condicion en caso de que el metodo "ControlarOpacidadDelEfectoAlDisparar()" se cumpla
        if (tiempoDesdeElUltimoDisparo >= cambiarOpacidad.tiempoDeOpacidad)
        {
            cambiarOpacidad.esTransparente = true;
        }

        IndicadorDeBalas = cantidadDeBalasActuales[NumeroDeArmaActual] + " / " + cantidadDeBalasPorTipoDeArma[NumeroDeArmaActual];
        infoPersonaje.ActualizarUI();

        ManejarRecarga();
    }

    public void CambiarArma()
    {
        NumeroDeArmaActual++;

        if (NumeroDeArmaActual >= tiposDeArmas.Length)
        {
            NumeroDeArmaActual = 0;
        }

        ActualizarArma();
    }

    void ActualizarArma()
    {
        // Asignar el arma actual según el índice
        armaActual = tiposDeArmas[NumeroDeArmaActual];

        // Activar el simbolo de arma correspondiente y desactivar el resto
        for (int i = 0; i < SimboloDeArmas.Length; i++)
        {
            if (i == NumeroDeArmaActual)
            {
                // Activar el símbolo de arma correspondiente
                SimboloDeArmas[i].SetActive(true);
            }
            else
            {
                // Desactivar los símbolos de las otras armas
                SimboloDeArmas[i].SetActive(false);
            }
        }

        // Si hay un proyectil, actualizar el daño
        if (proyectil != null)
        {
            proyectil.dano = danoDelArma[NumeroDeArmaActual];
            danoArmaActual = proyectil.dano;
        }

        // Mostrar en consola el arma actual y su daño
        Debug.Log("El arma actual es " + armaActual + " con daño de " + danoDelArma[NumeroDeArmaActual]);
    }


    private bool PuedeDisparar()
    {
        // Comprueba si ha pasado el cooldown necesario para el arma actual
        return tiempoDesdeElUltimoDisparo >= DisparosPorSegundoTipoDeArma[NumeroDeArmaActual];
    }

    private void Disparar()
    {
        BD_Audios.ReproducirConSolapamiento($"Disparo de {armaActual}");

        // Reinicia el tiempo desde el último disparo
        tiempoDesdeElUltimoDisparo = 0f;

        CrearBala();
        cantidadDeBalasActuales[NumeroDeArmaActual]--;
        infoPersonaje.ActualizarUI();

        //Debug.Log("Disparando " + armaActual + " con daño de " + danoDelArma[NumeroDeArmaActual]);
    }

    private void CrearBala()
    {
        // Instancia la bala en el origen de disparo
        Instantiate(prefabBala, origenDisparo.position, origenDisparo.rotation);
    }

    public void ControlarOpacidadDelEfectoAlDisparar()
    {
        cambiarOpacidad.esTransparente = false;
    }

    public void SeteoDeBalas()
    {
        for (int i = 0; i < cantidadDeBalasPorTipoDeArma.Length; i++)
        {
            cantidadDeBalasActuales[i] = cantidadDeBalasPorTipoDeArma[i];
            Debug.Log("Se han asignado balas " + cantidadDeBalasActuales[i] + " al Arma " + i);
        }
    }

    private void ManejarRecarga()
    {
        if (Input.GetKeyDown(KeyCode.R) && infoPersonaje != null && recargas[NumeroDeArmaActual] > 0)
        {
            if (armaActual == tiposDeArmas[NumeroDeArmaActual])
            {
                BD_Audios.ReproducirConSolapamiento("Recarga de pistola");
                Recargar();
            }
        }
    }

    public void Recargar()
    {
        if (cantidadDeBalasActuales[NumeroDeArmaActual] == cantidadDeBalasPorTipoDeArma[NumeroDeArmaActual])
        {
            Debug.Log("Tienes la máxima cantidad de balas.");
            BD_Audios.ReproducirConSolapamiento("Recarga de Maxima");
        }
        else if (recargas[NumeroDeArmaActual] > 0)
        {
            recargas[NumeroDeArmaActual]--; // Disminuir las recargas disponibles
            cantidadDeBalasActuales[NumeroDeArmaActual] = cantidadDeBalasPorTipoDeArma[NumeroDeArmaActual]; // Recargar
            infoPersonaje.ActualizarUI(); // Actualizar UI
            
            BD_Audios.ReproducirConSolapamiento($"Recarga de {armaActual}");
        }
        else
        {
            Debug.Log("No hay recargas disponibles para el arma actual.");
            BD_Audios.ReproducirConSolapamiento("RecargaFallida");
        }
    }

}