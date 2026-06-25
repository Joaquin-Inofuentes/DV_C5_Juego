using UnityEngine;
using Redes.Player;

namespace Redes.Views
{
    public class PlayerAnimationView : MonoBehaviour
    {
        [Header("References (auto-assigned by Prefab Tool)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerEventBus _eventBus;

        [SerializeField] private AudioClip _shootSound;

        private void OnEnable()
        {
            if (_eventBus != null)
            {
                _eventBus.OnMove += HandleMove;
                _eventBus.OnShoot += HandleShoot;
                _eventBus.OnDied += HandleDied;
                _eventBus.OnSpawned += HandleSpawned;
            }
        }

        private void OnDisable()
        {
            if (_eventBus != null)
            {
                _eventBus.OnMove -= HandleMove;
                _eventBus.OnShoot -= HandleShoot;
                _eventBus.OnDied -= HandleDied;
                _eventBus.OnSpawned -= HandleSpawned;
            }
        }

        private void HandleMove(Vector3 velocity)
        {
            if (_animator != null)
            {
                float speed = velocity.magnitude;
                _animator.SetFloat("MoveSpeed", speed);
            }
        }

        private void HandleShoot()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Shoot");
            }

            if (_shootSound != null)
            {
                AudioSource.PlayClipAtPoint(_shootSound, transform.position);
            }
            
            if (VFXManager.Instance != null)
            {
                Transform muzzle = transform.parent != null ? transform.parent.Find("Muzzle") : transform.Find("Muzzle");
                if (muzzle == null && transform.parent != null && transform.parent.parent != null) 
                    muzzle = transform.parent.parent.Find("Muzzle");
                
                if (muzzle != null) VFXManager.Instance.PlayMuzzleFlash(muzzle);
            }
        }

        private void HandleDied()
        {
            if (_animator != null)
            {
                _animator.SetBool("IsDead", true);
            }
        }

        private void HandleSpawned()
        {
            if (_animator != null)
            {
                _animator.SetBool("IsDead", false);
                _animator.SetFloat("MoveSpeed", 0f);
            }
        }
    }
}
