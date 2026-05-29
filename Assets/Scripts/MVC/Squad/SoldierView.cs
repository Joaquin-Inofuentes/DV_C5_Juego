using UnityEngine;

namespace Game.Squad
{
    /// <summary>
    /// Gestiona la presentación visual de cada soldado (animaciones, luces de disparo, selección).
    /// </summary>
    public class SoldierView : MonoBehaviour
    {
        [Header("Efectos Visuales")]
        [Tooltip("Indicador visual que se activa bajo el soldado cuando es seleccionado como Líder.")]
        public GameObject selectionRing;

        [Tooltip("SpriteRenderer de la unidad para efectos visuales (ej. flashes).")]
        public SpriteRenderer spriteRenderer;

        public void SetSelectionActive(bool isActive)
        {
            if (selectionRing != null)
            {
                selectionRing.SetActive(isActive);
            }
        }

        public void TriggerDamageFeedback()
        {
            if (spriteRenderer != null)
            {
                StopAllCoroutines();
                StartCoroutine(DamageFlashRoutine());
            }
        }

        private System.Collections.IEnumerator DamageFlashRoutine()
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }

        public void PlaySound(string soundName)
        {
            BD_Audios.ReproducirConSolapamiento(soundName);
        }
    }
}
