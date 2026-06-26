using UnityEngine;
using System.Collections;
using Redes.Core;

namespace Redes.Combat
{
    /// <summary>
    /// Component purely for visual feedback (squash & stretch) when an obstacle is hit by a projectile.
    /// </summary>
    public class ObstacleHitFeedback : MonoBehaviour
    {
        [Header("Bounce Settings")]
        [SerializeField] private float _bounceDuration = 0.2f;
        [SerializeField] private float _bounceScaleFactor = 1.2f;

        private Vector3 _originalScale;
        private Coroutine _bounceCoroutine;
        private Transform _visualTransform;

        private void Awake()
        {
            _visualTransform = transform;
            _originalScale = _visualTransform.localScale;
        }

        public void DoBounce()
        {
            if (!gameObject.activeInHierarchy) return;

            RedesLog.Info(RedesLog.VFX, $"[OBSTACLE FEEDBACK] Ejecutando rebote de escala en {gameObject.name}");
            
            if (_bounceCoroutine != null)
            {
                StopCoroutine(_bounceCoroutine);
                _visualTransform.localScale = _originalScale;
            }
            _bounceCoroutine = StartCoroutine(BounceRoutine());
        }

        private IEnumerator BounceRoutine()
        {
            float halfDuration = _bounceDuration / 2f;
            float elapsed = 0f;

            Vector3 targetScale = _originalScale * _bounceScaleFactor;

            // Scale up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                _visualTransform.localScale = Vector3.Lerp(_originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            // Scale down
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                _visualTransform.localScale = Vector3.Lerp(targetScale, _originalScale, t);
                yield return null;
            }

            _visualTransform.localScale = _originalScale;
            _bounceCoroutine = null;
        }
    }
}
