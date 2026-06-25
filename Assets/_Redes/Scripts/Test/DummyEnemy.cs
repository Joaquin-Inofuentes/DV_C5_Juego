using UnityEngine;

namespace Redes.Test
{
    /// <summary>
    /// Offline dummy enemy for the test scene.
    /// Takes damage, shows health, dies, and respawns.
    /// Completely independent of Fusion networking.
    /// </summary>
    public class DummyEnemy : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private float _respawnDelay = 2f;

        [Header("Visual")]
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Color _aliveColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _deadColor = new Color(0.3f, 0.1f, 0.1f);

        [Header("Display Overlay")]
        [SerializeField] private Views.EntityDisplayView _displayView;

        [Header("Shooting Config")]
        [SerializeField] private bool _autoShoot = true;
        [SerializeField] private float _shootInterval = 1f;
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private int _shootDamage = 15;
        [SerializeField] private float _shootSpeed = 15f;

        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        public System.Action<int> OnDummyKilled;   // payload = total kills

        private int _totalKills;
        private float _respawnTimer;
        private bool _isDead;
        private Coroutine _hitAnimCoroutine;
        private float _shootTimer;

        private void Awake()
        {
            CurrentHealth = _maxHealth;
        }

        private void Start()
        {
            ApplyColor();
            if (_displayView != null)
            {
                _displayView.SetNickname("DUMMY_ENEMY");
                _displayView.SetHealth(1f);
            }
            Debug.Log($"[TEST][DUMMY] Dummy Enemy inicializado. HP={CurrentHealth}");
        }

        private void Update()
        {
            if (_isDead)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f) Respawn();
            }
            else if (_autoShoot && _bulletPrefab != null && _muzzle != null)
            {
                _shootTimer -= Time.deltaTime;
                if (_shootTimer <= 0f)
                {
                    _shootTimer = _shootInterval;
                    Shoot();
                }
            }
        }

        private void Shoot()
        {
            if (_bulletPrefab == null || _muzzle == null) return;
            var bulletGo = Instantiate(_bulletPrefab, _muzzle.position, transform.rotation);
            var offlineBullet = bulletGo.AddComponent<OfflineBullet>();
            offlineBullet.Speed = _shootSpeed;
            offlineBullet.Damage = _shootDamage;
            offlineBullet.IsEnemyBullet = true; // Tell the bullet it was fired by the enemy
        }

        private void LateUpdate()
        {
            if (_displayView != null && Camera.main != null && !_isDead)
            {
                Vector3 worldPos = transform.position + Vector3.up * 2.2f;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                if (screenPos.z > 0)
                {
                    _displayView.SetVisible(true);
                    _displayView.SetPosition(screenPos);
                }
                else
                {
                    _displayView.SetVisible(false);
                }
            }
            else if (_displayView != null && _isDead)
            {
                _displayView.SetVisible(false);
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (_displayView != null)
            {
                _displayView.SetHealth((float)CurrentHealth / _maxHealth);
            }
            Debug.Log($"[TEST][DUMMY] Recibio {amount} de daño → HP={CurrentHealth}/{_maxHealth}");

            if (CurrentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (_hitAnimCoroutine != null)
                {
                    StopCoroutine(_hitAnimCoroutine);
                }
                _hitAnimCoroutine = StartCoroutine(HitAnimation());
            }
        }

        private System.Collections.IEnumerator HitAnimation()
        {
            float duration = 0.22f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            // Push dummy backward slightly in the direction of bullet impact (dummy faces shooter, so transform.forward goes away from shooter)
            Vector3 knockback = transform.forward * 0.5f;

            Color flashColor = Color.white;
            Color normalColor = IsAlive ? Color.Lerp(_deadColor, _aliveColor, (float)CurrentHealth / _maxHealth) : _deadColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float sinT = Mathf.Sin(t * Mathf.PI);

                // Stronger Squash/Stretch scale bounce (squash short/fat, stretch tall/thin, then normalize)
                float scaleY = 1f - sinT * 0.45f;
                float scaleXZ = 1f + sinT * 0.35f;
                transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

                // Knockback slide
                transform.position = startPos + knockback * sinT;

                // Hit color flash
                if (_bodyRenderer == null)
                    _bodyRenderer = GetComponentInChildren<Renderer>();

                if (_bodyRenderer != null)
                {
                    _bodyRenderer.material.color = Color.Lerp(flashColor, normalColor, t);
                }

                yield return null;
            }

            transform.localScale = Vector3.one;
            transform.position = startPos;
            ApplyColor();
            _hitAnimCoroutine = null;
        }

        private void Die()
        {
            if (_hitAnimCoroutine != null)
            {
                StopCoroutine(_hitAnimCoroutine);
                _hitAnimCoroutine = null;
            }
            transform.localScale = Vector3.one;

            _isDead = true;
            _totalKills++;
            _respawnTimer = _respawnDelay;

            Debug.Log($"[TEST][DUMMY] ¡MUERTO! Total kills: {_totalKills}");
            transform.localRotation = Quaternion.Euler(90, 0, 0); // cae de espaldas
            ApplyColor();

            OnDummyKilled?.Invoke(_totalKills);
        }

        private void Respawn()
        {
            _isDead = false;
            CurrentHealth = _maxHealth;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            ApplyColor();
            if (_displayView != null)
            {
                _displayView.SetVisible(true);
                _displayView.SetHealth(1f);
            }
            Debug.Log($"[TEST][DUMMY] Respawneado. HP={CurrentHealth}");
        }

        private void ApplyColor()
        {
            if (_bodyRenderer == null)
                _bodyRenderer = GetComponentInChildren<Renderer>();

            if (_bodyRenderer != null)
            {
                _bodyRenderer.material.color = IsAlive ? Color.Lerp(_deadColor, _aliveColor, (float)CurrentHealth / _maxHealth) : _deadColor;
            }
        }
    }
}
