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
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
    }

    private void Update()
    {
        // 1. Chequeo de nulidad de la instancia
        if (TopDownGameManager.Instance == null) return;

        // 2. Chequeo de validez del objeto de red
        if (!TopDownGameManager.Instance.Object.IsValid) return;

        var runner = TopDownGameManager.Instance.Runner;
        if (runner == null || !runner.IsRunning) return;

        NetworkObject localPlayerObj = runner.GetPlayerObject(runner.LocalPlayer);

        if (localPlayerObj != null && localPlayerObj.IsValid)
        {
            if (localPlayerObj.TryGetComponent(out TopDownPlayerHealth hp))
                healthText.text = $"VIDA: {hp.Health}";

            if (localPlayerObj.TryGetComponent(out TopDownPlayer player))
                ammoText.text = $"BALAS: {player.Ammo}";
        }
    }

    private void HandleGameEnded(PlayerRef winner)
    {
        var runner = TopDownGameManager.Instance.Runner;

        // Obtenemos el ID real para el log
        int myID = runner.LocalPlayer.PlayerId;
        int winnerID = winner.PlayerId;

        Debug.Log($"[UI] Fin del Juego. Mi ID: {myID} | Ganador ID: {winnerID}");

        // Comparación directa de PlayerRef (es lo más seguro en Fusion)
        if (winner == runner.LocalPlayer)
        {
            winPanel?.SetActive(true);
            losePanel?.SetActive(false);
        }
        else
        {
            losePanel?.SetActive(true);
            winPanel?.SetActive(false);
        }
    }
}