using UnityEngine;
using System;

/// <summary>
/// Administrador central de todas las entradas físicas (teclado y mouse) del juego.
/// Mantiene propiedades públicas legibles por otros sistemas y ofrece eventos con opción de depuración en consola.
/// </summary>
public class GEN_Inputs : MonoBehaviour
{
    public static GEN_Inputs Instance { get; private set; }

    [Header("Configuración de Cámara")]
    [Tooltip("Cámara utilizada para calcular la posición del mouse en el mundo. Si es nula, se usará Camera.main.")]
    public Camera camaraReferencia;

    [Header("Configuración de Debug")]
    [Tooltip("Si se activa, imprimirá logs de advertencia en consola.")]
    public bool debugVerbose = true;

    public Vector2 MovimientoInput   { get; private set; }
    public Vector3 MouseWorldPosition { get; private set; }
    public bool DisparoSostenido     { get; private set; }
    public bool DisparoPresionado    { get; private set; }
    public bool OrdenPresionada      { get; private set; }
    public bool RegresarAFormacion   { get; private set; }
    public bool RavivicionInput      { get; private set; } // Barra espaciadora sostenida (revivir)
    public bool HealPresionado       { get; private set; } // Barra espaciadora pulsada (médico cura)

    public event Action<bool> OnCycleLeader;  // false = Q (izq), true = E (der)
    public event Action<int>  OnOrdenDirecta; // 0/1/2 → teclas 1/2/3
    public void Awake()
    {
        OnEnable();
    }
    private void OnEnable()
    {
        Instance = this;

        if (camaraReferencia == null)
        {
            camaraReferencia = Camera.main;
            if (camaraReferencia == null && debugVerbose)
                Debug.LogWarning("[GEN_Inputs] No hay Cámara de Referencia asignada y Camera.main es null. Asignala en el Inspector.");
        }
    }

    private void Update()
    {
        // 1. Movimiento WASD / Flechas
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        MovimientoInput = new Vector2(moveX, moveY).normalized;

        // 2. Posición del Mouse en el Mundo
        if (camaraReferencia == null) camaraReferencia = Camera.main;
        if (camaraReferencia != null)
        {
            Vector3 worldPos = camaraReferencia.ScreenToWorldPoint(Input.mousePosition);
            MouseWorldPosition = new Vector3(worldPos.x, worldPos.y, 0f);
        }
        else
        {
            MouseWorldPosition = Vector3.zero;
        }

        // 3. Click Izquierdo (Disparo)
        DisparoSostenido  = Input.GetMouseButton(0);
        DisparoPresionado = Input.GetMouseButtonDown(0);

        // 4. Click Derecho (Órdenes)
        OrdenPresionada = Input.GetMouseButtonDown(1);

        // 5. Ciclado de Líder: Q (izquierda/-1) / E (derecha/+1)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (debugVerbose && (OnCycleLeader == null || OnCycleLeader.GetInvocationList().Length == 0))
                Debug.LogWarning("[GEN_Inputs] OnCycleLeader sin suscriptores (Q). LeaderManager puede no estar activo.");
            OnCycleLeader?.Invoke(true);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (debugVerbose && (OnCycleLeader == null || OnCycleLeader.GetInvocationList().Length == 0))
                Debug.LogWarning("[GEN_Inputs] OnCycleLeader sin suscriptores (E). LeaderManager puede no estar activo.");
            OnCycleLeader?.Invoke(false);
        }

        // 6. Órdenes Directas: 1 / 2 / 3
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) TriggerOrdenDirecta(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) TriggerOrdenDirecta(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) TriggerOrdenDirecta(2);

        // 7. Regresar a formación: Z
        RegresarAFormacion = Input.GetKeyDown(KeyCode.Z);

        // 8. Barra espaciadora
        RavivicionInput = Input.GetKey(KeyCode.Space);
        HealPresionado  = Input.GetKeyDown(KeyCode.Space);
    }

    private void TriggerOrdenDirecta(int index)
    {
        if (debugVerbose && (OnOrdenDirecta == null || OnOrdenDirecta.GetInvocationList().Length == 0))
            Debug.LogWarning($"[GEN_Inputs] OnOrdenDirecta sin suscriptores (tecla {index + 1}). UnitCommander puede no estar activo.");
        OnOrdenDirecta?.Invoke(index);
    }
}
