using UnityEngine;
using Redes.Player;

namespace Redes.Views
{
    public class DeathScreenController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DeathScreenView _view;
        [SerializeField] private PlayerEventBus _playerEventBus;

        private float _deathDuration = 5f;
        private float _elapsed;
        private bool _isDead;

        private void OnEnable()
        {
            if (_playerEventBus != null)
            {
                _playerEventBus.OnDied += HandleDied;
                _playerEventBus.OnSpawned += HandleSpawned;
            }

            DebugSystem.EventBus.OnPlayerDeath += HandleGlobalDied;
            DebugSystem.EventBus.OnRespawnExecuted += HandleGlobalSpawned;
        }

        private void OnDisable()
        {
            if (_playerEventBus != null)
            {
                _playerEventBus.OnDied -= HandleDied;
                _playerEventBus.OnSpawned -= HandleSpawned;
            }

            DebugSystem.EventBus.OnPlayerDeath -= HandleGlobalDied;
            DebugSystem.EventBus.OnRespawnExecuted -= HandleGlobalSpawned;
        }

        private void HandleDied(Vector3 hitDirection)
        {
            StartDeathCountdown(10f);
        }

        private void HandleSpawned()
        {
            StopDeathCountdown();
        }

        private void HandleGlobalDied(int actorId, int killerId, string cause)
        {
            if (actorId == DebugSystem.LocalNetworkMock.LocalActorID)
            {
                StartDeathCountdown(10f);
            }
        }

        private void HandleGlobalSpawned()
        {
            StopDeathCountdown();
        }

        private void StartDeathCountdown(float duration)
        {
            _deathDuration = duration;
            _elapsed = 0f;
            _isDead = true;
            if (_view != null)
            {
                _view.SetVisible(true);
                _view.UpdateProgress(0f, _deathDuration);
            }
        }

        private void StopDeathCountdown()
        {
            _isDead = false;
            if (_view != null)
            {
                _view.SetVisible(false);
            }
        }

        private void Update()
        {
            if (_isDead)
            {
                _elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(_elapsed / _deathDuration);
                float secondsLeft = Mathf.Max(0f, _deathDuration - _elapsed);
                if (_view != null)
                {
                    _view.UpdateProgress(progress, secondsLeft);
                }
            }
        }
    }
}
