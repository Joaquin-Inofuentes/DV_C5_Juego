using Game.Squad;
using UnityEngine;

public class GlobalHUD : MonoBehaviour
{
    public LeaderManager leaderManager;
    public Vector2 offset = new Vector2(0, 45);
    public float anchoBarra = 60f;
    public float altoBarra = 6f;

    // Desactivado para que solo se use la barra de vida limpia de UnitView
    void OnGUI()
    {
    }
}