using UnityEngine;
using Game.Squad;

public class InteractableAmmo : MonoBehaviour
{
    [Header("Configuración Munición")]
    public int ammoAmount = 300;
    [Tooltip("Si es true, recargará el arma al máximo ignorando ammoAmount.")]
    public bool reloadToMax = true;

    private void Start()
    {
        Debug.Log($"<color=cyan>[Municion]</color> Pickup de munición creado en la escena: {name}. Validando componentes...");
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"[Municion ERROR] ¡El objeto {name} NO tiene un Collider2D! Debes añadirle uno (por ejemplo, BoxCollider2D) en el Inspector para que funcione.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[Municion WARNING] El Collider2D en {name} no tiene marcada la casilla 'Is Trigger'. Funcionará, pero las unidades chocarán físicamente con él.");
        }
        else
        {
            Debug.Log($"<color=cyan>[Municion]</color> {name} configurado exitosamente. ¡Listo para recargar soldados!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"<color=cyan>[Municion]</color> Inicio OnTriggerEnter2D. Objeto colisionado: {other.name}");
        UnitController unit = other.GetComponent<UnitController>();
        
        // Verifica si colisionó con una unidad viva
        if (unit != null && unit.model != null && !unit.model.IsDead)
        {
            if (reloadToMax)
            {
                unit.model.ammoActual = unit.model.ammoMax;
            }
            else
            {
                unit.model.ammoActual += ammoAmount;
                if (unit.model.ammoActual > unit.model.ammoMax)
                {
                    unit.model.ammoActual = unit.model.ammoMax;
                }
            }

            // Muestra mensaje de recarga
            if (unit.view != null)
            {
                unit.view.ShowSpeech("¡Recargado!", 1.5f);
            }

            Debug.Log($"<color=cyan>[Municion]</color> {unit.name} recogió munición. Balas: {unit.model.ammoActual}/{unit.model.ammoMax}");

            // Destruye el pickup
            Destroy(gameObject);
        }
        Debug.Log($"<color=cyan>[Municion]</color> Fin OnTriggerEnter2D.");
    }
}
