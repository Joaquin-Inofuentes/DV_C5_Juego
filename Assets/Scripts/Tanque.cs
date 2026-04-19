
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tanque : MonoBehaviour
{
    // Prefabs
    public GameObject cohetePrefab; // Prefab del cohete
    public GameObject punteroPrefab; // Prefab del puntero

    // Variables de movimiento
    public float velocidadMovimiento = 5f;
    public float distanciaDeteccion = 5f;

    // Referencia al jugador
    public Transform jugador;

    // Variables de control de puntero
    private GameObject punteroActual;
    private bool puedeGenerarPuntero = true;
    private bool enModoApuntado = false;
    private bool puedeDisparar = true;
    public Transform OrigenDeDisparo;

    void Start()
    {
        jugador = GameObject.Find("Soldado_Jugador").transform; // Asignar al jugador desde la escena
    }

    void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, jugador.position) > distanciaDeteccion)
        {
            MoverHaciaJugador();
            // Verificamos si el puntero fue destruido y si ya se puede generar un nuevo puntero
            if (punteroActual == null && puedeGenerarPuntero && puedeDisparar)
            {
                //GenerarPuntero();
            }
        }
        else
        {
            if (!enModoApuntado)
            {
                EntrarModoApuntado();
            }
        }
    }

    void MoverHaciaJugador()
    {
        // Mover el tanque hacia el jugador
        transform.position = Vector3.MoveTowards(transform.position, jugador.position, velocidadMovimiento * Time.deltaTime);
    }

    void EntrarModoApuntado()
    {
        // El tanque entra en el modo de apuntado
        enModoApuntado = true;
    }

    void GenerarPuntero()
    {
        // Generamos el puntero en la posición del jugador
        punteroActual = Instantiate(punteroPrefab, jugador.position, Quaternion.identity);
        punteroActual.GetComponent<Puntero_Tanque>().StartCoroutine(punteroActual.GetComponent<Puntero_Tanque>().SeguirJugador());
        puedeGenerarPuntero = false;
    }

    public void Disparar() /// Cambie el nombre
    {
        /// Agregue esta verificacion para q no dispare a muros
        bool PuedeDisparar = VerificaSiAhiUnObstaculoEntreTuyElEnemigo(transform, jugador);

        if (PuedeDisparar == true)
        {
            //Debug.Log("Disparaon");
            /// Solo dispara si es q tiene al enemigo visible
            if (punteroActual != null)
            {
                GameObject cohete = Instantiate(cohetePrefab, OrigenDeDisparo.position, Quaternion.identity);
                cohete.GetComponent<Cohete>().SetObjetivo(punteroActual.transform.position);
            }
        }
    }

    // Método para recibir la notificación de que el puntero ha terminado su tiempo
    public void PunteroDestruido()
    {
        puedeGenerarPuntero = true;
        enModoApuntado = false; // El tanque deja el modo de apuntado
        puedeDisparar = true; // El tanque puede disparar nuevamente
        Disparar(); // Cambie el nombre de generar a disparar
    }

    // Método para evitar disparar constantemente
    public void DesactivarDisparoTemporal()
    {
        puedeDisparar = false;
        StartCoroutine(ReactivarDisparo());
    }

    private IEnumerator ReactivarDisparo()
    {
        yield return new WaitForSeconds(2f); // Espera 2 segundos antes de permitir disparar nuevamente
        puedeDisparar = true;
    }





    // Lista pública para almacenar todos los GameObjects que colisionaron
    public List<GameObject> objetosColisionados = new List<GameObject>();

    // Clase para almacenar el GameObject y su distancia
    [System.Serializable]
    public class ObjetoColisionado
    {
        public GameObject objeto;
        public float distancia;

        public ObjetoColisionado(GameObject obj, float dist)
        {
            objeto = obj;
            distancia = dist;
        }
    }

    // Lista pública que contiene los objetos colisionados con su distancia
    public List<ObjetoColisionado> objetosColisionadosConDistancia = new List<ObjetoColisionado>();


    public bool VerificaSiAhiUnObstaculoEntreTuyElEnemigo(Transform Tanque, Transform soldado)
    {
        // Dirección del rayo desde el tanque hacia el soldado
        Vector2 direccionDelRayo = (soldado.position - Tanque.position).normalized;

        // Dibujar el rayo en la escena para ver si se está lanzando correctamente, de color blanco
        //Debug.DrawRay(Tanque.position, direccionDelRayo * 10f, Color.white, 2f); // El rayo durará 2 segundos

        // Realizamos el raycast sin omitir capas (detecta todo)
        RaycastHit2D[] hits = Physics2D.RaycastAll(Tanque.position, direccionDelRayo, Mathf.Infinity);

        // Variables para almacenar el muro y el jugador más cercano
        GameObject muroMasCercano = null;
        GameObject jugadorMasCercano = null;
        float distanciaMuro = Mathf.Infinity;
        float distanciaJugador = Mathf.Infinity;

        // Verificar si encontramos alguna colisión
        if (hits.Length > 0)
        {

            // Iterar sobre las colisiones detectadas
            foreach (RaycastHit2D hit in hits)
            {
                // Obtener el nombre del objeto colisionado
                string nombreObjeto = hit.transform.name;


                // Obtener la capa del objeto colisionado
                string nombreCapa = LayerMask.LayerToName(hit.collider.gameObject.layer);

                // Si es un muro, comprobar si es el más cercano
                if (nombreCapa == "Muros")
                {
                    //Debug.Log(3);
                    float distancia = Vector2.Distance(transform.position, hit.transform.position);
                    if (distancia < distanciaMuro)
                    {
                        distanciaMuro = distancia;
                        muroMasCercano = hit.collider.gameObject;
                    }
                }
                // Si es un jugador, comprobar si es el más cercano
                else if (nombreCapa == "Jugador")
                {
                    float distancia = Vector2.Distance(transform.position, hit.transform.position);
                    if (distancia < distanciaJugador)
                    {
                        //Debug.Log(4);
                        distanciaJugador = distancia;
                        jugadorMasCercano = hit.collider.gameObject;
                    }
                }
            }

            Debug.Log("Distancia al jugador más cercano: " + distanciaJugador);
            // Imprimir las distancias al jugador y al muro
            Debug.Log("Distancia al muro más cercano: " + distanciaMuro);

            // Verificar cuál es más cercano y devolver true o false
            if (jugadorMasCercano != null && distanciaJugador < distanciaMuro)
            {
                // El jugador más cercano es más cercano que el muro
                //Debug.Log("¡Logrado! El jugador está más cerca que el muro.");
                return true;
            }
            else
            {
                // El muro más cercano está más cerca que el jugador
                //Debug.Log("No logrado. El muro está más cerca.");
                return false;
            }
        }

        // Si no hay colisiones, se devuelve false
        return false;
    }









}