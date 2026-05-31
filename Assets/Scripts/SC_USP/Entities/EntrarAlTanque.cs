// Clase principal que controla la entrada y salida del tanque en 2D
// Se encarga de gestionar la interacción entre el soldado y el tanque (entrar y salir)
using UnityEngine;

public class EntrarAlTanque : MonoBehaviour
{
    // Referencias al controlador del tanque y al controlador del soldado
    public ControladorTanque controladorTanque;
    public GameObject soldado; // El soldado será un objeto GameObject

    // Estado actual, si el soldado está dentro o fuera del tanque
    private bool estaDentroDelTanque = false;

    // Mensajes de GUI
    // Variable para controlar si el soldado está dentro del tanque
    private bool dentroDelTanque = false;

    public float SizeTamañoNativo;

    private void Start()
    {
        SizeTamañoNativo = Camera.main.orthographicSize;
    }

    void Update()
    {
        //Debug.Log(Vector2.Distance(transform.position, soldado.transform.position));
        // Verificar si el soldado está cerca del tanque y presiona la tecla "E"
        if (Vector2.Distance(transform.position, soldado.transform.position) < 5f)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Si el soldado no está dentro, entra al tanque
                if (!estaDentroDelTanque)
                {
                    Debug.Log("Entraste");
                    AccionDeEntrar();
                    //controladorTanque.controlActivo = true;
                }
                // Si el soldado está dentro, sale del tanque
                else
                {
                    //controladorTanque.controlActivo = false;
                    Debug.Log("Saliste");
                    SalirDelTanque();
                }
            }
        }


        if(controladorTanque.controlActivo == true)
        {
            soldado.transform.localPosition = new Vector3(0, 0, 0);
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -25);
            soldado.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
    }

    // Método para hacer que el soldado entre al tanque
    private void AccionDeEntrar()
    {
        if (dentroDelTanque)
        {
            // Salir del tanque
            dentroDelTanque = false;

            // Reactivar los componentes del soldado
            CambiarEstadoComponentesSoldado(true);

            // Separar al soldado del tanque y ubicarlo cerca
            soldado.transform.SetParent(null); // Quitar al soldado como hijo del tanque
            soldado.transform.position = new Vector3(transform.position.x + 2, transform.position.y, transform.position.z); // Ubicar a 2 unidades en X
            controladorTanque.DesactivarControlTanque();
            Camera.main.orthographicSize = SizeTamañoNativo;
        }
        else
        {
            
            // Entrar al tanque
            dentroDelTanque = true;

            // Desactivar los componentes del soldado
            CambiarEstadoComponentesSoldado(false);

            // Hacer al soldado hijo del objeto que porta este script
            soldado.transform.SetParent(transform);

            // Centrar al soldado dentro del tanque
            soldado.transform.localPosition = Vector3.zero; // Ubicar en el centro del tanque
            controladorTanque.ActivarControlTanque();
            Camera.main.orthographicSize = 10f;
        }
    }

    private void CambiarEstadoComponentesSoldado(bool estado)
    {
        // Cambia el estado activo de los componentes del soldado
        foreach (var componente in soldado.GetComponents<Component>())
        {
            if (!(componente is Transform)) // No desactivar el Transform
            {
                // Comprobar si es un componente desactivable
                if (componente is Behaviour)
                    ((Behaviour)componente).enabled = estado;
                else if (componente is Renderer)
                    ((Renderer)componente).enabled = estado;
                // Agregar más casos si es necesario
            }
        }
    }
    // Método para hacer que el soldado salga del tanque
    public void SalirDelTanque()
    {
        soldado.SetActive(true); // Reactivar al soldado
        controladorTanque.DesactivarControlTanque(); // Desactivar el controlador del tanque
        soldado.transform.SetParent(null); // El soldado deja de ser hijo del tanque
        ActivarTodosLosComponentes(soldado);


        // Colocar al soldado a 2 unidades en el eje X y mantener su posición en Y y Z
        soldado.transform.position = new Vector3(controladorTanque.transform.position.x + 2f, soldado.transform.position.y, soldado.transform.position.z);

        estaDentroDelTanque = false;
        Camera.main.orthographicSize = SizeTamañoNativo;

    }


    /// <summary>
    /// Activa todos los componentes de un GameObject.
    /// </summary>
    /// <param name="objeto">El GameObject cuyo componentes serán activados.</param>
    public void ActivarTodosLosComponentes(GameObject objeto)
    {
        // Activar el GameObject en sí
        objeto.SetActive(true);

        // Obtiene todos los componentes de tipo Component en el GameObject
        Component[] componentes = objeto.GetComponents<Component>();

        // Recorre cada componente y activa su estado
        foreach (var componente in componentes)
        {
            // Activar el componente si es un Behaviour (como MonoBehaviour, Collider, etc.)
            if (componente is Behaviour comportamiento)
            {
                comportamiento.enabled = true;
            }
        }
    }



    // Mostrar el mensaje en la pantalla utilizando GUI
    private void OnGUI()
    {
        // Mostrar el mensaje en el centro de la pantalla
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.alignment = TextAnchor.MiddleCenter;
    }
}
