using System.Collections.Generic;
using UnityEngine;

namespace Redes.Views
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private ParticleSystem _hitVfxPrefab;       // Blood burst (Red)
        [SerializeField] private ParticleSystem _muzzleFlashPrefab;  // Muzzle flash (Green)
        [SerializeField] private ParticleSystem _sparkVfxPrefab;      // Obstacle spark (White)

        private List<ParticleSystem> _muzzlePool = new List<ParticleSystem>();
        private List<ParticleSystem> _hitPool = new List<ParticleSystem>();
        private List<ParticleSystem> _sparkPool = new List<ParticleSystem>();

        private const int PoolSize = 20;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePools();
        }

        private void InitializePools()
        {
            // Clear existing if any
            _muzzlePool.Clear();
            _hitPool.Clear();
            _sparkPool.Clear();

            // Populate muzzle flash pool
            if (_muzzleFlashPrefab != null)
            {
                for (int i = 0; i < PoolSize; i++)
                {
                    var inst = Instantiate(_muzzleFlashPrefab, transform);
                    inst.gameObject.AddComponent<PooledParticleHelper>();
                    inst.gameObject.SetActive(false);
                    _muzzlePool.Add(inst);
                }
            }

            // Populate hit pool
            if (_hitVfxPrefab != null)
            {
                for (int i = 0; i < PoolSize; i++)
                {
                    var inst = Instantiate(_hitVfxPrefab, transform);
                    inst.gameObject.AddComponent<PooledParticleHelper>();
                    inst.gameObject.SetActive(false);
                    _hitPool.Add(inst);
                }
            }

            // Populate spark pool
            if (_sparkVfxPrefab != null)
            {
                for (int i = 0; i < PoolSize; i++)
                {
                    var inst = Instantiate(_sparkVfxPrefab, transform);
                    inst.gameObject.AddComponent<PooledParticleHelper>();
                    inst.gameObject.SetActive(false);
                    _sparkPool.Add(inst);
                }
            }
        }

        private ParticleSystem GetFromPool(List<ParticleSystem> pool, ParticleSystem prefab)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeSelf)
                {
                    return pool[i];
                }
            }

            // Fallback: spawn and expand pool dynamically if depleted
            if (prefab != null)
            {
                var inst = Instantiate(prefab, transform);
                inst.gameObject.AddComponent<PooledParticleHelper>();
                inst.gameObject.SetActive(false);
                pool.Add(inst);
                return inst;
            }
            return null;
        }

        public void PlayHit(Vector3 position, Quaternion rotation)
        {
            var inst = GetFromPool(_hitPool, _hitVfxPrefab);
            if (inst != null)
            {
                inst.transform.position = position;
                inst.transform.rotation = rotation;
                inst.gameObject.SetActive(true);
            }
        }

        public void PlayHit(Vector3 position)
        {
            PlayHit(position, Quaternion.identity);
        }

        public void PlayMuzzleFlash(Transform muzzlePoint)
        {
            var inst = GetFromPool(_muzzlePool, _muzzleFlashPrefab);
            if (inst != null)
            {
                inst.transform.position = muzzlePoint.position;
                inst.transform.rotation = muzzlePoint.rotation;
                inst.gameObject.SetActive(true);
            }
        }

        public void PlaySpark(Vector3 position, Quaternion rotation)
        {
            var inst = GetFromPool(_sparkPool, _sparkVfxPrefab);
            if (inst != null)
            {
                inst.transform.position = position;
                inst.transform.rotation = rotation;
                inst.gameObject.SetActive(true);
            }
        }
    }
}

