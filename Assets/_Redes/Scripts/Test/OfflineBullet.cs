using UnityEngine;

namespace Redes.Test
{
    /// <summary>
    /// Component added at runtime in the offline test scene to animate
    /// the bullet prefab's movement and handle offline trigger hits on DummyEnemy.
    /// </summary>
    public class OfflineBullet : MonoBehaviour
    {
        public float Speed = 25f;
        public int Damage = 25;
        public float LifeTime = 2f;

        public bool IsEnemyBullet = false;

        private void Start()
        {
            Destroy(gameObject, LifeTime);
        }

        private void Update()
        {
            transform.position += transform.forward * Speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsEnemyBullet)
            {
                // Enemy bullet hits the player
                var player = other.GetComponentInParent<OfflinePlayerTester>();
                if (player != null)
                {
                    player.TakeDamage(Damage);
                    Destroy(gameObject);
                }
            }
            else
            {
                // Player bullet hits the dummy enemy
                var dummy = other.GetComponentInParent<DummyEnemy>();
                if (dummy != null)
                {
                    if (dummy.IsAlive)
                    {
                        dummy.TakeDamage(Damage);
                        
                        // Trigger CustomCursorView hit visual effect
                        var cursorView = Object.FindAnyObjectByType<Views.CustomCursorView>();
                        if (cursorView != null)
                        {
                            cursorView.TriggerHit();
                        }

                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
