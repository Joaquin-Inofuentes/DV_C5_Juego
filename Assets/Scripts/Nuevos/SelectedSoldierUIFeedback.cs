using USP.Entities;
using UnityEngine;
using UnityEngine.UI;
using Game.Squad;

/// <summary>
/// Hace parpadear un RawImage del Canvas de selección al recibir un impacto el soldado seleccionado.
/// Utiliza el EventBus y un timer simple (sin corrutinas).
/// </summary>
public class SelectedSoldierUIFeedback : MonoBehaviour
{
    [Tooltip("La RawImage del canvas que parpadeará al recibir un impacto.")]
    public RawImage imagenFeedback;

    [Tooltip("Color de parpadeo.")]
    public Color colorParpadeo = Color.red;

    private float timerParpadeo = 0f;
    private Color colorOriginal = Color.white;

    private void OnEnable()
    {
        SquadEventBus.OnSoldierDamaged += HandleSoldierDamaged;
        if (imagenFeedback != null)
        {
            colorOriginal = imagenFeedback.color;
        }
    }

    private void OnDisable()
    {
        SquadEventBus.OnSoldierDamaged -= HandleSoldierDamaged;
    }

    private void HandleSoldierDamaged(SoldierController soldier, float damage)
    {
        // Si el soldado herido es el líder seleccionado actual (GlobalData.liderActual)
        if (soldier != null && soldier == GlobalData.liderActual)
        {
            timerParpadeo = 0.3f;
        }
    }

    private void Update()
    {
        if (imagenFeedback == null) return;

        if (timerParpadeo > 0f)
        {
            timerParpadeo -= Time.deltaTime;
            // Alternar color cada 0.05 segundos
            bool toggle = Mathf.Repeat(timerParpadeo, 0.1f) > 0.05f;
            imagenFeedback.color = toggle ? colorParpadeo : colorOriginal;
        }
        else if (imagenFeedback.color != colorOriginal)
        {
            imagenFeedback.color = colorOriginal;
        }
    }
}

