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

        [Header("Refs (assigned by the Link tool: the scene match controller)")]
        [SerializeField] private MatchNetworkController _matchNetwork;

        // Networked source of truth. OnChanged hook updates the HUD on every client.
        [Networked] public int CurrentHealth { get; set; }

        public bool IsAlive => CurrentHealth > 0;

        public override void Spawned()
        {
            // TODO (other agent): if (Object.HasStateAuthority) CurrentHealth = _maxHealth;
        }

        public void TakeDamage(int amount, PlayerRef attacker)
        {
            // REQUIRED LOG -> "El jugador B recibio el impacto"
            RedesLog.Info(RedesLog.COMBAT, $"El jugador {Object.InputAuthority} recibio el impacto");

            // TODO (other agent): only the State Authority mutates CurrentHealth.
            // CurrentHealth -= amount;

            if (!IsAlive)
            {
                // REQUIRED LOG -> "El jugador A Perdio"
                RedesLog.Info(RedesLog.MATCH, $"El jugador {Object.InputAuthority} Perdio");

                // TODO (other agent): announce to everyone who lost (and thus who won):
                // _matchNetwork.AnnounceResult(loser: Object.InputAuthority, winner: attacker);
            }
        }
    }
}
