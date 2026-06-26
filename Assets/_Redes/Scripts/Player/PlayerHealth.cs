using Fusion;
using UnityEngine;
using Redes.Core;
using Redes.Combat;
using Redes.Gameplay;
using Redes.Views;

namespace Redes.Player
{
    /// <summary>
    /// Player health. Implements IDamageable (SOLID/DIP) so projectiles damage it
    /// without knowing its concrete type.
    ///
    /// Health is [Networked] => the host is the source of truth, replicated to all
    /// clients (no desfase). When health hits 0 it asks the MatchNetworkController
    /// to announce the result to EVERYONE.
    /// Logic is implemented by another agent.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour, IDamageable
    {
        [Header("Tuning")]
        [SerializeField] private int _maxHealth = GameConstants.DEFAULT_MAX_HEALTH;

        [Header("Refs (assigned by the Prefab tool)")]
        [SerializeField] private GameEventBus _eventBus;

        // Networked source of truth. OnChanged hook updates the HUD on every client.
        // Networked source of truth. OnChanged hook updates the HUD on every client.
        [Networked, OnChangedRender(nameof(OnHealthChangedRender))] public int CurrentHealth { get; set; }
        [Networked] public Vector3 LastHitDirection { get; set; }

        public bool IsAlive => CurrentHealth > 0;

        public event System.Action<int> OnHealthChanged;

        [SerializeField] private AudioClip _hitSound;
        private PlayerEventBus _playerEventBus;
        private int _lastHealth = GameConstants.DEFAULT_MAX_HEALTH;

        private void Awake()
        {
            _playerEventBus = GetComponent<PlayerEventBus>();
        }

        private void OnHealthChangedRender()
        {
            bool isLocal = Object.HasInputAuthority;
            string localStr = isLocal ? "LOCAL" : "REMOTE";
            RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [IN] OnHealthChangedRender: Player {Object.InputAuthority} ({localStr}) (LocalPlayer={Runner.LocalPlayer}) health updated to {CurrentHealth}");
            try
            {
                if (CurrentHealth < _lastHealth)
                {
                    RedesLog.Info(RedesLog.VFX, $"[NET_HEALTH] Player {Object.InputAuthority} ({localStr}) took damage. Playing HitSFX+BloodBurstVFX. (Runs on ALL clients via [Networked] OnChangedRender)");
                    if (_hitSound != null)
                    {
                        AudioSource.PlayClipAtPoint(_hitSound, transform.position);
                        RedesLog.Info(RedesLog.VFX, $"[SFX] HitSound played at {transform.position} on client {Runner.LocalPlayer}");
                    }
                    if (Views.VFXManager.Instance != null)
                    {
                        Quaternion rot = LastHitDirection != Vector3.zero ? Quaternion.LookRotation(LastHitDirection) : Quaternion.identity;
                        Views.VFXManager.Instance.PlayHit(transform.position + Vector3.up, rot);
                        RedesLog.Info(RedesLog.VFX, $"[VFX] BloodBurst played at {transform.position + Vector3.up} on client {Runner.LocalPlayer}");
                    }
                }
                _lastHealth = CurrentHealth;

                OnHealthChanged?.Invoke(CurrentHealth);
                RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [OUT] OnHealthChangedRender completed successfully.");
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.COMBAT, $">> PlayerHealth: [ERROR] OnHealthChangedRender exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public override void Spawned()
        {
            RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [IN] Spawned: Player {Object.InputAuthority}");
            try
            {
                if (Object.HasStateAuthority)
                {
                    CurrentHealth = _maxHealth;
                    LastHitDirection = Vector3.up;
                    RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: Spawned initialized CurrentHealth to {CurrentHealth} on Server.");
                }
                OnHealthChanged?.Invoke(CurrentHealth);
                RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [OUT] Spawned completed successfully.");
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.COMBAT, $">> PlayerHealth: [ERROR] Spawned exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void TakeDamage(int amount, PlayerRef attacker)
        {
            RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [IN] TakeDamage: target={Object.InputAuthority}, amount={amount}, attacker={attacker}");
            
            // REQUIRED LOG -> "El jugador B recibio el impacto"
            RedesLog.Info(RedesLog.COMBAT, $"El jugador {Object.InputAuthority} recibio el impacto");

            if (_playerEventBus != null)
            {
                _playerEventBus.TriggerTookDamage(attacker.PlayerId, null);
            }

            if (!Object.HasStateAuthority)
            {
                RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [OUT] TakeDamage skipped since this client is NOT StateAuthority.");
                return;
            }

            // Calculate hit direction on State Authority
            Vector3 hitDirection = Vector3.up;
            if (Runner != null)
            {
                var attackerGo = Runner.GetPlayerObject(attacker);
                if (attackerGo == null)
                {
                    foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                    {
                        if (np.Object != null && np.Object.InputAuthority == attacker)
                        {
                            attackerGo = np.Object;
                            break;
                        }
                    }
                }
                if (attackerGo != null)
                {
                    hitDirection = (transform.position - attackerGo.transform.position).normalized;
                }
            }
            LastHitDirection = hitDirection;

            try
            {
                CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
                RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: CurrentHealth updated to {CurrentHealth}");
                
                // Notificamos al atacante que dio en el blanco (para el cursor)
                if (Runner != null)
                {
                    var attackerObj = Runner.GetPlayerObject(attacker);
                    if (attackerObj == null)
                    {
                        foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                        {
                            if (np.Object != null && np.Object.InputAuthority == attacker)
                            {
                                attackerObj = np.Object;
                                break;
                            }
                        }
                    }

                    if (attackerObj != null)
                    {
                        var attackerNetPlayer = attackerObj.GetComponent<NetworkPlayer>();
                        if (attackerNetPlayer != null)
                        {
                            Debug.Log($"[CURSOR_DEBUG] 3. Jugador {Object.InputAuthority} recibio daño. Llamando RpcNotifyHitMarker en atacante {attacker}");
                            attackerNetPlayer.RpcNotifyHitMarker();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CURSOR_DEBUG] No se encontro el NetworkPlayer del atacante {attacker} para disparar RpcNotifyHitMarker.");
                    }
                }

                if (_eventBus != null)
                {
                    RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [IN] TriggerPlayerTookDamage event bus");
                    _eventBus.TriggerPlayerTookDamage(Object.InputAuthority, CurrentHealth, attacker);
                    RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [OUT] TriggerPlayerTookDamage completed");
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.COMBAT, $">> PlayerHealth: [ERROR] Exception during TakeDamage calculations: {ex.Message}\n{ex.StackTrace}");
            }

            if (!IsAlive)
            {
                // REQUIRED LOG -> "El jugador A Perdio"
                RedesLog.Info(RedesLog.MATCH, $"El jugador {Object.InputAuthority} Perdio");
                
                if (_playerEventBus != null)
                {
                    _playerEventBus.TriggerDied();
                }

                try
                {
                    if (_eventBus != null)
                    {
                        RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [IN] TriggerPlayerDied event bus");
                        _eventBus.TriggerPlayerDied(Object.InputAuthority, attacker);
                        RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [OUT] TriggerPlayerDied completed");
                    }
                }
                catch (System.Exception ex)
                {
                    RedesLog.Error(RedesLog.COMBAT, $">> PlayerHealth: [ERROR] Exception during TriggerPlayerDied: {ex.Message}\n{ex.StackTrace}");
                }
            }
            
            RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [OUT] TakeDamage completed");
        }
    }
}
