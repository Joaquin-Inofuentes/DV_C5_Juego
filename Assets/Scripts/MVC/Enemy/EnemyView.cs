using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Componente de Vista del enemigo. Se encarga de dibujar la GUI de vida y efectos visuales de daño.
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        public EnemyModel model;
        public SpriteRenderer spriteRenderer;

        [Header("Configuración Barra OnGUI")]
        public float anchoBarra = 60f;
        public float altoBarra = 6f;
        public Vector2 offset = new Vector2(0, 45);

        private void Start()
        {
            if (model == null) model = GetComponent<EnemyModel>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (model == null) Debug.LogError($"[EnemyView] ¡Falta EnemyModel! En el objeto '{name}'");
            if (spriteRenderer == null) Debug.LogError($"[EnemyView] ¡Falta SpriteRenderer! En el objeto '{name}'");
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
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }

        private void OnGUI()
        {
            if (model == null || model.IsDead || Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z <= 0) return;

            float x = screenPos.x - (anchoBarra / 2);
            float y = Screen.height - screenPos.y + offset.y;

            // Barra de fondo (Gris)
            GUI.color = Color.gray;
            GUI.DrawTexture(new Rect(x, y, anchoBarra, altoBarra), Texture2D.whiteTexture);

            // Barra de salud (Verde)
            float porcentaje = model.vidaActual / model.vidaMaxima;
            GUI.color = porcentaje > 0.3f ? Color.green : Color.red;
            GUI.DrawTexture(new Rect(x, y, anchoBarra * porcentaje, altoBarra), Texture2D.whiteTexture);

            // Texto de vida debajo
            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            GUI.skin.label.fontSize = 9;
            GUI.Label(new Rect(x, y + altoBarra, anchoBarra, 20), $"{(int)model.vidaActual}/{(int)model.vidaMaxima}");

            // Restaurar color original de GUI
            GUI.color = Color.white;
        }
    }
}
