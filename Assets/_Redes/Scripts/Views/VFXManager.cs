using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Redes.Core;

namespace Redes.Views
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private ParticleSystem _hitVfxPrefab;       // Blood burst (Red)
        [SerializeField] private ParticleSystem _muzzleFlashPrefab;  // Muzzle flash (Green)
        [SerializeField] private ParticleSystem _sparkVfxPrefab;      // Obstacle spark (White)

        [Header("SFX")]
        [SerializeField] private AudioClip _obstacleHitSound;
        [SerializeField] private AudioClip[] _ouchSounds;   // Varios clips de dolor (reproducidos aleatoriamente)
        [SerializeField] private AudioMixerGroup _sfxGroup;

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
            RedesLog.Info(RedesLog.VFX, $"[VFX] PlayHit (blood burst) at {position}");
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
            RedesLog.Info(RedesLog.VFX, $"[VFX] PlayMuzzleFlash at {muzzlePoint.position}");
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
            RedesLog.Info(RedesLog.VFX, $"[VFX] PlaySpark (obstacle) at {position}");
            var inst = GetFromPool(_sparkPool, _sparkVfxPrefab);
            if (inst != null)
            {
                inst.transform.position = position;
                inst.transform.rotation = rotation;
                inst.gameObject.SetActive(true);
            }

            // Play obstacle impact sound
            if (_obstacleHitSound != null)
            {
                RedesLog.Info(RedesLog.VFX, $"[SFX] ObstacleHit sound at {position}");
                PlayClipAtPointRouted(_obstacleHitSound, position, 0.7f);
            }
        }

        public void PlayOuch(Vector3 position)
        {
            if (_ouchSounds == null || _ouchSounds.Length == 0) return;
            var clip = _ouchSounds[Random.Range(0, _ouchSounds.Length)];
            if (clip != null)
            {
                RedesLog.Info(RedesLog.VFX, $"[SFX] Ouch sound '{clip.name}' at {position}");
                PlayClipAtPointRouted(clip, position, 0.75f);
            }
        }

        private void PlayClipAtPointRouted(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;
            GameObject tempGo = new GameObject("TempSFX_" + clip.name);
            tempGo.transform.position = position;
            AudioSource source = tempGo.AddComponent<AudioSource>();
            source.clip = clip;
            source.outputAudioMixerGroup = _sfxGroup;
            source.volume = volume;
            source.spatialBlend = 1f; // 3D spatial sound
            source.minDistance = 2f;
            source.maxDistance = 20f;
            source.Play();
            Destroy(tempGo, clip.length + 0.1f);
        }
    }
}
