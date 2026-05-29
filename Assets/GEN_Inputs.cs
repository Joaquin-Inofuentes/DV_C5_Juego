using UnityEngine;
using System;

/// <summary>
/// Administrador central de todas las entradas físicas (teclado y mouse) del juego.
/// Mantiene propiedades públicas legibles por otros sistemas y ofrece eventos detallados con opción de depuración en consola.
/// </summary>
public class GEN_Inputs : MonoBehaviour
{
    public static GEN_Inputs Instance { get; private set; }

    [Header("Configuración de Cámara")]
    [Tooltip("Cámara utilizada para calcular la posición del mouse en el mundo. Si es nula, se intentará usar la Cámara Principal (Camera.main).")]
    public Camera camaraReferencia;

    [Header("Configuración de Debug")]
    [Tooltip("Si se activa, imprimirá un log en la consola para cada entrada física detectada.")]
    public bool debugVerbose = true;

    // Propiedades expuestas de Entrada
    public Vector2 MovimientoInput { get; private set; }
    public Vector3 MouseWorldPosition { get; private set; }
    public bool DisparoSostenido { get; private set; }
    public bool DisparoPresionado { get; private set; }
    public bool OrdenPresionada { get; private set; }
    public bool RegresarAFormacion { get; private set; }

    // Eventos específicos para acciones discretas
    public event Action<bool> OnCycleLeader; // false = izquierda (Q), true = derecha (E)
    public event Action<int> OnOrdenDirecta; // 0, 1, 2 asignado a teclas 1, 2, 3

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-detectar cámara de referencia
        if (camaraReferencia == null)
        {
            camaraReferencia = Camera.main;
            if (camaraReferencia == null)
            {
                Debug.LogWarning("[GEN_Inputs] No se ha asignado una Cámara de Referencia y no se encontró una 'MainCamera'. Asigna una en el Inspector para evitar fallos de cálculo de apuntado.");
            }
        }
    }

    private void Update()
    {
        // 1. Movimiento WASD / Flechas
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        MovimientoInput = new Vector2(moveX, moveY).normalized;

        // 2. Apuntado / Posición del Mouse en el Mundo
        if (camaraReferencia == null) camaraReferencia = Camera.main;
        if (camaraReferencia != null)
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = camaraReferencia.ScreenToWorldPoint(mousePos);
            MouseWorldPosition = new Vector3(worldPos.x, worldPos.y, 0f);
        }
        else
        {
            MouseWorldPosition = Vector3.zero;
        }

        // 3. Click Izquierdo (Disparo)
        DisparoSostenido = Input.GetMouseButton(0);
        DisparoPresionado = Input.GetMouseButtonDown(0);
        if (debugVerbose && DisparoPresionado)
        {
            Debug.Log("[GEN_Inputs] Click Izquierdo (Disparo) presionado.");
        }

        // 4. Click Derecho (Ordenes)
        OrdenPresionada = Input.GetMouseButtonDown(1);
        if (debugVerbose && OrdenPresionada)
        {
            Debug.Log("[GEN_Inputs] Click Derecho (Orden) presionado.");
        }

        // 5. Ciclado de Líder: Q (izquierda) y E (derecha)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (debugVerbose) Debug.Log("[GEN_Inputs] Tecla Q presionada (Ciclado Líder Izquierda).");
            OnCycleLeader?.Invoke(false);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (debugVerbose) Debug.Log("[GEN_Inputs] Tecla E presionada (Ciclado Líder Derecha).");
            OnCycleLeader?.Invoke(true);
        }

        // 6. Órdenes Directas con teclas 1, 2, 3
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) TriggerOrdenDirecta(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) TriggerOrdenDirecta(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) TriggerOrdenDirecta(2);

        // 7. Regresar a formación (Z)
        RegresarAFormacion = Input.GetKeyDown(KeyCode.Z);
        if (debugVerbose && RegresarAFormacion)
        {
            Debug.Log("[GEN_Inputs] Tecla Z (Regresar a formación) presionada.");
        }
    }

    private void TriggerOrdenDirecta(int index)
    {
        if (debugVerbose) Debug.Log($"[GEN_Inputs] Tecla Orden Directa (1, 2, 3) presionada para slot: {index + 1}");
        OnOrdenDirecta?.Invoke(index);
    }
}
