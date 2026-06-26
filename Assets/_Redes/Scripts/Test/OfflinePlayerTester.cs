using UnityEngine;
using UnityEngine.UI;
using Redes.Player;

namespace Redes.Test
{
    /// <summary>
    /// Offline player controller for the test scene.
    /// Directly drives movement, shooting and animates the player
    /// WITHOUT Fusion networking — purely for local input/animation/VFX testing.
    /// Handles death with ragdoll activation via EventBus.
    /// </summary>
    public enum PlayerInputMode
    {
        WASD_Mouse,
        Arrows_Space
    }

    public class OfflinePlayerTester : MonoBehaviour
    {
        [Header("Input Setup")]
        [SerializeField] private PlayerInputMode _inputMode = PlayerInputMode.WASD_Mouse;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Shooting")]
        [SerializeField] private int _damage = 25;
        [SerializeField] private float _fireRate = 0.2f;
        [SerializeField] private Transform _muzzle;

        [Header("Ammo")]
        [SerializeField] private int _maxAmmo = 10;
        [SerializeField] private float _reloadDuration = 1.5f;

        [Header("References")]
        [SerializeField] private PlayerEventBus _eventBus;
        [SerializeField] private DummyEnemy _target;
        [SerializeField] private AudioClip _shootSound;
        [SerializeField] private AudioClip _reloadSound;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private Text _debugText;
        [SerializeField] private Views.EntityDisplayView _displayView;

        [Header("Health Config")]
        [SerializeField] private int _maxHealth = 100;

        private float _fireCooldown;
        private int _shotsFired;
        private Vector3 _lastPosition;

        private int _currentAmmo;
        private bool _isReloading;
        private float _reloadTimer;
        private float _shootClipDuration = 0.5f; // fallback
        private Coroutine _recoilCoroutine;
        private int _currentHealth;
        private bool _isDead;
        private Vector3 _lastHitDirection;

        private void Start()
        {
            _lastPosition = transform.position;
            _currentAmmo = _maxAmmo;
            _currentHealth = _maxHealth;
            _isDead = false;

            if (_displayView != null)
            {
                _displayView.SetNickname("PLAYER");
                _displayView.SetHealth(1f);
            }

            // Read the shoot clip duration from the animator so we can scale speed precisely
            var model = transform.Find("[MODEL & ANIMATOR] Player Visual Model") ?? transform.Find("Model");
            if (model != null)
            {
                var animator = model.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    {
                        if (clip.name.ToLower().Contains("shoot"))
                        {
                            _shootClipDuration = clip.length;
                            Debug.Log($"[TEST][PLAYER]   ShootClip: {clip.name} dur={_shootClipDuration:F3}s");
                            break;
                        }
                    }
                }
            }
            
            Debug.Log($"[TEST][PLAYER] OfflinePlayerTester iniciado en modo {_inputMode}. WASD/Arrows=mover, LMB/Space=disparar, Shift=sprint");
            Debug.Log($"[TEST][PLAYER]   EventBus: {(_eventBus != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Muzzle:   {(_muzzle != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Target:   {(_target != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Sound:    {(_shootSound != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Bullet:   {(_bulletPrefab != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   FireRate: {_fireRate:F2}s  ClipDur: {_shootClipDuration:F3}s  AnimSpeed: {(_shootClipDuration / _fireRate):F2}x");

            // Initial UI sync
            _eventBus?.TriggerAmmoChanged(_currentAmmo, _maxAmmo);

            // Play ambient music programmatically in play mode
            var bgm = GameObject.Find("AmbientMusic");
            if (bgm != null)
            {
                var src = bgm.GetComponent<AudioSource>();
                if (src != null && !src.isPlaying)
                {
                    src.Play();
                    Debug.Log("[TEST][PLAYER] AmbientMusic started programmatically in Play Mode.");
                }
            }
        }

        private void Update()
        {
            if (_isDead) return;

            HandleMovement();
            HandleShooting();
            HandleReloadInput();
            HandleReloadTimer();
            HandleDebugKey();
            UpdateDebugUI();
        }

        private void LateUpdate()
        {
            if (_displayView != null && Camera.main != null)
            {
                Vector3 worldPos = transform.position + Vector3.up * 2.2f;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                if (screenPos.z > 0 && !_isDead)
                {
                    _displayView.SetVisible(true);
                    _displayView.SetPosition(screenPos);
                    float reloadProgress = _isReloading ? 1f - (_reloadTimer / _reloadDuration) : 0f;
                    _displayView.SetReloadProgress(reloadProgress, _isReloading);
                }
                else
                {
                    _displayView.SetVisible(false);
                }
            }
        }

        public void TakeDamage(int amount, Vector3 hitDirection = default)
        {
            if (_isDead) return;
            _lastHitDirection = hitDirection;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            if (_displayView != null)
            {
                _displayView.SetHealth((float)_currentHealth / _maxHealth);
            }
            Debug.Log($"[TEST][PLAYER] Jugador recibio {amount} de daño → HP={_currentHealth}/{_maxHealth}");

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            _isDead = true;
            _isReloading = false;
            _eventBus?.TriggerDied(_lastHitDirection);
            Debug.Log("[TEST][PLAYER] ¡MUERTO! Activando Ragdoll.");
            DebugSystem.EventBus.TriggerPlayerDeath(DebugSystem.LocalNetworkMock.LocalActorID, 0, "DummyEnemy");
            StartCoroutine(RespawnCoroutine());
        }

        private System.Collections.IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(10.0f); // Let the ragdoll rest on the floor for 10.0s

            // Reset player position and health
            transform.position = Vector3.zero;
            _currentHealth = _maxHealth;
            _currentAmmo = _maxAmmo;
            _isDead = false;

            if (_displayView != null)
            {
                _displayView.SetVisible(true);
                _displayView.SetHealth(1f);
            }

            _eventBus?.TriggerSpawned();
            _eventBus?.TriggerAmmoChanged(_currentAmmo, _maxAmmo);
            DebugSystem.EventBus.TriggerRespawnExecuted();
            Debug.Log("[TEST][PLAYER] Jugador respawneado.");
        }

        private void HandleMovement()
        {
            float h = 0f;
            float v = 0f;

            if (_inputMode == PlayerInputMode.WASD_Mouse)
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }
            else
            {
                if (Input.GetKey(KeyCode.UpArrow)) v += 1f;
                if (Input.GetKey(KeyCode.DownArrow)) v -= 1f;
                if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
                if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
            }

            Vector3 dir = new Vector3(h, 0, v);

            // Shift increases movement speed by 1.2x
            bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float currentMoveSpeed = _moveSpeed * (isSprinting ? 1.2f : 1.0f);

            if (dir.sqrMagnitude > 0.01f)
            {
                Vector3 move = dir.normalized * currentMoveSpeed * Time.deltaTime;
                transform.position += move;

                _eventBus?.TriggerMove(dir.normalized * currentMoveSpeed);
            }
            else
            {
                _eventBus?.TriggerMove(Vector3.zero);
            }

            // Aiming / Rotation
            if (_inputMode == PlayerInputMode.WASD_Mouse)
            {
                // Always rotate to face the mouse cursor
                if (Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                    if (groundPlane.Raycast(ray, out float rayDistance))
                    {
                        Vector3 lookPoint = ray.GetPoint(rayDistance);
                        Vector3 lookDir = lookPoint - transform.position;
                        lookDir.y = 0f; // Keep rotation horizontal
                        if (lookDir.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.LookRotation(lookDir);
                        }
                    }
                }
            }
            else
            {
                // Rotate to face movement direction for Keyboard-only Player 2
                if (dir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(dir.normalized);
                }
            }
        }

        private void HandleShooting()
        {
            _fireCooldown -= Time.deltaTime;

            if (_isReloading) return;

            bool firePressed = false;
            if (_inputMode == PlayerInputMode.WASD_Mouse)
            {
                firePressed = Input.GetButton("Fire1") || Input.GetMouseButton(0);
            }
            else
            {
                firePressed = Input.GetKey(KeyCode.Space);
            }

            if (firePressed && _fireCooldown <= 0f)
            {
                _fireCooldown = _fireRate;
                if (_currentAmmo > 0)
                {
                    Shoot();
                }
                else
                {
                    // Dry fire -> Start reload automatically!
                    StartReload();
                }
            }
        }

        private void Shoot()
        {
            _currentAmmo--;
            _shotsFired++;
            _eventBus?.TriggerAmmoChanged(_currentAmmo, _maxAmmo);

            Debug.Log($"[TEST][PLAYER] Disparo #{_shotsFired} ({_inputMode})! Pos muzzle: {(_muzzle != null ? _muzzle.position.ToString() : "SIN MUZZLE")}. Balas: {_currentAmmo}/{_maxAmmo}");

            // Double shoot animation duration -> Half animation speed
            // e.g. 0.5s clip / 0.2s rate = 2.5x speed. * 0.5f = 1.25x speed (takes double duration, i.e. 0.4s)
            float animSpeed = (_shootClipDuration / _fireRate) * 0.5f;

            // Event Bus (drives animation and audio with dynamic speed)
            _eventBus?.TriggerShoot(animSpeed);

            // Trigger recoil kickback coroutine for snappy combat feel
            if (_recoilCoroutine != null) StopCoroutine(_recoilCoroutine);
            _recoilCoroutine = StartCoroutine(RecoilCoroutine());

            // Determine shoot direction
            Vector3 shootDir = transform.forward;
            if (_inputMode == PlayerInputMode.WASD_Mouse && Camera.main != null && _muzzle != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                if (groundPlane.Raycast(ray, out float rayDistance))
                {
                    Vector3 lookPoint = ray.GetPoint(rayDistance);
                    Vector3 targetPoint = new Vector3(lookPoint.x, _muzzle.position.y, lookPoint.z);
                    shootDir = (targetPoint - _muzzle.position).normalized;
                }
            }

            // Spawn visual bullet at Muzzle facing the exact target point
            if (_bulletPrefab != null && _muzzle != null)
            {
                var bulletGo = Instantiate(_bulletPrefab, _muzzle.position, Quaternion.LookRotation(shootDir));
                var offlineBullet = bulletGo.AddComponent<OfflineBullet>();
                offlineBullet.Speed = 25f;
                offlineBullet.Damage = _damage;
            }
            else
            {
                // Fallback to instant distance check if prefab is missing
                if (_target != null && _target.IsAlive)
                {
                    float dist = Vector3.Distance(transform.position, _target.transform.position);
                    if (dist < 15f)
                    {
                        Debug.Log($"[TEST][PLAYER] (Sin Prefab) Impacto en dummy a {dist:F1}m. Daño={_damage}");
                        _target.TakeDamage(_damage);
                    }
                }
            }
        }

        private System.Collections.IEnumerator RecoilCoroutine()
        {
            var model = transform.Find("[MODEL & ANIMATOR] Player Visual Model") ?? transform.Find("Model");
            if (model == null) yield break;

            Vector3 origPos = Vector3.zero;
            float elapsed = 0f;
            float duration = 0.12f; // Fast snappy recovery
            Vector3 kickback = -Vector3.forward * 0.35f; // Sneak backward slightly
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Sin curve for punchy recoil return
                float weight = Mathf.Sin(t * Mathf.PI);
                model.localPosition = origPos + kickback * weight;
                yield return null;
            }

            model.localPosition = origPos;
            _recoilCoroutine = null;
        }

        private void HandleReloadInput()
        {
            if (Input.GetKeyDown(KeyCode.R) && _inputMode == PlayerInputMode.WASD_Mouse)
            {
                StartReload();
            }
        }

        private void StartReload()
        {
            if (_isReloading || _currentAmmo == _maxAmmo) return;
            _isReloading = true;
            _reloadTimer = _reloadDuration;
            Debug.Log($"[TEST][PLAYER] ({_inputMode}) Recargando...");
            _eventBus?.TriggerReload();
        }

        private void HandleReloadTimer()
        {
            if (_isReloading)
            {
                _reloadTimer -= Time.deltaTime;
                if (_reloadTimer <= 0f)
                {
                    _currentAmmo = _maxAmmo;
                    _isReloading = false;
                    _eventBus?.TriggerAmmoChanged(_currentAmmo, _maxAmmo);
                    Debug.Log($"[TEST][PLAYER] ({_inputMode}) Recarga completa.");
                }
            }
        }

        private void HandleDebugKey()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("[TEST][PLAYER] === DEBUG STATUS ===");
                Debug.Log($"  EventBus: {(_eventBus != null ? "OK" : "NULL")}");
                Debug.Log($"  Muzzle:   {(_muzzle != null ? _muzzle.position.ToString() : "NULL")}");
                Debug.Log($"  Target:   {(_target != null ? $"HP={_target.CurrentHealth}" : "NULL")}");
                Debug.Log($"  Shots:    {_shotsFired}");

                _eventBus?.TriggerSpawned();
                Debug.Log("[TEST][PLAYER] TriggerSpawned() llamado (reset animaciones)");
            }
        }

        private void UpdateDebugUI()
        {
            if (_debugText == null) return;

            string ctrlInfo = _inputMode == PlayerInputMode.WASD_Mouse 
                ? "WASD: Mover | LMB: Disparar | R: Recargar"
                : "FLECHAS: Mover | ESPACIO: Disparar (Auto-Recarga)";

            _debugText.text = $"{ctrlInfo} | T: Debug Reset\n" +
                              $"Pos: {transform.position:F1}\n" +
                              $"Disparos: {_shotsFired}\n" +
                              $"Munición: {_currentAmmo}/{_maxAmmo} {(_isReloading ? "(Recargando...)" : "")}\n" +
                              $"Enemigo HP: {(_target != null ? _target.CurrentHealth + "/" + 100 : "N/A")}\n" +
                              $"EventBus: {(_eventBus != null ? "✓" : "✗")}";
        }
    }
}
