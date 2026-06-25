using UnityEngine;
using UnityEngine.UI;
using Redes.Player;

namespace Redes.Test
{
    /// <summary>
    /// Offline player controller for the test scene.
    /// Directly drives movement, shooting and animates the player
    /// WITHOUT Fusion networking — purely for local input/animation/VFX testing.
    /// </summary>
    public class OfflinePlayerTester : MonoBehaviour
    {
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
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private Text _debugText;

        private float _fireCooldown;
        private int _shotsFired;
        private Vector3 _lastPosition;

        private int _currentAmmo;
        private bool _isReloading;
        private float _reloadTimer;
        private float _shootClipDuration = 0.5f; // fallback

        private void Start()
        {
            _lastPosition = transform.position;
            _currentAmmo = _maxAmmo;

            // Read the shoot clip duration from the animator so we can scale speed precisely
            var model = transform.Find("Model");
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
            
            Debug.Log("[TEST][PLAYER] OfflinePlayerTester iniciado. WASD=mover, LMB=disparar, R=recargar, T=debug");
            Debug.Log($"[TEST][PLAYER]   EventBus: {(_eventBus != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Muzzle:   {(_muzzle != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Target:   {(_target != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Sound:    {(_shootSound != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Bullet:   {(_bulletPrefab != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   FireRate: {_fireRate:F2}s  ClipDur: {_shootClipDuration:F3}s  AnimSpeed: {(_shootClipDuration / _fireRate):F2}x");

            // Initial UI sync
            _eventBus?.TriggerAmmoChanged(_currentAmmo, _maxAmmo);
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
            HandleReloadInput();
            HandleReloadTimer();
            HandleDebugKey();
            UpdateDebugUI();
        }

        private void HandleMovement()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 dir = new Vector3(h, 0, v);

            if (dir.sqrMagnitude > 0.01f)
            {
                Vector3 move = dir.normalized * _moveSpeed * Time.deltaTime;
                transform.position += move;

                _eventBus?.TriggerMove(dir.normalized * _moveSpeed);
            }
            else
            {
                _eventBus?.TriggerMove(Vector3.zero);
            }

            // Always rotate to face the mouse cursor (orthogonal/top-down standard aiming)
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

        private void HandleShooting()
        {
            _fireCooldown -= Time.deltaTime;

            if (_isReloading) return;

            bool firePressed = Input.GetButton("Fire1");
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

            Debug.Log($"[TEST][PLAYER] ¡Disparo #{_shotsFired}! Pos muzzle: {(_muzzle != null ? _muzzle.position.ToString() : "SIN MUZZLE")}. Balas: {_currentAmmo}/{_maxAmmo}");

            // Sound
            if (_shootSound != null)
                AudioSource.PlayClipAtPoint(_shootSound, transform.position);

            // Animation speed = clipDuration / fireRate
            // This makes the animation complete in exactly _fireRate seconds
            // e.g. 0.5s clip / 0.2s rate = 2.5x speed → anim plays in 0.2s (fills the gap)
            float animSpeed = _shootClipDuration / _fireRate;

            // Event Bus (drives animation with dynamic speed)
            _eventBus?.TriggerShoot(animSpeed);

            // Spawn visual bullet
            if (_bulletPrefab != null && _muzzle != null)
            {
                var bulletGo = Instantiate(_bulletPrefab, _muzzle.position, transform.rotation);
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

        private void HandleReloadInput()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartReload();
            }
        }

        private void StartReload()
        {
            if (_isReloading || _currentAmmo == _maxAmmo) return;
            _isReloading = true;
            _reloadTimer = _reloadDuration;
            Debug.Log("[TEST][PLAYER] Recargando...");
            _eventBus?.TriggerReload();
            if (_reloadSound != null)
                AudioSource.PlayClipAtPoint(_reloadSound, transform.position);
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
                    Debug.Log("[TEST][PLAYER] Recarga completa.");
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

            _debugText.text = $"WASD: Mover | LMB: Disparar | R: Recargar | T: Debug Reset\n" +
                              $"Pos: {transform.position:F1}\n" +
                              $"Disparos: {_shotsFired}\n" +
                              $"Munición: {_currentAmmo}/{_maxAmmo} {(_isReloading ? "(Recargando...)" : "")}\n" +
                              $"Enemigo HP: {(_target != null ? _target.CurrentHealth + "/" + 100 : "N/A")}\n" +
                              $"EventBus: {(_eventBus != null ? "✓" : "✗")}";
        }
    }
}
