using UnityEngine;

/// <summary>
/// Gestiona el comportamiento de un proyectil, como aplicar fuerza inicial y da�o.
/// Este script debe estar asociado a cada objeto de proyectil (bala o granada).
/// </summary>
public class Proyectil : MonoBehaviour
{
    public float velocidadInicial = 20f; // Velocidad inicial del proyectil
    public float dano;

    private Rigidbody2D rb2D;

    public bool esBomba; // Determina si el proyectil es una bomba o un disparo

    // Asignacion de arma segun numeracion
    public Texture2D[] Armas; // Textura de las armas
    public Texture2D ImagenDeArmaActual; // Imagen del arma actual

    public float tiempoVida;

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();

        // Aplicar fuerza inicial al proyectil
        if (rb2D != null)
        {
            rb2D.velocity = transform.right * velocidadInicial;
        }

        // Destruir el proyectil despu�s de tiempoVida segundos
        Invoke("DestruirBala", tiempoVida); // Llama a DestruirBala despu�s de tiempoVida segundos
    }


    // Asigna el arma segun su numero del inventario o lugar
    public void AsignarArma(int NumeroDeArmaAsignar)
    {
        ImagenDeArmaActual = Armas[NumeroDeArmaAsignar];
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log(collision.gameObject.name);

        // Deshabilitar el colisionador del proyectil
        gameObject.GetComponent<Collider2D>().enabled = false;

        // Si el proyectil es una bomba, realiza una expansi�n para da�ar a m�ltiples objetivos
        if (esBomba)
        {
            // Da�o en �rea (AOE)
            Collider2D[] objetosCercanos = Physics2D.OverlapCircleAll(transform.position, 2f); // Radio de da�o de 2 unidades
            foreach (Collider2D objeto in objetosCercanos)
            {
                //Debug.Log("Aqui mira "+ objeto.tag);
                if (objeto.CompareTag("Jugadores") || objeto.CompareTag("Soldado_Enemigo"))
                {
                    InformacionPersonaje infoPersonaje = objeto.GetComponent<InformacionPersonaje>();
                    if (infoPersonaje != null)
                    {
                        infoPersonaje.RecibirDanio(dano);
                    }

                    Enemigo InfoDelEnemigo = objeto.GetComponent<Enemigo>();
                    if (InfoDelEnemigo != null)
                    {
                        InfoDelEnemigo.RecibirDanio(dano);
                    }
                }
            }

            // Destruir la bomba despu�s de la explosi�n
            DestruirBala();
        }
        else
        {
            // Comportamiento para proyectil regular
            if (collision.gameObject.CompareTag("Jugadores") || collision.gameObject.CompareTag("Soldado_Enemigo"))
            {
                InformacionPersonaje infoPersonaje = collision.gameObject.GetComponent<InformacionPersonaje>();
                if (infoPersonaje != null)
                {
                    infoPersonaje.RecibirDanio(dano);
                }

                Enemigo InfoDelEnemigo = collision.gameObject.GetComponent<Enemigo>();
                if (InfoDelEnemigo != null)
                {
                    InfoDelEnemigo.RecibirDanio(dano);
                }

                // Destruir el proyectil despu�s de aplicar el da�o
                DestruirBala();
            }
            else if (collision.gameObject.CompareTag("Obstaculo"))
            {
                //Debug.Log("Choc� con un obst�culo");
                DestruirBala();
            }
        }
    }


    void DestruirBala()
    {
        // Si es una bomba, aplicar una expansi�n visual 3 veces mayor antes de destruir
        if (esBomba)
        {
            gameObject.transform.localScale = new Vector3(3, 3, 3); // Escala 3 veces mayor para la bomba
        }
        else
        {
            gameObject.transform.localScale = new Vector3(2, 2, 2); // Escala regular para proyectiles normales
        }

        //Debug.Log("Destruyendo proyectil");

        // Invocar la destrucci�n del objeto despu�s de 0.1 segundos
        Invoke("DesaparecerBala", 0.1f);
    }


    private void OnDrawGizmosSelected()
    {
        // Dibujar el radio de da�o de la bomba en modo de edici�n para visualizar su alcance
        if (esBomba)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 2f); // Radio de 2 unidades para la bomba
        }
    }


    void DesaparecerBala()
    {
        Destroy(gameObject);
    }
}
