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
        // En lugar de Instances[0], usamos el Singleton del Manager que ya tiene el Runner
        if (TopDownGameManager.Instance == null || TopDownGameManager.Instance.Runner == null) return;

        var runner = TopDownGameManager.Instance.Runner;
        NetworkObject localPlayerObj = runner.GetPlayerObject(runner.LocalPlayer);

        if (localPlayerObj != null)
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