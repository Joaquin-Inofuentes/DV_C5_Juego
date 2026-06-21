using System.Collections.Generic;
using UnityEngine;

namespace DebugSystem
{
    public class BulletPool : MonoBehaviour
    {
        public static BulletPool Instance { get; private set; }

        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private int initialSize = 10;

        private Queue<Bullet> pool = new Queue<Bullet>();

        private void Awake()
        {
            Instance = this;
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialSize; i++)
            {
                Bullet b = Instantiate(bulletPrefab, transform);
                b.gameObject.SetActive(false);
                pool.Enqueue(b);
            }
        }

        public Bullet Get()
        {
            Bullet b;
            if (pool.Count > 0)
            {
                b = pool.Dequeue();
            }
            else
            {
                b = Instantiate(bulletPrefab, transform);
            }
            b.gameObject.SetActive(true);
            return b;
        }

        public void ReturnToPool(Bullet b)
        {
            b.gameObject.SetActive(false);
            b.transform.SetParent(transform);
            pool.Enqueue(b);
        }
    }
}
