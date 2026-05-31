using UnityEngine;
using UnityEngine.SceneManagement;
using Game.MVC;
using USP.Core;
using USP.Services;

namespace USP.Entities
{
    /// <summary>
    /// Controlador principal del jugador (MVC - Controller).
    /// Coordina la entrada física (Input), la actualización del modelo (CharacterModel) y la vista (CharacterView).
    /// Preparado para integración futura con Photon mediante la bandera 'hasInputAuthority'.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
    [Header("Componentes MVC")]
    [Tooltip("Modelo del personaje que almacena estadísticas de velocidad y estados.")]
    public CharacterModel model;

    [Tooltip("Vista del personaje que administra cámaras, cursores e instanciación.")]
    public CharacterView view;

    [Tooltip("Manejador de movimiento físico (por defecto Rigidbody2D).")]
    public Rigidbody2DMovementHandler movementHandler;

    [Header("Configuración de Escenas y Victoria")]
    [Tooltip("Nombre de la escena de victoria a cargar cuando se colisiona con el trigger de Victoria.")]
    public string nombreEscenaVictoria = "MenuDeVictoria";

    [Header("Preparación para Multijugador (Photon)")]
    [Tooltip("Define si este cliente controla a este jugador localmente. En multijugador, esto debe sincronizarse con Object.HasInputAuthority o photonView.IsMine.")]
    public bool hasInputAuthority = true;

    // Componentes de Entrada e Internos
    private ICharacterInput input;
    private Rigidbody2D rb2D;

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        input = new UnityCharacterInput();

        // Autodetectar componentes si están vacíos
        if (model == null) model = GetComponent<CharacterModel>();
        if (view == null) view = GetComponent<CharacterView>();
        if (movementHandler == null) movementHandler = GetComponent<Rigidbody2DMovementHandler>();
    }

    private void Update()
    {
        // Si no tiene autoridad de entrada (es un proxy remoto en red), no procesar inputs locales
        if (!hasInputAuthority) return;

        if (model == null || view == null || input == null) return;

        // 1. Lógica del Mouse (Girar y actualizar cursor)
        Vector3 mouseWorldPos = input.GetMouseWorldPosition(null);
        view.UpdateCursorPosition(mouseWorldPos);
        
        if (movementHandler != null)
        {
            movementHandler.RotateTowards(transform, mouseWorldPos);
        }

        // 2. Curación manual (Q y T)
        if (input.GetHealInput())
        {
            model.UsarKitMedico();
        }
        if (input.GetFullHealInput())
        {
            model.CurarAlMaximo();
        }

        // 3. Lanzar granadas (G o click derecho)
        if (input.GetThrowGrenadeInput() && model.HasGrenades())
        {
            view.SpawnGrenade();
            model.ConsumeGrenade();
        }

        // 4. Lógica de PowerUps (Adrenalina - tecla P)
        ManejarUsoPowerUps();
    }

    private void FixedUpdate()
    {
        // Si es un clon de red remoto, la física y posición se sincronizan por red, no por entrada local
        if (!hasInputAuthority) return;

        if (model == null || movementHandler == null || input == null) return;

        // Movimiento por teclado
        Vector2 moveInput = input.GetMovementInput();
        if (moveInput.sqrMagnitude > 0.001f)
        {
            float velocidadActual = model.ObtenerVelocidadActual();
            movementHandler.Move(transform, rb2D, moveInput, velocidadActual);
        }

        // Salto (Escape)
        if (input.GetJumpInput())
        {
            movementHandler.Jump(transform, rb2D, model.desplazamientoSalto);
        }
    }

    private void LateUpdate()
    {
        // En multijugador, la cámara y el cursor solo deben actualizarse para el jugador local
        if (!hasInputAuthority) return;
        if (view == null) return;

        // Sincronizar física del Rigidbody2D
        if (rb2D != null)
        {
            rb2D.position = transform.position;
            rb2D.rotation = transform.eulerAngles.z;
        }

        // Cámara de seguimiento
        view.SyncCameraPosition();
    }

    private void ManejarUsoPowerUps()
    {
        // Activar adrenalina si presionamos P
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (model.infoPersonaje != null && model.infoPersonaje.adrenalina > 0)
            {
                if (!model.activadoPowerUp1)
                {
                    model.activadoPowerUp1 = true;
                    model.powerUpsActivos++;
                }
                else if (!model.activadoPowerUp2)
                {
                    model.activadoPowerUp2 = true;
                    model.powerUpsActivos++;
                }

                model.infoPersonaje.adrenalina--;
                model.infoPersonaje.ActualizarUI();
                BD_Audios.ReproducirConSolapamiento("PowerUp");
            }
        }

        // Cuenta regresiva de duración de Power-Ups
        if (model.activadoPowerUp1)
        {
            model.duracionPowerUp1 += Time.deltaTime;
            if (model.duracionPowerUp1 >= model.duracionPorVelocidadAumentada)
            {
                model.activadoPowerUp1 = false;
                model.duracionPowerUp1 = 0f;
                model.powerUpsActivos--;
            }
        }

        if (model.activadoPowerUp2)
        {
            model.duracionPowerUp2 += Time.deltaTime;
            if (model.duracionPowerUp2 >= model.duracionPorVelocidadAumentada)
            {
                model.activadoPowerUp2 = false;
                model.duracionPowerUp2 = 0f;
                model.powerUpsActivos--;
            }
        }
    }

    // Detector de colisión (Victoria)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo el jugador local puede gatillar el cambio de escena de victoria
        if (!hasInputAuthority) return;

        if (other.gameObject.name.Contains("Victoria"))
        {
            SceneManager.LoadScene(nombreEscenaVictoria);
        }
    }
}
}
