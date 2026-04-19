using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Controla las acciones del personaje principal en un entorno 2D, como disparar, recargar, curarse, moverse hacia adelante y atrás, y rotar.
/// También maneja el seguimiento de la cámara.
/// Debe estar asociado al objeto principal del jugador.
/// </summary>
public class ControladorPersonaje : MonoBehaviour
{
    // Variables de disparo
    public GameObject soldado_jugador;

    // Variables de granadas
    public Transform origenGranada;   // Origen desde donde se lanzan las granadas
    public GameObject prefabGranada;  // Prefab de la granada que se lanzará

    // Variables de movimiento y rotación
    public float velocidadMovimiento = 5f;
    public float velocidadRotacion = 100f;

    // Referencia a la clase que maneja la información del personaje
    private InformacionPersonaje infoPersonaje;

    // Referencia al Rigidbody2D
    private Rigidbody2D rb2D;

    // Variables de seguimiento de cámara
    public Camera camaraPrincipal;    // Referencia a la cámara principal

    // Cursor
    public GameObject Cursor;

    float VelocidadDeMovimiento = 1;

    public float DesplazamientoSalto = 5;

    private void Start()
    {
        // Buscar el componente InformacionPersonaje en el mismo GameObject
        infoPersonaje = GetComponent<InformacionPersonaje>();
        rb2D = GetComponent<Rigidbody2D>();

        // Validación de las referencias asignadas
        VerificarReferencias();
    }

    private void FixedUpdate()
    {
        ManejarMovimientoPorTeclado();

        if (Input.GetKey(KeyCode.Escape ))
        {
            Debug.Log("salto hecho");
            Saltar();
        }

    }


    public void Saltar()
    {
        transform.GetComponent<Rigidbody>().AddForce(transform.forward*10, ForceMode.Impulse);
    }

    private void Update()
    {
        ManejarUsoKitMedico(); // Llama al método para manejar el uso del kit médico
        //ManejarRotacion();

        // Agregado 23/09/24. Para q rote con el mouse y no con A o D
        ManejarRotacionPorMouse();
        QueMireAlCursor();

        //ManejarDisparo();
        ManejarCuracion();
        ManejarLanzamientoGranada();
    }

    private void QueMireAlCursor()
    {
        transform.up = (Cursor.transform.position - transform.position).normalized;
    }

    // Maneja la rotacion del jugador para q siempre apunte hacia el cursor del mouse
    void ManejarRotacionPorMouse()
    {
        // Definimos la variable de posicion en base a desde donde se apunta desde la pantalla
        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);


        // Corrije lo de q no tome el eje z
        Vector3 fixedMousePos = new Vector3(MousePosition.x, MousePosition.y, 0);
        Cursor.transform.position = fixedMousePos;
    }


    private void LateUpdate()
    {
        // Sincronizar la posición del Rigidbody2D con el Transform al final del frame
        if (rb2D != null)
        {
            rb2D.position = transform.position;
            rb2D.rotation = transform.eulerAngles.z;

            Vector3 targetpos = soldado_jugador.transform.position;

            camaraPrincipal.transform.position = new Vector3(targetpos.x, targetpos.y, camaraPrincipal.transform.position.z);
        }
    }

    private void VerificarReferencias()
    {
        if (origenGranada == null) Debug.LogError("OrigenGranada no está asignado en el Inspector.");
        if (prefabGranada == null) Debug.LogError("PrefabGranada no está asignado en el Inspector.");
        if (infoPersonaje == null) Debug.LogError("No se encontró el script InformacionPersonaje en el objeto del jugador.");
        if (rb2D == null) Debug.LogError("No se encontró el componente Rigidbody2D en el objeto del jugador.");
        if (camaraPrincipal == null) Debug.LogError("La referencia a la cámara principal no está asignada en el Inspector.");
    }

    private void ManejarMovimientoPorTeclado()
    {

        // Obtener entrada horizontal y vertical
        float movimientoHorizontal = Input.GetAxis("Horizontal"); // A y D
        float movimientoVertical = Input.GetAxis("Vertical"); // W y S

        // Crear un vector de movimiento basado en la entrada (solo X)
        Vector3 desplazamiento = new Vector3(movimientoHorizontal, movimientoVertical, 0) * velocidadMovimiento * Time.deltaTime;

        // Aplicar el movimiento al objeto y mantener la posición Z
        transform.position = new Vector3(transform.position.x + desplazamiento.x, transform.position.y + desplazamiento.y, 0);
    }

    public void Desplazarse(string Direccion, float Velocidad)
    {
        BD_Audios.ReproducirBucleConVolumen("Caminar", true, 0.5f);
        switch (Direccion)
        {
            case "Adelante":
                transform.Translate(new Vector3(0, 1, 0) * Velocidad * Time.deltaTime);
                break;
            case "Derecha":
                transform.Translate(new Vector3(1, 0, 0) * Velocidad * Time.deltaTime);
                break;
            case "Izquierda":
                transform.Translate(new Vector3(0, -1, 0) * Velocidad * Time.deltaTime);
                break;
            case "Atras":
                transform.Translate(new Vector3(-1, 0, 0) * Velocidad * Time.deltaTime);
                break;
        }
    }

    private void ManejarRotacion()
    {
        float rotacionHorizontal = Input.GetAxisRaw("Horizontal");
        transform.Rotate(Vector3.forward, -rotacionHorizontal * velocidadRotacion * Time.deltaTime);
    }

    private void ManejarCuracion()
    {
        if (Input.GetKeyDown(KeyCode.Q) && infoPersonaje != null)
        {
            infoPersonaje.UsarKitMedico();
        }
    }

    private void ManejarLanzamientoGranada()
    {
        if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (infoPersonaje.granadas > 0)
            {
                CrearGranada();
                infoPersonaje.granadas--;
                infoPersonaje.ActualizarUI();
            }
        }
    }


    private void ManejarUsoKitMedico()
    {
        // Verifica si se presionó la tecla "T" y si hay un kit médico disponible
        if (Input.GetKeyDown(KeyCode.T) && infoPersonaje != null)
        {
            // Usar un kit médico para curar al máximo
            infoPersonaje.CurarAlMaximo();
        }
    }


    private void CrearGranada()
    {
        // Instanciar la granada en el origen de lanzamiento
        Instantiate(prefabGranada, origenGranada.position, origenGranada.rotation);
    }















}
