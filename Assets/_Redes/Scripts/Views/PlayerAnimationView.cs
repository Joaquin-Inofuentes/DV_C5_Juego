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

                // Reverse locomotion when walking backwards relative to aiming direction
                float multiplier = 1f;
                if (speed > 0.01f)
                {
                    float dot = Vector3.Dot(transform.forward, velocity.normalized);
                    if (dot < -0.1f)
                    {
                        multiplier = -1f; // Invert feet animation
                    }
                }
                
                // Shift sprint increases locomotion animation speed by 1.2x
                float sprintFactor = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 1.2f : 1.0f;
                _animator.SetFloat("MoveSpeedMultiplier", multiplier * sprintFactor);
            }
        }

        private void HandleShoot(float animSpeed)
        {
            if (_animator != null)
            {
                _animator.SetFloat("ShootSpeed", animSpeed);
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
