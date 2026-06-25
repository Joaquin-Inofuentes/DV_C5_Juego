using UnityEngine;
using Redes.Player;

namespace Redes.Views
{
    public class PlayerAnimationView : MonoBehaviour
    {
        [Header("References (auto-assigned by Prefab Tool)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerEventBus _eventBus;
        [SerializeField] private AudioSource _audioSource;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip _shootSound;
        [SerializeField] private AudioClip _reloadSound;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private AudioClip _footstepSound;

        [Header("Footstep Settings")]
        [SerializeField] private float _footstepInterval = 0.38f;
        private float _footstepTimer;

        private void OnEnable()
        {
            if (_eventBus != null)
            {
                _eventBus.OnMove += HandleMove;
                _eventBus.OnShoot += HandleShoot;
                _eventBus.OnReload += HandleReload;
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
                _eventBus.OnReload -= HandleReload;
                _eventBus.OnDied -= HandleDied;
                _eventBus.OnSpawned -= HandleSpawned;
            }
        }

        private void Update()
        {
            if (_animator != null)
            {
                float speed = _animator.GetFloat("MoveSpeed");
                if (speed > 0.2f)
                {
                    _footstepTimer -= Time.deltaTime;
                    if (_footstepTimer <= 0f)
                    {
                        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                        _footstepTimer = _footstepInterval / (isSprinting ? 1.2f : 1.0f);
                        PlaySound3D(_footstepSound, 0.3f);
                    }
                }
                else
                {
                    _footstepTimer = 0f;
                }
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

            PlaySound3D(_shootSound, 0.8f);
            
            if (VFXManager.Instance != null)
            {
                Transform muzzle = null;
                Transform parent = transform.parent;
                if (parent != null)
                {
                    muzzle = parent.Find("OrigenDeDisparo") ?? parent.Find("Muzzle");
                    if (muzzle == null && parent.parent != null)
                    {
                        muzzle = parent.parent.Find("OrigenDeDisparo") ?? parent.parent.Find("Muzzle");
                    }
                }
                else
                {
                    muzzle = transform.Find("OrigenDeDisparo") ?? transform.Find("Muzzle");
                }
                
                if (muzzle != null) VFXManager.Instance.PlayMuzzleFlash(muzzle);
            }
        }

        private void HandleReload()
        {
            PlaySound3D(_reloadSound, 0.8f);
        }

        private void HandleDied()
        {
            if (_animator != null)
            {
                _animator.SetBool("IsDead", true);
            }

            PlaySound3D(_deathSound, 1.0f);

            var ragdoll = GetComponentInParent<Gameplay.RagdollController>();
            if (ragdoll != null)
            {
                ragdoll.SetRagdollActive(true);
            }
        }

        private void HandleSpawned()
        {
            if (_animator != null)
            {
                _animator.SetBool("IsDead", false);
                _animator.SetFloat("MoveSpeed", 0f);
            }

            var ragdoll = GetComponentInParent<Gameplay.RagdollController>();
            if (ragdoll != null)
            {
                ragdoll.ResetBones();
            }
        }

        private void PlaySound3D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(clip, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            }
        }
    }
}
