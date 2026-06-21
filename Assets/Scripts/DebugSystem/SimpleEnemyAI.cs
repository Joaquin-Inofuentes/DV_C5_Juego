using UnityEngine;

namespace DebugSystem
{
    public class SimpleEnemyAI : MonoBehaviour
    {
        private PlayerModel target;
        private PlayerModel selfModel;
        private Weapon weapon;
        private PlayerView view;

        public float AttackRange = 8f;
        public float ShootCooldown = 2f;
        private float lastShootTime;

        private void Awake()
        {
            selfModel = GetComponent<PlayerModel>();
            weapon = GetComponentInChildren<Weapon>();
            view = GetComponent<PlayerView>();
        }

        private void Start()
        {
            // Find the player
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                target = pc.GetComponent<PlayerModel>();
            }
        }

        private void Update()
        {
            if (target == null || selfModel.CurrentHP <= 0 || target.CurrentHP <= 0) return;

            // Aim at target
            Vector3 direction = (target.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Distance check
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= AttackRange)
            {
                if (Time.time - lastShootTime >= ShootCooldown)
                {
                    lastShootTime = Time.time;
                    EventBus.TriggerInputReceived("EnemyShoot", Time.frameCount);
                    if (weapon != null && weapon.TryShoot())
                    {
                        if (view != null)
                        {
                            view.PlayShootVFX(weapon.transform.position);
                        }
                    }
                }
            }
        }
    }
}
