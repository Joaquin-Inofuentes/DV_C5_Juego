using Fusion;
using UnityEngine;
using Redes.Core;
using Redes.Combat;
using Redes.Gameplay;

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
        [Networked, OnChangedRender(nameof(OnHealthChangedRender))] public int CurrentHealth { get; set; }

        public bool IsAlive => CurrentHealth > 0;

        public event System.Action<int> OnHealthChanged;

        private void OnHealthChangedRender()
        {
            RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [IN] OnHealthChangedRender: Player {Object.InputAuthority} (LocalPlayer={Runner.LocalPlayer}) health updated to {CurrentHealth}");
            try
            {
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

            if (!Object.HasStateAuthority)
            {
                RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: [OUT] TakeDamage skipped since this client is NOT StateAuthority.");
                return;
            }

            try
            {
                CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
                RedesLog.Info(RedesLog.COMBAT, $">> PlayerHealth: CurrentHealth updated to {CurrentHealth}");
                
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
