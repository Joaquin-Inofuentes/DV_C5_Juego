// Clase que controla el tanque
// Gestiona el movimiento y disparo del tanque cuando el soldado est� dentro
using UnityEngine;
using TMPro;  // Aseg�rate de incluir la librer�a de TextMeshPro


public class ControladorTanque : MonoBehaviour
{
    public float velocidadTanque = 10f;
    public float rotacionTanque = 50f;
    public GameObject proyectilPrefab;
    public Transform puntoDisparo;

    public bool controlActivo = false;

    public Rigidbody2D rb2D; // Referencia al Rigidbody2D del tanque
    public GameObject InterfazDelSoldado;
    public GameObject InterfazDelTanque;


    // Velocidad actual del tanque
    public float velocidadActual = 0;

    // Configuraci�n del tanque
    public float velocidadMaxima = 5f; // Velocidad m�xima permitida
    public float aceleracionTanque = 2f; // Qu� tan r�pido acelera el tanque
    public float desaceleracionTanque = 3f; // Qu� tan r�pido desacelera el tanque

    public float friccionPiso = 1.5f; // Tasa de fricci�n en el piso


    public GameObject canon;

    public TextMeshProUGUI texto;


    private void Start()
    {
        // Obt�n el Rigidbody2D del objeto al inicio
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void MoverTanque()
    {
        // Entrada del jugador
        float entradaMovimiento = Input.GetAxis("Vertical"); // Entrada hacia adelante o atr�s
        float entradaRotacion = Input.GetAxis("Horizontal"); // Entrada para girar

        // Aceleraci�n progresiva del tanque
        if (entradaMovimiento != 0)
        {
            // Si el tanque est� movi�ndose hacia adelante pero se presiona "W" para retroceder, o viceversa
            if ((entradaMovimiento > 0 && velocidadActual > 0) || (entradaMovimiento < 0 && velocidadActual < 0))
            {
                // Aumentar la velocidad en la direcci�n actual
                velocidadActual += entradaMovimiento * aceleracionTanque * Time.deltaTime;
            }
            else
            {
                // Reducir la velocidad a cero si la entrada es opuesta a la direcci�n del movimiento
                velocidadActual = 0;
                // Retroceder el tanque en la direcci�n opuesta
                velocidadActual = entradaMovimiento * velocidadMaxima;
            }
        }
        else
        {
            // Aplicar fricci�n cuando no hay entrada
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0, friccionPiso * Time.deltaTime);
        }

        // Limitar la velocidad m�xima
        velocidadActual = Mathf.Clamp(velocidadActual, -velocidadMaxima, velocidadMaxima);

        // Movimiento del tanque usando Rigidbody2D
        Vector2 movimiento = transform.right * velocidadActual * Time.deltaTime; // Direcci�n local (X en 2D)
        rb2D.MovePosition(rb2D.position - movimiento);

        // Rotaci�n del tanque (cuando hay movimiento)
        if (Mathf.Abs(velocidadActual) > 0.1f) // Evitar rotar si el tanque est� casi detenido
        {
            float rotacion = entradaRotacion * rotacionTanque * Time.deltaTime;
            transform.Rotate(new Vector3(0, 0, -rotacion));
        }
    }


    private void ApuntarCanon()
    {
        if (!gameObject.activeSelf) return; // Verificar si el tanque est� activo

        // Obtener la posici�n del cursor en el mundo
        Vector3 posicionCursor = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Calcular la direcci�n hacia el cursor desde el ca��n
        Vector3 direccion = posicionCursor - canon.transform.position;

        // Convertir la direcci�n a un �ngulo en el eje Z
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        // Aplicar la rotaci�n al ca��n en su eje Z local
        canon.transform.rotation = Quaternion.Euler(0, 0, angulo-180);
    }




    void FixedUpdate()
    {
        // Solo el jugador dentro del tanque puede controlar el tanque
        if (controlActivo)
        {
            MoverTanque();
            DispararProyectil();
            ApuntarCanon();
        }

        if(vida <= 0)
        {
            Destroy(gameObject, 1f);
            gameObject.GetComponent<EntrarAlTanque>().SalirDelTanque();
        }

    }

    // M�todo para activar el control del tanque
    public void ActivarControlTanque()
    {
        controlActivo = true;
        InterfazDelSoldado.SetActive(false);
        InterfazDelTanque.SetActive(true);
        texto.text = vida.ToString();

    }

    // M�todo para desactivar el control del tanque
    public void DesactivarControlTanque()
    {
        controlActivo = false;
        InterfazDelSoldado.SetActive(true);
        InterfazDelTanque.SetActive(false);
    }

    public int vida = 100;

    // M�todo que maneja el disparo del proyectil
    private void DispararProyectil()
    {
        if (Input.GetMouseButtonDown(0)) // Bot�n izquierdo del rat�n
        {
            // Crear el proyectil en el punto de disparo
            GameObject bala = Instantiate(proyectilPrefab, puntoDisparo.position, puntoDisparo.rotation);
            
            Proyectil p = bala.GetComponent<Proyectil>();
            if (p != null)
            {
                p.velocidadInicial = 25;
                p.owner = gameObject;
                p.dano = 20;
            }

            Bala b = bala.GetComponent<Bala>();
            if (b != null)
            {
                b.velocidad = 25;
                b.dueno = gameObject;
                b.damage = 20;
            }
            BD_Audios.ReproducirConSolapamiento("DisparoDeCanon 1");
        }
    }


    void OnCollisionEnter2D(Collision2D collision)
    {
        // Verificar si el proyectil ha colisionado con la capa "Muros"
        if (collision.gameObject.layer == LayerMask.NameToLayer("Muros"))
        {
            // Detener la velocidad del proyectil
            Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.velocity = Vector2.zero; // Establecer la velocidad a 0 para detener el proyectil
                velocidadActual = 0;
            }
        }

        // Verificar si el proyectil ha colisionado con la capa "Muros"
        if (collision.gameObject.layer == LayerMask.NameToLayer("Bala"))
        {
            vida -= 20;
            texto.text = vida.ToString();
        }

        // Aqu� ir�a el resto de la l�gica para manejar otras colisiones...
    }


}
