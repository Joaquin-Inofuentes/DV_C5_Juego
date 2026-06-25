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

        [Header("References")]
        [SerializeField] private PlayerEventBus _eventBus;
        [SerializeField] private DummyEnemy _target;
        [SerializeField] private AudioClip _shootSound;

        [Header("Debug UI")]
        [SerializeField] private Text _debugText;

        private float _fireCooldown;
        private int _shotsFired;
        private Vector3 _lastPosition;

        private void Start()
        {
            _lastPosition = transform.position;
            Debug.Log("[TEST][PLAYER] OfflinePlayerTester iniciado. WASD=mover, LMB=disparar, R=debug");
            Debug.Log($"[TEST][PLAYER]   EventBus: {(_eventBus != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Muzzle:   {(_muzzle != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Target:   {(_target != null ? "OK" : "NULL ⚠️")}");
            Debug.Log($"[TEST][PLAYER]   Sound:    {(_shootSound != null ? "OK" : "NULL ⚠️")}");
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
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
                transform.rotation = Quaternion.LookRotation(dir);

                _eventBus?.TriggerMove(dir.normalized * _moveSpeed);
            }
            else
            {
                _eventBus?.TriggerMove(Vector3.zero);
            }
        }

        private void HandleShooting()
        {
            _fireCooldown -= Time.deltaTime;

            bool firePressed = Input.GetButton("Fire1");
            if (firePressed && _fireCooldown <= 0f)
            {
                _fireCooldown = _fireRate;
                Shoot();
            }
        }

        private void Shoot()
        {
            _shotsFired++;
            Debug.Log($"[TEST][PLAYER] ¡Disparo #{_shotsFired}! Pos muzzle: {(_muzzle != null ? _muzzle.position.ToString() : "SIN MUZZLE")}");

            // Sound
            if (_shootSound != null)
                AudioSource.PlayClipAtPoint(_shootSound, transform.position);

            // Event Bus (drives animation)
            _eventBus?.TriggerShoot();

            // Hit detection — simple raycast or distance check to dummy
            if (_target != null && _target.IsAlive)
            {
                float dist = Vector3.Distance(transform.position, _target.transform.position);
                if (dist < 15f) // hit range
                {
                    Debug.Log($"[TEST][PLAYER] Impacto en dummy a {dist:F1}m. Daño={_damage}");
                    _target.TakeDamage(_damage);
                }
                else
                {
                    Debug.Log($"[TEST][PLAYER] Disparo fallado — dummy muy lejos ({dist:F1}m > 15m)");
                }
            }
        }

        private void HandleDebugKey()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[TEST][PLAYER] === DEBUG STATUS ===");
                Debug.Log($"  EventBus: {(_eventBus != null ? "OK" : "NULL")}");
                Debug.Log($"  Muzzle:   {(_muzzle != null ? _muzzle.position.ToString() : "NULL")}");
                Debug.Log($"  Target:   {(_target != null ? $"HP={_target.CurrentHealth}" : "NULL")}");
                Debug.Log($"  Shots:    {_shotsFired}");

                // Fire a force-test TriggerSpawned to verify bus connections
                _eventBus?.TriggerSpawned();
                Debug.Log("[TEST][PLAYER] TriggerSpawned() llamado (reset animaciones)");
            }
        }

        private void UpdateDebugUI()
        {
            if (_debugText == null) return;

            _debugText.text = $"WASD: Mover | LMB: Disparar | R: Debug Reset\n" +
                              $"Pos: {transform.position:F1}\n" +
                              $"Disparos: {_shotsFired}\n" +
                              $"Enemigo HP: {(_target != null ? _target.CurrentHealth + "/" + 100 : "N/A")}\n" +
                              $"EventBus: {(_eventBus != null ? "✓" : "✗")}";
        }
    }
}
