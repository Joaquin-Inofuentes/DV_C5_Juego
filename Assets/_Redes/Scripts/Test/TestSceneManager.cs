using UnityEngine;
using UnityEngine.UI;

namespace Redes.Test
{
    /// <summary>
    /// Manages the test scene HUD: kill counter, debug panel, controls legend.
    /// </summary>
    public class TestSceneManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _killCounterText;
        [SerializeField] private Text _controlsLegendText;
        [SerializeField] private Text _eventLog;

        [Header("Scene References")]
        [SerializeField] private DummyEnemy _dummy;

        private int _kills;
        private string _logBuffer = "";

        private void Start()
        {
            Application.logMessageReceived += OnLogReceived;

            // Subscribe to all DummyEnemy instances in the scene
            var dummies = Object.FindObjectsByType<DummyEnemy>(FindObjectsSortMode.None);
            foreach (var dummy in dummies)
            {
                dummy.OnDummyKilled += HandleKill;
            }

            UpdateKillUI();

            if (_controlsLegendText != null)
            {
                _controlsLegendText.text =
                    "CONTROLES:\n" +
                    "P1 (WASD + Mouse):\n" +
                    "  WASD → Moverse\n" +
                    "  LMB  → Disparar\n" +
                    "  SHIFT→ Sprint (1.2x)\n" +
                    "\n" +
                    "P2 (Flechitas):\n" +
                    "  FLECHAS → Moverse\n" +
                    "  ESPACIO → Disparar\n" +
                    "  SHIFT   → Sprint (1.2x)";
            }

            Debug.Log("[TEST][MANAGER] TestSceneManager listo.");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogReceived;
        }

        private void HandleKill(int totalKills)
        {
            _kills = totalKills;
            UpdateKillUI();
            Debug.Log($"[TEST][MANAGER] ¡Kill registrado! Total: {_kills}");
        }

        private void UpdateKillUI()
        {
            if (_killCounterText != null)
                _killCounterText.text = $"KILLS: {_kills}";
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            // Show only [TEST] logs in the on-screen panel for clarity
            if (!condition.Contains("[TEST]")) return;

            string color = type == LogType.Error ? "red" : type == LogType.Warning ? "orange" : "white";
            _logBuffer = $"<color={color}>{condition}</color>\n" + _logBuffer;

            // Cap to avoid overflow
            if (_logBuffer.Length > 2000)
                _logBuffer = _logBuffer.Substring(0, 2000);

            if (_eventLog != null)
                _eventLog.text = _logBuffer;
        }
    }
}
