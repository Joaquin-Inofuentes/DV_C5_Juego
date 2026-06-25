using UnityEngine;

namespace Redes.Views
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private ParticleSystem _hitVfxPrefab;
        [SerializeField] private ParticleSystem _muzzleFlashPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void PlayHit(Vector3 position)
        {
            if (_hitVfxPrefab != null)
            {
                var inst = Instantiate(_hitVfxPrefab, position, Quaternion.identity);
                Destroy(inst.gameObject, 1f);
            }
        }

        public void PlayMuzzleFlash(Transform muzzlePoint)
        {
            if (_muzzleFlashPrefab != null)
            {
                var inst = Instantiate(_muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
                Destroy(inst.gameObject, 0.5f);
            }
        }
    }
}
