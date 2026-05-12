using UnityEngine;

public class GlobalHUD : MonoBehaviour
{
    public LeaderManager leaderManager;
    public Vector2 offset = new Vector2(0, 45); // Un poco más de offset para que quepa el texto
    public float anchoBarra = 60f;
    public float altoBarra = 6f;

    void OnGUI()
    {
        if (leaderManager == null) return;

        foreach (var soldado in leaderManager.unidades)
        {
            if (soldado == null) continue;

            // 1. Obtener Vida y FSM
            Destruible v = soldado.GetComponent<Destruible>();
            FSMController fsm = soldado.GetComponent<FSMController>();

            if (v == null) continue;

            // 2. Posicionamiento
            Vector3 screenPos = Camera.main.WorldToScreenPoint(soldado.transform.position);
            if (screenPos.z <= 0) continue;

            float x = screenPos.x - (anchoBarra / 2);
            float y = Screen.height - screenPos.y + offset.y;

            // --- DIBUJAR BARRA DE VIDA ---
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x, y, anchoBarra, altoBarra), Texture2D.whiteTexture);

            float porcentaje = (float)v.vida / v.maxVida;
            GUI.color = porcentaje > 0.3f ? Color.green : Color.red;
            GUI.DrawTexture(new Rect(x, y, anchoBarra * porcentaje, altoBarra), Texture2D.whiteTexture);

            // --- DIBUJAR TEXTO DE VIDA (XX/XX) ---
            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            GUI.skin.label.fontSize = 10;
            GUI.Label(new Rect(x, y + altoBarra, anchoBarra, 20), $"{(int)v.vida}/{(int)v.maxVida}");

            // --- DIBUJAR ESTADO DEL FSM ---
            if (fsm != null)
            {
                string textoEstado = fsm.currentState.ToString();

                // Cambiar color según el estado para que sea más visual
                GUI.color = GetColorByState(fsm.currentState);

                GUI.skin.label.fontStyle = FontStyle.Bold;
                // Lo dibujamos un poco más abajo (y + altoBarra + 12)
                GUI.Label(new Rect(x - 20, y + altoBarra + 12, anchoBarra + 40, 20), textoEstado);
            }
            Municion muni = soldado.GetComponent<Municion>();

            // --- DIBUJAR MUNICIÓN ---
            if (muni != null)
            {
                GUI.color = Color.yellow;
                GUI.skin.label.fontSize = 9;
                // Dibujamos debajo del estado (y + altoBarra + 25)
                GUI.Label(new Rect(x, y + altoBarra + 25, anchoBarra, 20), $"AMMO: {muni.balasActuales}");
            }
        }
    }

    // Método auxiliar para dar colores a los estados
    Color GetColorByState(FSMController.State state)
    {
        switch (state)
        {
            case FSMController.State.Atacar: return Color.red;
            case FSMController.State.Liderando: return Color.cyan;
            case FSMController.State.IrAObjetivo: return Color.yellow;
            case FSMController.State.IrAFormacion: return Color.gray;
            case FSMController.State.Esperando: return Color.white;
            default: return Color.white;
        }
    }
}