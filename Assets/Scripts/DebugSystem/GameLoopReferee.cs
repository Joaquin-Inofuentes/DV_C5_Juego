using UnityEngine;

namespace DebugSystem
{
    public class GameLoopReferee : MonoBehaviour
    {
        private PlayerModel player;
        private PlayerModel enemy;

        private void Start()
        {
            // Find references to player and enemy models
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                player = pc.GetComponent<PlayerModel>();
            }

            SimpleEnemyAI ai = FindObjectOfType<SimpleEnemyAI>();
            if (ai != null)
            {
                enemy = ai.GetComponent<PlayerModel>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && player != null && enemy != null)
            {
                GameManager.Instance.CheckGameOver(player, enemy);
            }
        }
    }
}
