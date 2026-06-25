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
            // Try to find DummyEnemy on target or its parents (e.g. root)
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
