using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Redes.Player;
using Redes.Core;

namespace Redes.Combat
{
    /// <summary>
    /// Network-enabled combustible bomb obstacle (3D Cube).
    /// </summary>
    public class DestructibleBombObstacle : NetworkBehaviour, IDamageable
    {
        [Header("Explosion Tuning")]
        [SerializeField] private int _bombHealth = 1;
        [SerializeField] private int _explosionDamage = 50;
        [SerializeField] private float _explosionRadius = 4f;
        [SerializeField] private AudioClip _explosionSound;

        [Header("Components")]
        [SerializeField] private Collider _boxCollider; 
        [SerializeField] private SphereCollider _explosionTrigger; 
        [SerializeField] private ParticleSystem _fireParticles;
        [SerializeField] private ParticleSystem _explosionParticles;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Transform _visualCube;

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
        
        // UI Radial
        private Canvas _chargeCanvas;
        private Image _chargeImage;

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

            if (_explosionTrigger != null)
            {
                _explosionTrigger.enabled = false;
                _explosionTrigger.isTrigger = true;
                _explosionTrigger.radius = _explosionRadius;
            }
            
            SetupChargeUI();
        }

        private void OnEnable()
        {
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [IN] OnEnable en {gameObject.name}");
            if (_fireParticles != null) _fireParticles.Stop();
            if (_explosionParticles != null) _explosionParticles.Stop();
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [OUT] OnEnable completado en {gameObject.name}");
        }

        private void SetupChargeUI()
        {
            if (_chargeCanvas != null) return;
            
            GameObject canvasObj = new GameObject("ChargeCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0); 
            
            _chargeCanvas = canvasObj.AddComponent<Canvas>();
            _chargeCanvas.renderMode = RenderMode.WorldSpace;
            
            var rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, 2);
            
            GameObject imgObj = new GameObject("ChargeImage");
            imgObj.transform.SetParent(canvasObj.transform, false);
            
            _chargeImage = imgObj.AddComponent<Image>();
            _chargeImage.type = Image.Type.Filled;
            _chargeImage.fillMethod = Image.FillMethod.Radial360;
            _chargeImage.fillAmount = 0;
            _chargeImage.color = new Color(1f, 0.5f, 0f, 0.8f); 
            
            var imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.sizeDelta = new Vector2(1.5f, 1.5f);
            
            canvasObj.SetActive(false);
        }

        public override void Spawned()
        {
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [IN] Spawned. ObjetoId: {Object.Id}, HasStateAuthority: {Object.HasStateAuthority}");
            if (Object.HasStateAuthority)
            {
                _currentHealth = _bombHealth;
                CurrentState = BombState.Normal;
            }
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [OUT] Spawned completado. Estado inicial: {CurrentState}");
        }

        public void TakeDamage(int amount, PlayerRef attacker)
        {
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [IN] TakeDamage. Recibido de: {attacker}. Autoridad local: {Object.InputAuthority}. HasStateAuthority: {Object.HasStateAuthority}");
            if (!Object.HasStateAuthority) return;
            if (CurrentState != BombState.Normal) return;

            _currentHealth -= amount;
            if (_currentHealth <= 0)
            {
                RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] Bomb destruida por {attacker}. Cambiando estado a OnFire (StateAuth: {Object.StateAuthority}).");
                CurrentState = BombState.OnFire;
                StartCoroutine(ServerOnFireTimer());
            }
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [OUT] TakeDamage completado.");
        }

        private IEnumerator ServerOnFireTimer()
        {
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [IN] ServerOnFireTimer iniciado en el Servidor (2 segundos).");
            yield return new WaitForSeconds(2.0f);
            
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] ServerOnFireTimer finalizado. Cambiando estado a Exploded.");
            CurrentState = BombState.Exploded;

            if (_explosionTrigger != null)
            {
                _explosionTrigger.enabled = true;
                RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] Collider de explosion activado. Radio: {_explosionTrigger.radius}");
            }

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [OUT] ServerOnFireTimer - Despawneando objeto.");
            Runner.Despawn(Object);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Object.HasStateAuthority) return;
            if (CurrentState != BombState.Exploded) return;

            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [IN] OnTriggerEnter de Explosión colisionó con {other.name}");
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                var player = other.GetComponentInParent<Redes.Player.NetworkPlayer>();
                if (player != null)
                {
                    damageable.TakeDamage(_explosionDamage, PlayerRef.None);
                    RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] ¡Explosión dañó al jugador {player.Object.InputAuthority} por {_explosionDamage} de daño!");
                }
            }
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [OUT] OnTriggerEnter completado para {other.name}");
        }

        private void OnStateChangedRender()
        {
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [IN] OnStateChangedRender. Nuevo estado: {CurrentState} en el cliente (LocalPlayer: {Runner.LocalPlayer})");
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
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] [OUT] OnStateChangedRender completado.");
        }

        private void ResetObstacle()
        {
            RedesLog.Info(RedesLog.COMBAT, $"[BOMB_OBSTACLE] ResetObstacle visuales iniciados.");
            if (_boxCollider != null) _boxCollider.enabled = true;
            if (_explosionTrigger != null) _explosionTrigger.enabled = false;
            if (_fireParticles != null) _fireParticles.Stop();
            if (_explosionParticles != null) _explosionParticles.Stop();
            if (_visualCube != null) _visualCube.localScale = _originalScale;
            if (_chargeCanvas != null) _chargeCanvas.gameObject.SetActive(false);
        }

        private void StartOnFireVisuals()
        {
            RedesLog.Info(RedesLog.VFX, $"[BOMB_OBSTACLE] StartOnFireVisuals: Activando particulas de fuego y efecto radial.");
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
            float speed = 15f; 

            Transform targetTransform = _visualCube != null ? _visualCube : transform;

            if (_chargeCanvas != null)
            {
                _chargeCanvas.gameObject.SetActive(true);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Animación de escala (bounce)
                float factor = 1.1f + Mathf.Sin(elapsed * speed) * 0.2f;
                targetTransform.localScale = _originalScale * factor;

                // UI Radial
                if (_chargeImage != null)
                {
                    _chargeImage.fillAmount = progress;
                }
                if (_chargeCanvas != null && Camera.main != null)
                {
                    _chargeCanvas.transform.rotation = Quaternion.LookRotation(_chargeCanvas.transform.position - Camera.main.transform.position);
                }

                yield return null;
            }

            targetTransform.localScale = _originalScale;
            if (_chargeCanvas != null) _chargeCanvas.gameObject.SetActive(false);
        }

        private void TriggerExplosionVisuals()
        {
            RedesLog.Info(RedesLog.VFX, $"[BOMB_OBSTACLE] TriggerExplosionVisuals: Liberando particulas y reproduciendo audio.");
            if (_fireLerpCoroutine != null)
            {
                StopCoroutine(_fireLerpCoroutine);
            }
            
            if (_chargeCanvas != null) _chargeCanvas.gameObject.SetActive(false);

            Transform targetTransform = _visualCube != null ? _visualCube : transform;
            targetTransform.localScale = _originalScale;

            if (_fireParticles != null) _fireParticles.Stop();

            if (_visualCube != null) _visualCube.gameObject.SetActive(false);
            if (_boxCollider != null) _boxCollider.enabled = false;

            if (_audioSource != null && _explosionSound != null)
            {
                _audioSource.PlayOneShot(_explosionSound);
            }
            else if (_explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(_explosionSound, transform.position);
            }

            if (_explosionParticles != null)
            {
                _explosionParticles.transform.SetParent(null); 
                _explosionParticles.gameObject.SetActive(true);
                _explosionParticles.Play();
                Destroy(_explosionParticles.gameObject, 4.0f);
            }

            StartCoroutine(GreenExplosionSphereRoutine());
        }

        private IEnumerator GreenExplosionSphereRoutine()
        {
            RedesLog.Info(RedesLog.VFX, $"[BOMB_OBSTACLE] Iniciando esfera verde radial de explosión en {transform.position}. Target Radius={_explosionRadius}");
            GameObject greenSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            greenSphere.transform.position = transform.position;
            
            Collider col = greenSphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = greenSphere.GetComponent<Renderer>();
            Material greenMat = new Material(Shader.Find("Standard"));
            greenMat.color = new Color(0f, 1f, 0f, 0.5f); 
            
            greenMat.SetFloat("_Mode", 3);
            greenMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            greenMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            greenMat.SetInt("_ZWrite", 0);
            greenMat.DisableKeyword("_ALPHATEST_ON");
            greenMat.EnableKeyword("_ALPHABLEND_ON");
            greenMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            greenMat.renderQueue = 3000;
            rend.material = greenMat;

            float duration = 0.5f; 
            float elapsed = 0f;
            // The size of the sphere primitive is 1. To match a radius of _explosionRadius, we scale by _explosionRadius * 2.
            float maxScale = _explosionRadius * 2f; 
            
            RedesLog.Info(RedesLog.VFX, $"[BOMB_OBSTACLE] Tamaño máximo de la esfera verde visual será: {maxScale}");

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                float currentScale = Mathf.Lerp(0.1f, maxScale, Mathf.Sin(progress * Mathf.PI));
                greenSphere.transform.localScale = Vector3.one * currentScale;

                Color c = greenMat.color;
                c.a = Mathf.Lerp(0.5f, 0f, progress);
                greenMat.color = c;

                yield return null;
            }

            Destroy(greenSphere);
        }
    }
}
