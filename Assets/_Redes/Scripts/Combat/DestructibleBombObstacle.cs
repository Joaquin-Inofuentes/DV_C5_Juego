using Fusion;
using UnityEngine;
using System.Collections;
using Redes.Player;
using Redes.Core;

namespace Redes.Combat
{
    /// <summary>
    /// Network-enabled combustible bomb obstacle (3D Cube).
    /// Phases:
    /// 1. Normal: Box collider active, handles regular obstacle collision/blocking.
    ///    When shot/damaged, it catches fire.
    /// 2. Catching Fire (Incendiándose): For 2 seconds, repeats scale LERP.
    /// 3. Explosion: Plays explosion sound (SFX_Bomba.mp3), triggers a particle system,
    ///    activates a double-sized SphereCollider trigger to damage nearby entities, then deactivates/despawns.
    /// </summary>
    public class DestructibleBombObstacle : NetworkBehaviour, IDamageable
    {
        [Header("Explosion Tuning")]
        [SerializeField] private int _bombHealth = 1;
        [SerializeField] private int _explosionDamage = 50;
        [SerializeField] private float _explosionRadius = 4f; // Double of typical size (box fits 1x1x1, sphere is radius 2 = diameter 4)
        [SerializeField] private AudioClip _explosionSound;

        [Header("Components")]
        [SerializeField] private Collider _boxCollider; // Solid blocker
        [SerializeField] private SphereCollider _explosionTrigger; // Trigger for damage
        [SerializeField] private ParticleSystem _fireParticles;
        [SerializeField] private ParticleSystem _explosionParticles;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Transform _visualCube;

        // FSM States
        public enum BombState
        {
            Normal,
            OnFire,
            Exploded
        }

        [Networked, OnChangedRender(nameof(OnStateChangedRender))]
        public BombState CurrentState { get; set; }

        [Networked]
        private int _currentHealth { get; set; }

        public bool IsAlive => CurrentState == BombState.Normal;

        private Vector3 _originalScale;
        private Coroutine _fireLerpCoroutine;

        private void Awake()
        {
            if (_visualCube != null)
            {
                _originalScale = _visualCube.localScale;
            }
            else
            {
                _originalScale = transform.localScale;
            }

            // Make sure the explosion trigger starts disabled
            if (_explosionTrigger != null)
            {
                _explosionTrigger.enabled = false;
                _explosionTrigger.isTrigger = true;
                _explosionTrigger.radius = _explosionRadius;
            }
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                _currentHealth = _bombHealth;
                CurrentState = BombState.Normal;
            }
        }

        public void TakeDamage(int amount, PlayerRef attacker)
        {
            if (!Object.HasStateAuthority) return;
            if (CurrentState != BombState.Normal) return;

            _currentHealth -= amount;
            if (_currentHealth <= 0)
            {
                CurrentState = BombState.OnFire;
                // Start fire stage timer (2 seconds) on server before triggering explosion
                StartCoroutine(ServerOnFireTimer());
            }
        }

        private IEnumerator ServerOnFireTimer()
        {
            yield return new WaitForSeconds(2.0f);
            
            // Trigger explosion state
            CurrentState = BombState.Exploded;

            // Enable sphere collider to detect and damage players
            if (_explosionTrigger != null)
            {
                _explosionTrigger.enabled = true;
            }

            // Give a short physical frame window for the trigger overlap to register
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // Despawn/disable
            Runner.Despawn(Object);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only damage on state authority during explosion phase
            if (!Object.HasStateAuthority) return;
            if (CurrentState != BombState.Exploded) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                // Verify it's a player and not hitting self
                var player = other.GetComponentInParent<Redes.Player.NetworkPlayer>();
                if (player != null)
                {
                    damageable.TakeDamage(_explosionDamage, PlayerRef.None);
                    Debug.Log($"[BOMB EXPLOSION] Damaged player {player.Object.InputAuthority} for {_explosionDamage} damage.");
                }
            }
        }

        private void OnStateChangedRender()
        {
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB] Estado cambiado a {CurrentState} en {transform.position}\nStack Trace:\n{System.Environment.StackTrace}");
            switch (CurrentState)
            {
                case BombState.Normal:
                    ResetObstacle();
                    break;
                case BombState.OnFire:
                    StartOnFireVisuals();
                    break;
                case BombState.Exploded:
                    TriggerExplosionVisuals();
                    break;
            }
        }

        private void ResetObstacle()
        {
            if (_boxCollider != null) _boxCollider.enabled = true;
            if (_explosionTrigger != null) _explosionTrigger.enabled = false;
            if (_fireParticles != null) _fireParticles.Stop();
            if (_explosionParticles != null) _explosionParticles.Stop();
            if (_visualCube != null) _visualCube.localScale = _originalScale;
        }

        private void StartOnFireVisuals()
        {
            if (_fireParticles != null)
            {
                _fireParticles.Play();
            }

            if (_fireLerpCoroutine != null)
            {
                StopCoroutine(_fireLerpCoroutine);
            }
            _fireLerpCoroutine = StartCoroutine(IncendiandoLerpEffect());
        }

        private IEnumerator IncendiandoLerpEffect()
        {
            float duration = 2.0f;
            float elapsed = 0f;
            float speed = 15f; // Oscillate frequency

            Transform targetTransform = _visualCube != null ? _visualCube : transform;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Oscillate scale between 90% and 130%
                float factor = 1.1f + Mathf.Sin(elapsed * speed) * 0.2f;
                targetTransform.localScale = _originalScale * factor;
                yield return null;
            }

            targetTransform.localScale = _originalScale;
        }

        private void TriggerExplosionVisuals()
        {
            if (_fireLerpCoroutine != null)
            {
                StopCoroutine(_fireLerpCoroutine);
            }

            Transform targetTransform = _visualCube != null ? _visualCube : transform;
            targetTransform.localScale = _originalScale;

            if (_fireParticles != null) _fireParticles.Stop();

            // Disable visual cube and box collider immediately so it looks like it exploded
            if (_visualCube != null) _visualCube.gameObject.SetActive(false);
            if (_boxCollider != null) _boxCollider.enabled = false;

            // Play sound
            if (_audioSource != null && _explosionSound != null)
            {
                _audioSource.PlayOneShot(_explosionSound);
            }
            else if (_explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(_explosionSound, transform.position);
            }

            // Play particles
            if (_explosionParticles != null)
            {
                _explosionParticles.transform.SetParent(null); // Detach so they don't get destroyed immediately
                _explosionParticles.gameObject.SetActive(true);
                _explosionParticles.Play();
                Destroy(_explosionParticles.gameObject, 4.0f);
            }

            // Create Green Explosion Sphere (visual only)
            StartCoroutine(GreenExplosionSphereRoutine());
        }

        private IEnumerator GreenExplosionSphereRoutine()
        {
            RedesLog.Info(RedesLog.VFX, $"[BOMB] Iniciando esfera verde radial de explosion en {transform.position}");
            GameObject greenSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            greenSphere.transform.position = transform.position;
            
            // Remove collider so it doesn't block physics
            Collider col = greenSphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Setup green transparent material
            Renderer rend = greenSphere.GetComponent<Renderer>();
            Material greenMat = new Material(Shader.Find("Standard"));
            greenMat.color = new Color(0f, 1f, 0f, 0.5f); // Semi-transparent green
            // Set material to Transparent mode
            greenMat.SetFloat("_Mode", 3);
            greenMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            greenMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            greenMat.SetInt("_ZWrite", 0);
            greenMat.DisableKeyword("_ALPHATEST_ON");
            greenMat.EnableKeyword("_ALPHABLEND_ON");
            greenMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            greenMat.renderQueue = 3000;
            rend.material = greenMat;

            float duration = 0.5f; // Fast explosion
            float elapsed = 0f;
            float maxScale = _explosionRadius * 2f; // Scale to match damage radius

            // Expand and pulse
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Scale up quickly, then shrink a bit (pulse effect)
                float currentScale = Mathf.Lerp(0.1f, maxScale, Mathf.Sin(progress * Mathf.PI));
                greenSphere.transform.localScale = Vector3.one * currentScale;

                // Fade out
                Color c = greenMat.color;
                c.a = Mathf.Lerp(0.5f, 0f, progress);
                greenMat.color = c;

                yield return null;
            }

            Destroy(greenSphere);
        }
    }
}
