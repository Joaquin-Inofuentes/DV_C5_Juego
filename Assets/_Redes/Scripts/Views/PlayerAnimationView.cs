using UnityEngine;
using Redes.Player;
using Redes.Core;

namespace Redes.Views
{
    public class PlayerAnimationView : MonoBehaviour
    {
        [Header("References (auto-assigned by Prefab Tool)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerEventBus _eventBus;
        [SerializeField] private AudioSource _audioSource;

        [Header("SFX Clips - Disparos (1 por jugador, selección por PlayerId % length)")]
        [SerializeField] private AudioClip[] _shootSounds;   // [0]=Disparo1, [1]=Disparo2, [2]=Disparo3

        [Header("SFX Clips - Impacto (aleatorio al recibir daño)")]
        [SerializeField] private AudioClip[] _ouchSounds;    // [0..3]=RecibirDano1..4

        [Header("SFX Clips - Generales")]
        [SerializeField] private AudioClip _reloadSound;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private AudioClip _footstepSound;

        // PlayerId del jugador local (cacheado en Start para selección de SFX)
        private int _cachedPlayerId = 1;

        [Header("Footstep Settings")]
        [SerializeField] private float _footstepInterval = 0.38f;
        private float _footstepTimer;
        private float _currentMoveSpeed;

        // --- Network References for Reactive Sinc ---
        private Redes.Player.NetworkPlayer _netPlayer;
        private PlayerMovement _movement;
        private PlayerShooting _shooting;
        private PlayerHealth _health;
        private AmmoSystem _ammo;

        // Local state trackers to detect transitions in networked properties
        private bool _isNetworked;
        private bool _wasMoving;
        private int _lastShootCount;
        private bool _lastIsReloading;
        private int _lastHealth = 100;

        private void Awake()
        {
            _netPlayer = GetComponent<Redes.Player.NetworkPlayer>();
            _movement = GetComponent<PlayerMovement>();
            _shooting = GetComponent<PlayerShooting>();
            _health = GetComponent<PlayerHealth>();
            _ammo = GetComponent<AmmoSystem>();
        }

        private void Start()
        {
            // Detect if we are running in an active network runner session
            _isNetworked = (_netPlayer != null && _netPlayer.Object != null && _netPlayer.Object.IsValid);

            if (_isNetworked)
            {
                // Initialize local variables to current networked states to avoid trigger spikes on spawn
                _wasMoving = _movement != null && _movement.NetworkVelocity.sqrMagnitude > 0.01f;
                _lastShootCount = _shooting != null ? _shooting.ShootCount : 0;
                _lastIsReloading = _ammo != null && _ammo.IsReloading;
                _lastHealth = _health != null ? _health.CurrentHealth : 100;

                // Cachear PlayerId para selección de clip de disparo
                if (_netPlayer != null && _netPlayer.Object != null && _netPlayer.Object.IsValid)
                    _cachedPlayerId = _netPlayer.Object.InputAuthority.PlayerId;

                // Debug.Log($"[REDES][NET_ANIM] Player '{_netPlayer.Nickname}' (ID: {_netPlayer.Object.InputAuthority.PlayerId}) animation view initialized in NETWORKED mode.");
            }
            else
            {
                // Debug.Log("[REDES][NET_ANIM] Animation view initialized in OFFLINE mode.");
            }
        }

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
            if (_isNetworked)
            {
                UpdateNetworkedState();
            }
            else
            {
                UpdateOfflineFootsteps();
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  REACTIVE NETWORKED ANIMATION & SOUND LOGIC
        // ──────────────────────────────────────────────────────────────────
        private void UpdateNetworkedState()
        {
            if (_netPlayer == null || !_netPlayer.Object.IsValid) return;

            // 1. Move Speed & Footsteps
            Vector3 velocity = _movement != null ? _movement.NetworkVelocity : Vector3.zero;
            float speed = velocity.magnitude;

            if (_animator != null)
            {
                _animator.SetFloat("MoveSpeed", speed);

                // Reverse feet animation if walking backwards relative to look direction
                float multiplier = 1f;
                if (speed > 0.01f)
                {
                    float dot = Vector3.Dot(transform.forward, velocity.normalized);
                    if (dot < -0.1f)
                    {
                        multiplier = -1f;
                    }
                }
                _animator.SetFloat("MoveSpeedMultiplier", multiplier);
            }

            bool isMoving = speed > 0.01f;
            if (isMoving != _wasMoving)
            {
                _wasMoving = isMoving;
                // Debug.Log($"[REDES][NET_ANIM] Player '{_netPlayer.Nickname}' (Local={_netPlayer.Object.HasInputAuthority}) {(isMoving ? "STARTED walking. Velocity: " + velocity : "STOPPED walking.")}");
            }

            if (isMoving)
            {
                _footstepTimer -= Time.deltaTime;
                if (_footstepTimer <= 0f)
                {
                    _footstepTimer = _footstepInterval;
                    PlaySound3D(_footstepSound, 0.3f);
                }
            }
            else
            {
                _footstepTimer = 0f;
            }

            // 2. Shooting
            int currentShootCount = _shooting != null ? _shooting.ShootCount : 0;
            if (currentShootCount > _lastShootCount)
            {
                _lastShootCount = currentShootCount;
                bool isLocal = _netPlayer.Object.HasInputAuthority;
                RedesLog.Info(RedesLog.VFX, $"[NET_ANIM] Player '{_netPlayer.Nickname}' ({(isLocal ? "LOCAL" : "REMOTE")}) FIRED shot #{currentShootCount}. MuzzleFlash+ShootSFX triggered.");
                
                if (_animator != null)
                {
                    _animator.SetFloat("ShootSpeed", 1.0f);
                    _animator.SetTrigger("Shoot");
                }

                if (_audioSource != null)
                {
                    _audioSource.pitch = 1.0f;
                }
                PlaySound3D(SelectShootClip(_cachedPlayerId), 0.85f);

                PlayMuzzleFlashEffect();
            }

            // 3. Reloading
            bool currentIsReloading = _ammo != null && _ammo.IsReloading;
            if (currentIsReloading != _lastIsReloading)
            {
                _lastIsReloading = currentIsReloading;
                if (currentIsReloading)
                {
                    // Debug.Log($"[REDES][NET_ANIM] Player '{_netPlayer.Nickname}' (Local={_netPlayer.Object.HasInputAuthority}) STARTED reloading.");
                    if (_audioSource != null)
                    {
                        _audioSource.pitch = 1.0f;
                    }
                    PlaySound3D(_reloadSound, 0.8f);
                }
            }

            // 4. Health / Hit / Death
            int currentHealth = _health != null ? _health.CurrentHealth : 100;
            if (currentHealth != _lastHealth)
            {
                bool isLocal = _netPlayer.Object.HasInputAuthority;
                if (currentHealth < _lastHealth)
                {
                    if (currentHealth <= 0)
                    {
                        RedesLog.Info(RedesLog.VFX, $"[NET_ANIM] Player '{_netPlayer.Nickname}' ({(isLocal ? "LOCAL" : "REMOTE")}) DIED. Health: {currentHealth}. DeathSFX+Ragdoll triggered.");
                        
                        if (_audioSource != null)
                        {
                            _audioSource.pitch = 1.0f;
                        }
                        if (_animator != null)
                        {
                            _animator.SetBool("IsDead", true);
                        }
                        PlaySound3D(_deathSound, 1.0f);

                        // Trigger Ragdoll controller
                        var ragdoll = GetComponent<Gameplay.RagdollController>();
                        if (ragdoll != null)
                        {
                            Vector3 hitForceDir = _health != null ? _health.LastHitDirection : Vector3.up;
                            ragdoll.SetRagdollActive(true, hitForceDir);
                        }
                    }
                    else
                    {
                        int dmg = _lastHealth - currentHealth;
                        RedesLog.Info(RedesLog.VFX, $"[NET_ANIM] Player '{_netPlayer.Nickname}' ({(isLocal ? "LOCAL" : "REMOTE")}) TOOK DAMAGE (-{dmg}). Health: {currentHealth}. OuchSFX triggered.");
                        // Play ouch sound aleatorio al recibir daño (VIEW elige clip, modelo no sabe nada de audio)
                        PlaySound3D(SelectRandomOuchClip(), 0.95f);
                        // También reproducir en VFXManager para audio 3D posicional del enemigo
                        if (VFXManager.Instance != null)
                        {
                            VFXManager.Instance.PlayOuch(transform.position);
                        }
                    }
                }
                else if (currentHealth > _lastHealth && _lastHealth <= 0)
                {
                    // Debug.Log($"[REDES][NET_ANIM] Player '{_netPlayer.Nickname}' (Local={_netPlayer.Object.HasInputAuthority}) RESPAWNED / SPAWNED. Health: {currentHealth}");
                    
                    if (_audioSource != null)
                    {
                        _audioSource.pitch = 1.0f;
                    }
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
                _lastHealth = currentHealth;
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  OFFLINE FALLBACK LOGIC
        // ──────────────────────────────────────────────────────────────────
        private void UpdateOfflineFootsteps()
        {
            if (_currentMoveSpeed > 0.2f)
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

        private void HandleMove(Vector3 velocity)
        {
            if (_isNetworked) return; // Managed by UpdateNetworkedState

            _currentMoveSpeed = velocity.magnitude;

            if (_animator != null)
            {
                float speed = velocity.magnitude;
                _animator.SetFloat("MoveSpeed", speed);

                float multiplier = 1f;
                if (speed > 0.01f)
                {
                    float dot = Vector3.Dot(transform.forward, velocity.normalized);
                    if (dot < -0.1f)
                    {
                        multiplier = -1f;
                    }
                }
                
                float sprintFactor = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 1.2f : 1.0f;
                _animator.SetFloat("MoveSpeedMultiplier", multiplier * sprintFactor);
            }
        }

        private void HandleShoot(float animSpeed)
        {
            if (_isNetworked) return; // Managed by UpdateNetworkedState

            if (_animator != null)
            {
                _animator.SetFloat("ShootSpeed", animSpeed);
                _animator.SetTrigger("Shoot");
            }

            if (_audioSource != null)
            {
                _audioSource.pitch = 1.0f;
            }

            PlaySound3D(SelectShootClip(_cachedPlayerId), 0.85f);
            PlayMuzzleFlashEffect();
        }

        private void HandleReload()
        {
            if (_isNetworked) return; // Managed by UpdateNetworkedState

            if (_audioSource != null)
            {
                _audioSource.pitch = 1.0f;
            }
            PlaySound3D(_reloadSound, 0.8f);
        }

        private void HandleDied(Vector3 hitDirection)
        {
            if (_isNetworked) return; // Managed by UpdateNetworkedState

            if (_audioSource != null)
            {
                _audioSource.pitch = 1.0f;
            }

            if (_animator != null)
            {
                _animator.SetBool("IsDead", true);
            }

            PlaySound3D(_deathSound, 1.0f);

            var ragdoll = GetComponentInParent<Gameplay.RagdollController>();
            if (ragdoll != null)
            {
                ragdoll.SetRagdollActive(true, hitDirection);
            }
        }

        private void HandleSpawned()
        {
            if (_isNetworked) return; // Managed by UpdateNetworkedState

            if (_audioSource != null)
            {
                _audioSource.pitch = 1.0f;
            }

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

        // ──────────────────────────────────────────────────────────────────
        //  COMMON AUDIO & VFX UTILITIES
        // ──────────────────────────────────────────────────────────────────
        // ─── Selección de clips (lógica de presentación, puramente en la Vista) ──
        /// <summary>Selecciona el clip de disparo según el PlayerId del jugador (1-indexado).</summary>
        private AudioClip SelectShootClip(int playerId)
        {
            if (_shootSounds == null || _shootSounds.Length == 0) return null;
            // PlayerId 1 → índice 0, PlayerId 2 → índice 1, resto → módulo
            int idx = (playerId - 1) % _shootSounds.Length;
            if (idx < 0) idx = 0;
            return _shootSounds[idx];
        }

        /// <summary>Selecciona un clip de dolor/impacto de forma aleatoria.</summary>
        private AudioClip SelectRandomOuchClip()
        {
            if (_ouchSounds == null || _ouchSounds.Length == 0) return null;
            return _ouchSounds[Random.Range(0, _ouchSounds.Length)];
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

        private void PlayMuzzleFlashEffect()
        {
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
                
                if (muzzle != null)
                {
                    VFXManager.Instance.PlayMuzzleFlash(muzzle);
                }
            }
        }
    }
}
