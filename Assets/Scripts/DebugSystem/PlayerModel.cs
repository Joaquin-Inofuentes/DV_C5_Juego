using System;
using UnityEngine;

namespace DebugSystem
{
    public class PlayerModel : MonoBehaviour
    {
        public int ActorID = 1;
        public string Username = "Player1";
        
        [Header("Stats")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float currentHP = 100f;
        [SerializeField] private float shield = 0f;

        public float MaxHP => maxHP;
        public float CurrentHP => currentHP;
        public float Shield => shield;

        public event Action<float, float> OnHealthChanged;

        public void Initialize(int actorID, string username)
        {
            ActorID = actorID;
            Username = username;
            currentHP = maxHP;
            shield = 0f;
        }

        public void ApplyDamage(float amount, int attackerID)
        {
            if (currentHP <= 0) return;

            float beforeHP = currentHP;
            currentHP = Mathf.Max(0f, currentHP - amount);
            
            EventBus.TriggerDamageApplied(ActorID, amount, beforeHP, currentHP, attackerID);
            EventBus.TriggerHealthSynced(ActorID, currentHP, shield);
            OnHealthChanged?.Invoke(currentHP, shield);

            if (currentHP <= 0)
            {
                EventBus.TriggerPlayerDeath(ActorID, attackerID, "Pistol");
            }
        }

        public void Heal(float amount, int healerID)
        {
            if (currentHP <= 0) return;

            currentHP = Mathf.Min(maxHP, currentHP + amount);
            EventBus.TriggerHealReceived(ActorID, amount, healerID);
            EventBus.TriggerHealthSynced(ActorID, currentHP, shield);
            OnHealthChanged?.Invoke(currentHP, shield);
        }
    }
}
