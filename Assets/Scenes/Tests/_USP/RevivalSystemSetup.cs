using UnityEngine;
using Game.Squad;

/// <summary>
/// Script helper para configurar automáticamente el sistema de revivimiento.
/// Agrega este script a cada soldado en el inspector y presiona el botón de Setup.
/// </summary>
public class RevivalSystemSetup : MonoBehaviour
{
#if UNITY_EDITOR

    [Header("SETUP AUTOMÁTICO DEL SISTEMA DE REVIVIMIENTO")]
    [TextArea(3, 5)]
    public string instructions = "1. Crea un hijo 'Vivo' con el sprite de soldado vivo\n2. Crea un hijo 'Caido' con el sprite de soldado caído (desactivado)\n3. Presiona 'Setup Revivimiento' abajo";

    [SerializeField] private bool hasAliveGameObject = false;
    [SerializeField] private bool hasDownGameObject = false;
    [SerializeField] private bool hasRevivalBarView = false;

    private void OnValidate()
    {
        // Verificar componentes necesarios
        Transform aliveTransform = transform.Find("Vivo");
        Transform downTransform = transform.Find("Caido");
        RevivalBarView revivalBar = GetComponent<RevivalBarView>();

        hasAliveGameObject = aliveTransform != null;
        hasDownGameObject = downTransform != null;
        hasRevivalBarView = revivalBar != null;
    }

#endif

    /// <summary>Método que se ejecuta desde el Inspector (botón)</summary>
    public void SetupRevivalSystem()
    {
#if UNITY_EDITOR
        Debug.Log($"<color=cyan>[RevivalSystemSetup]</color> Configurando sistema de revivimiento para {gameObject.name}...");

        // 1. Asegurar que exista GameObject "Vivo"
        Transform aliveGO = transform.Find("Vivo");
        if (aliveGO == null)
        {
            GameObject vivo = new GameObject("Vivo");
            vivo.transform.SetParent(transform);
            vivo.transform.localPosition = Vector3.zero;
            aliveGO = vivo.transform;
            Debug.Log($"<color=green>[Setup]</color> Creado GameObject 'Vivo'");
        }

        // 2. Asegurar que exista GameObject "Caido" (desactivado)
        Transform downGO = transform.Find("Caido");
        if (downGO == null)
        {
            GameObject caido = new GameObject("Caido");
            caido.transform.SetParent(transform);
            caido.transform.localPosition = Vector3.zero;
            caido.SetActive(false);
            downGO = caido.transform;
            Debug.Log($"<color=green>[Setup]</color> Creado GameObject 'Caido' (desactivado)");
        }

        // 3. Asegurar que exista RevivalBarView
        RevivalBarView revivalBar = GetComponent<RevivalBarView>();
        if (revivalBar == null)
        {
            revivalBar = gameObject.AddComponent<RevivalBarView>();
            Debug.Log($"<color=green>[Setup]</color> Agregado componente RevivalBarView");
        }

        // 4. Asegurar que exista UnitController_Revival (si no está como partial, validar)
        UnitController controller = GetComponent<UnitController>();
        if (controller == null)
        {
            Debug.LogError($"<color=red>[Setup ERROR]</color> No se encontró UnitController en {gameObject.name}");
            return;
        }

        // 5. Asegurar que UnitModel existe
        UnitModel model = GetComponent<UnitModel>();
        if (model == null)
        {
            Debug.LogError($"<color=red>[Setup ERROR]</color> No se encontró UnitModel en {gameObject.name}");
            return;
        }

        // Verificar configuración de UnitModel
        if (model.reviveHealthPercent <= 0 || model.reviveHealthPercent > 1)
        {
            model.reviveHealthPercent = 0.3f;
            Debug.Log($"<color=yellow>[Setup WARNING]</color> reviveHealthPercent ajustado a 0.3 (30%)");
        }

        Debug.Log($"<color=green>[SetupRevivalSystem]</color> ✅ Sistema de revivimiento configurado exitosamente para {gameObject.name}");
#endif
    }
}
