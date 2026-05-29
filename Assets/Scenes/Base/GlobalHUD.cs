using UnityEngine;
using Game.Squad;

public class GlobalHUD : MonoBehaviour
{
    public LeaderManager leaderManager;
    public Vector2 offset = new Vector2(0, 45);
    public float anchoBarra = 60f;
    public float altoBarra = 6f;

    void OnGUI()
    {
        if (leaderManager == null) return;

        foreach (var soldado in leaderManager.unidades)
        {
            if (soldado == null) continue;

            SoldierModel m = soldado.model;
            if (m == null) continue;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(soldado.transform.position);
            if (screenPos.z <= 0) continue;

            float x = screenPos.x - (anchoBarra / 2);
            float y = Screen.height - screenPos.y + offset.y;

            // --- DIBUJAR BARRA DE VIDA ---
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x, y, anchoBarra, altoBarra), Texture2D.whiteTexture);

            float porcentaje = m.vidaActual / m.vidaMaxima;
            GUI.color = porcentaje > 0.3f ? Color.green : Color.red;
            GUI.DrawTexture(new Rect(x, y, anchoBarra * porcentaje, altoBarra), Texture2D.whiteTexture);

            // --- DIBUJAR TEXTO DE VIDA ---
            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            GUI.skin.label.fontSize = 10;
            GUI.Label(new Rect(x, y + altoBarra, anchoBarra, 20), $"{(int)m.vidaActual}/{(int)m.vidaMaxima}");

            // --- DIBUJAR ESTADO DEL FSM ---
            string textoEstado = soldado.currentState.ToString();
            GUI.color = GetColorByState(soldado.currentState);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(x - 20, y + altoBarra + 12, anchoBarra + 40, 20), textoEstado);

            // --- DIBUJAR MUNICIÓN ---
            GUI.color = Color.yellow;
            GUI.skin.label.fontSize = 9;
            GUI.Label(new Rect(x, y + altoBarra + 25, anchoBarra, 20), $"AMMO: {m.balasActuales}");
        }
    }

    Color GetColorByState(SoldierController.State state)
    {
        switch (state)
        {
            case SoldierController.State.Atacar: return Color.red;
            case SoldierController.State.Liderando: return Color.cyan;
            case SoldierController.State.IrAObjetivo: return Color.yellow;
            case SoldierController.State.IrAFormacion: return Color.gray;
            case SoldierController.State.Esperando: return Color.white;
            default: return Color.white;
        }
    }
}
