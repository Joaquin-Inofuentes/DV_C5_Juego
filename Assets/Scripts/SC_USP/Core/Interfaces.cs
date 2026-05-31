using UnityEngine;

namespace USP.Core
{
    public interface IHealth
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
        void TakeDamage(float amount, GameObject attacker);
    }

    public interface IMovable
    {
        float Speed { get; }
        void MoveTo(Vector3 position);
        void Stop();
    }

    public interface IAttackable
    {
        void Attack();
        bool CanAttack { get; }
    }

    public interface IInputProvider
    {
        Vector2 GetMovementInput();
        bool GetShootInput();
    }
}
