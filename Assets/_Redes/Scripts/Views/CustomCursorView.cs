using UnityEngine;
using UnityEngine.UI;
using Redes.Player;

namespace Redes.Views
{
    /// <summary>
    /// Custom Cursor View following MVC/MVP pattern.
    /// Handles custom cursor graphics, scaling/rebound animations on shoot,
    /// target/hit color changes, radial loading indicator on reload,
    /// and ensures the custom cursor is only active when the mouse is within the game window/screen.
    /// </summary>
    public class CustomCursorView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEventBus _eventBus;
        
        [Header("Cursor Sprites")]
        [SerializeField] private Sprite _cursorBase;
        [SerializeField] private Sprite _cursorShoot;
        [SerializeField] private Sprite _cursorHit;
        [SerializeField] private Sprite _cursorReload;

        [Header("Cursor Settings")]
        public float CursorSize = 96f;

        private Canvas _canvas;
        private RectTransform _cursorRect;
        private Image _cursorImage;
        private Image _reloadProgressImage;

        // Animation / State variables
        private float _reloadTimer;
        private float _reloadDuration = 1.5f;
        private bool _isReloading;
        private float _shootReboundScale = 1f;
        private float _hitScale = 1f;
        private bool _isHitting;
        private float _hitTimer;

        private void Awake()
        {
            // Create the Cursor UI element dynamically under this transform (which should be the Canvas)
            _canvas = GetComponentInParent<Canvas>();
            
            GameObject cursorGo = new GameObject("CustomCursor", typeof(RectTransform));
            cursorGo.transform.SetParent(transform, false);
            _cursorRect = cursorGo.GetComponent<RectTransform>();
            _cursorRect.sizeDelta = new Vector2(CursorSize, CursorSize);
            _cursorRect.pivot = new Vector2(0.5f, 0.5f);
            
            _cursorImage = cursorGo.AddComponent<Image>();
            _cursorImage.sprite = _cursorBase;
            _cursorImage.raycastTarget = false;

            // Create reloading radial sub-image
            GameObject reloadGo = new GameObject("RadialProgress", typeof(RectTransform));
            reloadGo.transform.SetParent(cursorGo.transform, false);
            var reloadRect = reloadGo.GetComponent<RectTransform>();
            reloadRect.anchorMin = Vector2.zero;
            reloadRect.anchorMax = Vector2.one;
            reloadRect.sizeDelta = Vector2.zero;

            _reloadProgressImage = reloadGo.AddComponent<Image>();
            _reloadProgressImage.sprite = _cursorReload;
            _reloadProgressImage.type = Image.Type.Filled;
            _reloadProgressImage.fillMethod = Image.FillMethod.Radial360;
            _reloadProgressImage.fillOrigin = (int)Image.Origin360.Top;
            _reloadProgressImage.fillClockwise = true;
            _reloadProgressImage.color = Color.yellow;
            _reloadProgressImage.raycastTarget = false;
            _reloadProgressImage.gameObject.SetActive(false);

        }

        private void OnEnable()
        {
            TryBindLocalPlayer();
        }

        private void OnDisable()
        {
            UnbindEventBus();
        }

        private void TryBindLocalPlayer()
        {
            if (_eventBus != null) return;

            // Search for local network player
            var netPlayers = FindObjectsByType<Redes.Player.NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var np in netPlayers)
            {
                if (np.Object != null && np.Object.IsValid && np.Object.HasInputAuthority)
                {
                    _eventBus = np.GetComponent<PlayerEventBus>();
                    break;
                }
            }

            // Fallback for offline play tester
            if (_eventBus == null)
            {
                var tester = FindFirstObjectByType<Redes.Test.OfflinePlayerTester>();
                if (tester != null)
                {
                    _eventBus = tester.GetComponent<PlayerEventBus>();
                }
            }

            if (_eventBus != null)
            {
                _eventBus.OnShoot += HandleShoot;
                _eventBus.OnReload += HandleReload;
                _eventBus.OnAmmoChanged += HandleAmmoChanged;
                _eventBus.OnTookDamage += HandleTookDamage;
            }
        }

        private void UnbindEventBus()
        {
            if (_eventBus != null)
            {
                _eventBus.OnShoot -= HandleShoot;
                _eventBus.OnReload -= HandleReload;
                _eventBus.OnAmmoChanged -= HandleAmmoChanged;
                _eventBus.OnTookDamage -= HandleTookDamage;
                _eventBus = null;
            }
        }

        // Exposed public method to trigger hit effect from OfflinePlayerTester/DummyEnemy collisions
        public void TriggerHit()
        {
            _isHitting = true;
            _hitTimer = 0.2f;
            _hitScale = 1.4f;
        }

        private void HandleShoot(float speed)
        {
            if (!_isReloading)
            {
                _shootReboundScale = 1.5f; // Scale bounce on shoot
            }
        }

        private void HandleReload()
        {
            _isReloading = true;
            _reloadTimer = _reloadDuration;
            if (_reloadProgressImage != null)
            {
                _reloadProgressImage.gameObject.SetActive(true);
                _reloadProgressImage.fillAmount = 1f;
            }
        }

        private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
        {
            if (currentAmmo == maxAmmo && _isReloading)
            {
                // Reload finished
                _isReloading = false;
                if (_reloadProgressImage != null)
                {
                    _reloadProgressImage.gameObject.SetActive(false);
                }
            }
        }

        private void HandleTookDamage(int attackerId, GameObject bullet)
        {
            // Player got hit, can shake or react if desired
        }

        private void Update()
        {
            TryBindLocalPlayer();

            // 1. Only show cursor when within game window bounds
            Vector3 mousePos = Input.mousePosition;
            bool insideWindow = mousePos.x >= 0 && mousePos.x <= Screen.width &&
                                mousePos.y >= 0 && mousePos.y <= Screen.height;

            if (insideWindow)
            {
                _cursorImage.enabled = true;
                if (_isReloading && _reloadProgressImage != null)
                    _reloadProgressImage.enabled = true;
            }
            else
            {
                _cursorImage.enabled = false;
                if (_reloadProgressImage != null)
                    _reloadProgressImage.enabled = false;
                return;
            }

            if (_cursorRect != null)
            {
                _cursorRect.sizeDelta = new Vector2(CursorSize, CursorSize);
            }

            // 2. Position custom cursor element matching mouse position
            Vector2 localPoint;
            var parentRect = transform as RectTransform;
            if (parentRect == null) parentRect = GetComponent<RectTransform>();
            if (parentRect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                mousePos,
                _canvas != null ? _canvas.worldCamera : null,
                out localPoint
            );
            _cursorRect.anchoredPosition = localPoint;

            // 3. Handle Reload radial fill animation
            if (_isReloading)
            {
                _reloadTimer -= Time.deltaTime;
                if (_reloadTimer <= 0f)
                {
                    _reloadTimer = 0f;
                    _isReloading = false;
                    _reloadProgressImage.gameObject.SetActive(false);
                }
                else if (_reloadProgressImage != null)
                {
                    // Clockwise 360 radial loading indicator
                    _reloadProgressImage.fillAmount = 1f - (_reloadTimer / _reloadDuration);
                }
            }

            // 4. Handle hit state timers
            if (_isHitting)
            {
                _hitTimer -= Time.deltaTime;
                if (_hitTimer <= 0f)
                {
                    _isHitting = false;
                }
            }

            // 5. Select correct sprite & color based on priority: Hit -> Reload -> Shoot (momentary) -> Base
            if (_isHitting)
            {
                _cursorImage.sprite = _cursorHit;
                _cursorImage.color = Color.red;
                _hitScale = Mathf.MoveTowards(_hitScale, 1.0f, Time.deltaTime * 3f);
                _cursorRect.localScale = Vector3.one * _hitScale;
            }
            else if (_isReloading)
            {
                _cursorImage.sprite = _cursorBase;
                _cursorImage.color = Color.yellow;
                _cursorRect.localScale = Vector3.one;
            }
            else if (_shootReboundScale > 1f)
            {
                _cursorImage.sprite = _cursorShoot;
                _cursorImage.color = new Color(1f, 0.6f, 0f); // orange/yellow shoot tint
                _shootReboundScale = Mathf.MoveTowards(_shootReboundScale, 1f, Time.deltaTime * 5f);
                _cursorRect.localScale = Vector3.one * _shootReboundScale;
            }
            else
            {
                _cursorImage.sprite = _cursorBase;
                _cursorImage.color = Color.white;
                _cursorRect.localScale = Vector3.one;
            }
        }
    }
}
