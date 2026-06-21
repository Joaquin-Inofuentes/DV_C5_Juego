using UnityEngine;

namespace DebugSystem
{
    public class Bullet : MonoBehaviour
    {
        public float Speed = 15f;
        public float Damage = 25f;
        public float Lifetime = 3f;

        private Vector3 direction;
        private int shooterActorID;
        private int projectileID;
        private float spawnTime;

        private static int nextProjectileID = 1;

        public void Setup(int shooterID, Vector3 dir)
        {
            shooterActorID = shooterID;
            direction = dir;
            projectileID = nextProjectileID++;
            spawnTime = Time.time;

            EventBus.TriggerProjectileCreated(projectileID, shooterActorID, "PistolBullet", transform.position.x, transform.position.y, transform.position.z);
        }

        private void Update()
        {
            transform.position += direction * Speed * Time.deltaTime;

            if (Time.time - spawnTime >= Lifetime)
            {
                Recycle();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            PlayerModel other = collision.GetComponent<PlayerModel>();
            if (other != null)
            {
                if (other.ActorID != shooterActorID)
                {
                    if (EffectPool.Instance != null) EffectPool.Instance.SpawnImpact(transform.position, Color.red);
                    EventBus.TriggerImpactClient(projectileID, other.ActorID, Damage);
                    EventBus.TriggerImpactHost(projectileID, other.ActorID, Damage);
                    other.ApplyDamage(Damage, shooterActorID);
                    Recycle();
                }
            }
            else if (collision.CompareTag("Obstacle"))
            {
                if (EffectPool.Instance != null) EffectPool.Instance.SpawnImpact(transform.position, Color.gray);
                Recycle();
            }
        }

        private void Recycle()
        {
            BulletPool.Instance.ReturnToPool(this);
        }
    }
}
