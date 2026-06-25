using UnityEngine;
using UnityEngine.UI;
using Redes.Player;

namespace Redes.Views
{
    /// <summary>
    /// UI View for ammunition.
    /// Listens to PlayerEventBus for ammo state and animates when shooting/reloading/empty.
    /// Completely follows the MVC pattern.
    /// </summary>
    public class PlayerAmmoView : MonoBehaviour
    {
        [SerializeField] private PlayerEventBus _eventBus;
        [SerializeField] private Text _ammoText;
        [SerializeField] private Slider _reloadSlider;

        private Vector3 _originalScale = Vector3.one;
        private Coroutine _animationCoroutine;
        private Color _normalColor = Color.white;
        private Color _emptyColor = Color.red;
        private RectTransform _rectTransform;

        // Offline reload logic mirror if needed, or update dynamically via Update
        private float _reloadTimer;
        private float _reloadDuration = 1.5f;
        private bool _isReloading;

        private void Awake()
        {
            if (_ammoText == null)
                _ammoText = GetComponent<Text>();
            _originalScale = transform.localScale;
            _rectTransform = GetComponent<RectTransform>();

            if (_reloadSlider != null)
            {
                _reloadSlider.gameObject.SetActive(false);
                _reloadSlider.minValue = 0f;
                _reloadSlider.maxValue = 1f;
                _reloadSlider.value = 0f;
            }
        }

        private void OnEnable()
        {
            if (_eventBus != null)
            {
                _eventBus.OnAmmoChanged += HandleAmmoChanged;
                _eventBus.OnReload += HandleReload;
            }
        }

        private void OnDisable()
        {
            if (_eventBus != null)
            {
                _eventBus.OnAmmoChanged -= HandleAmmoChanged;
                _eventBus.OnReload -= HandleReload;
            }
        }

        private void Update()
        {
            if (_isReloading)
            {
                _reloadTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_reloadTimer / _reloadDuration);
                if (_reloadSlider != null)
                {
                    _reloadSlider.value = progress;
                }

                if (_reloadTimer >= _reloadDuration)
                {
                    _isReloading = false;
                    if (_reloadSlider != null)
                    {
                        _reloadSlider.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
        {
            _isReloading = false;
            if (_reloadSlider != null)
            {
                _reloadSlider.gameObject.SetActive(false);
            }

            if (_ammoText != null)
            {
                _ammoText.text = $"AMMO: {currentAmmo}/{maxAmmo}";
                _ammoText.color = currentAmmo > 0 ? _normalColor : _emptyColor;
            }

            // Punch scale bounce on shoot
            if (currentAmmo > 0)
            {
                TriggerBounce(1.3f, 0.12f, _normalColor);
            }
            else
            {
                // Shake and flash red on empty
                TriggerBounce(1.4f, 0.22f, _emptyColor, true);
            }
        }

        private void HandleReload()
        {
            _isReloading = true;
            _reloadTimer = 0f;
            if (_reloadSlider != null)
            {
                _reloadSlider.gameObject.SetActive(true);
                _reloadSlider.value = 0f;
            }

            if (_ammoText != null)
            {
                _ammoText.text = "RELOADING...";
                _ammoText.color = Color.yellow;
            }
            TriggerBounce(1.2f, 0.25f, Color.yellow);
        }

        private void TriggerBounce(float targetScale, float duration, Color color, bool shake = false)
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(BounceCoroutine(targetScale, duration, color, shake));
        }

        private System.Collections.IEnumerator BounceCoroutine(float targetScale, float duration, Color color, bool shake)
        {
            float elapsed = 0f;
            if (_ammoText != null) _ammoText.color = color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Scale goes up and down
                float s = Mathf.Lerp(targetScale, 1f, t);
                transform.localScale = _originalScale * s;

                // Shake offset if empty (shake side to side)
                if (shake)
                {
                    float shakeOffset = Mathf.Sin(t * Mathf.PI * 6f) * 10f * (1f - t);
                    if (_rectTransform != null)
                        _rectTransform.anchoredPosition = new Vector2(shakeOffset, 0f);
                }
                else
                {
                    if (_rectTransform != null)
                        _rectTransform.anchoredPosition = Vector2.zero;
                }

                yield return null;
            }

            transform.localScale = _originalScale;
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = Vector2.zero;
            _animationCoroutine = null;
        }
    }
}
