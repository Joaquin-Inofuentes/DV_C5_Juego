using UnityEngine;
using USP.Core;
using USP.Entities;
using USP.Weapons;

namespace USP.Services
{
    public class MovementProxy
    {
        public static void MoveTo(IMovable unit, Vector3 target)
        {
            unit.MoveTo(target);
        }

        public static void Stop(IMovable unit)
        {
            unit.Stop();
        }
    }

    public class ShootingProxy
    {
        public static void Shoot(IAttackable unit)
        {
            if (unit.CanAttack)
            {
                unit.Attack();
            }
        }
    }

    public class AIProxy
    {
        public static bool CanSeeTarget(Transform agent, Transform target, float viewDistance, LayerMask obstacleMask)
        {
            if (target == null) return false;
            float dist = Vector3.Distance(agent.position, target.position);
            if (dist > viewDistance) return false;

            Vector3 direction = (target.position - agent.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(agent.position, direction, dist, obstacleMask);
            return hit.collider == null;
        }
    }

    public class UIProxy
    {
        public static void UpdateHealthBar(Transform bar, float current, float max)
        {
            if (bar != null && max > 0)
            {
                float pct = Mathf.Clamp01(current / max);
                bar.localScale = new Vector3(pct, 1f, 1f);
            }
        }
    }
}
