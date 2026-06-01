using Game.Squad;
using UnityEngine;

public class GlobalHUD : MonoBehaviour
{
    public LeaderManager leaderManager;
    public Vector2 offset = new Vector2(0, 45);
    public float anchoBarra = 60f;
    public float altoBarra = 6f;

    void OnGUI()
    {
        if (leaderManager == null) return;

        foreach (var unidad in leaderManager.unidades)
        {
            if (unidad == null || unidad.model == null) continue;

            UnitModel m = unidad.model;
            UnitFSM fsm = unidad.GetComponent<UnitFSM>();

            Vector3 screenPos = Camera.main.WorldToScreenPoint(unidad.transform.position);
            if (screenPos.z <= 0) continue;

            float x = screenPos.x - (anchoBarra / 2);
            float y = Screen.height - screenPos.y + offset.y;

            // --- BARRA DE VIDA ---
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x, y, anchoBarra, altoBarra), Texture2D.whiteTexture);

            float porcentaje = m.healthActual / m.healthMax;
            GUI.color = porcentaje > 0.3f ? Color.green : Color.red;
            GUI.DrawTexture(new Rect(x, y, anchoBarra * porcentaje, altoBarra), Texture2D.whiteTexture);

            // --- TEXTO VIDA ---
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 10;
            GUI.Label(new Rect(x, y + altoBarra, anchoBarra, 20), $"{(int)m.healthActual}/{(int)m.healthMax}");

            // --- ESTADO FSM ---
            if (fsm != null)
            {
                string textoEstado = fsm.currentState.ToString();
                GUI.color = Color.yellow;
                GUI.Label(new Rect(x - 20, y + altoBarra + 12, anchoBarra + 40, 20), textoEstado);
            }

            // --- MUNICIÓN ---
            GUI.color = Color.cyan;
            GUI.Label(new Rect(x, y + altoBarra + 25, anchoBarra, 20), $"AMMO: {m.ammoActual}");
        }
    }
}