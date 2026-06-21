using System.Collections;
using UnityEngine;

namespace DebugSystem
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EventBus.OnStateTransition += HandleStateTransition;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            EventBus.OnStateTransition -= HandleStateTransition;
            EventBus.ClearAllEvents();
        }

        public GameObject playerPrefab;
        public Transform spawnPoint1;
        public Transform spawnPoint2;

        private IEnumerator Start()
        {
            // Simulate startup / network / gameplay lifecycle logs to show full trace
            EventBus.TriggerGameStarted("1.0.0", 12345);
            EventBus.TriggerTargetFrameRate(60);
            EventBus.TriggerTimeScale(1.0f);
            EventBus.TriggerTestSeed(12345);

            EventBus.TriggerConnectingMaster();
            yield return new WaitForSeconds(0.2f);
            EventBus.TriggerConnectedMaster();
            EventBus.TriggerNetworkRegion("us-east");
            EventBus.TriggerPing(45);
            
            // Note: The rest of the flow is now handled by ScreenManager and LocalNetworkMock
            
            StartCoroutine(PeriodicalSystemSimulations());
        }

        private void HandleStateTransition(string from, string to)
        {
            if (to == "Playing")
            {
                SpawnPlayers();
            }
        }

        private void SpawnPlayers()
        {
            if (playerPrefab == null) return;
            
            // Spawn Player 1
            if (spawnPoint1 != null)
            {
                GameObject p1 = Instantiate(playerPrefab, spawnPoint1.position, Quaternion.identity);
                p1.name = "Player";
                PlayerModel model1 = p1.GetComponent<PlayerModel>();
                if (model1 != null)
                {
                    model1.Initialize(1, LocalNetworkMock.IsHost ? LocalNetworkMock.LocalPlayerName : LocalNetworkMock.GetRoomData().HostName);
                    FloatingHealthBar fhb = p1.GetComponentInChildren<FloatingHealthBar>();
                    if (fhb != null) fhb.Setup(model1, model1.Username);
                }
            }

            // Spawn Player 2
            if (spawnPoint2 != null)
            {
                GameObject p2 = Instantiate(playerPrefab, spawnPoint2.position, Quaternion.identity);
                p2.name = "Player2";
                
                SpriteRenderer sr = p2.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = Color.red;

                PlayerModel model2 = p2.GetComponent<PlayerModel>();
                if (model2 != null)
                {
                    model2.Initialize(2, !LocalNetworkMock.IsHost ? LocalNetworkMock.LocalPlayerName : LocalNetworkMock.GetRoomData().Player2Name);
                    FloatingHealthBar fhb = p2.GetComponentInChildren<FloatingHealthBar>();
                    if (fhb != null) fhb.Setup(model2, model2.Username);
                }
            }
            
            EventBus.TriggerGlobalNotif("Match Started! Defeat the enemy.", "Info");
        }

        private IEnumerator PeriodicalSystemSimulations()
        {
            // Periodic ping updates and state replication logs to showcase network traces
            while (true)
            {
                yield return new WaitForSeconds(5.0f);
                EventBus.TriggerPing(UnityEngine.Random.Range(30, 60));
                EventBus.TriggerSnapshotSent(Time.frameCount, System.Environment.TickCount);
                EventBus.TriggerSnapshotReceived();
            }
        }

        public void CheckGameOver(PlayerModel player1, PlayerModel player2)
        {
            if (player1.CurrentHP <= 0)
            {
                DeclareWinner(2, 1);
            }
            else if (player2.CurrentHP <= 0)
            {
                DeclareWinner(1, 2);
            }
        }

        private void DeclareWinner(int winningActor, int losingActor)
        {
            EventBus.TriggerWinCondition("Elimination");
            EventBus.TriggerTeamWin(winningActor == 1 ? 1 : 2);
            EventBus.TriggerMatchFinished(winningActor == 1 ? "Victory" : "Defeat", winningActor);
            EventBus.TriggerMVP(winningActor, "Kills", 1.0f);
            EventBus.TriggerGlobalNotif($"Player {winningActor} Wins the Match!", "GameOver");
        }
    }
}
