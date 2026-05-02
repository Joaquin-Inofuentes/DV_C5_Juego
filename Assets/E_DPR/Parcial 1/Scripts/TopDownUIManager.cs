using UnityEngine;
using TMPro;
using Fusion;

public class TopDownUIManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Textos TMP")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI ammoText;

    private void OnEnable() => TopDownGameManager.OnGameEndedStatic += HandleGameEnded;
    private void OnDisable() => TopDownGameManager.OnGameEndedStatic -= HandleGameEnded;

    private void Start()
    {
        if (winPanel == null || losePanel == null || healthText == null || ammoText == null)
            Debug.LogError("[CLASS: TopDownUIManager] Faltan referencias en el Inspector.");

        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
    }

    private void Update()
    {
        if (NetworkRunner.Instances.Count == 0) return;
        var runner = NetworkRunner.Instances[0];
        if (runner == null || !runner.IsRunning) return;

        // HUD: Buscamos al jugador local y actualizamos textos
        NetworkObject localPlayer = runner.GetPlayerObject(runner.LocalPlayer);
        if (localPlayer != null)
        {
            var hp = localPlayer.GetComponent<TopDownPlayerHealth>();
            var player = localPlayer.GetComponent<TopDownPlayer>();

            if (hp != null && healthText != null) healthText.text = $"VIDA: {hp.Health}";
            if (player != null && ammoText != null) ammoText.text = $"BALAS: {player.Ammo}";
        }
        else
        {
            if (healthText != null) healthText.text = "VIDA: 0";
        }
    }

    private void HandleGameEnded(PlayerRef winner)
    {
        var runner = NetworkRunner.Instances[0];
        Debug.Log($"[CLASS: TopDownUIManager] Fin del juego. Ganador: P{winner.PlayerId}");

        if (winner == runner.LocalPlayer)
        {
            winPanel?.SetActive(true);
            Debug.Log("<color=green>¡MOSTRANDO PANEL VICTORIA!</color>");
        }
        else
        {
            losePanel?.SetActive(true);
            Debug.Log("<color=red>¡MOSTRANDO PANEL DERROTA!</color>");
        }
    }
}