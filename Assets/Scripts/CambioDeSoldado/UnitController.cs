using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{
    public NavMeshAgent agent;
    public float attackRange = 5f;
    public float stoppingDistance = 0.2f;

    [Header("Formación")]
    public static List<Transform> TakenSlots = new List<Transform>();
    public Transform currentSlot;

    private Transform currentEnemy;
    private Vector3 targetPoint;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Configuración para 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    public void MoveToPoint(Vector3 point)
    {
        ReleaseSlot();
        targetPoint = point;
        agent.isStopped = false;
        agent.SetDestination(point);
    }

    public void FollowLeader(List<Transform> allSlots)
    {
        if (currentSlot == null)
        {
            currentSlot = GetNearestFreeSlot(allSlots);
        }

        if (currentSlot != null)
        {
            agent.isStopped = false;
            agent.SetDestination(currentSlot.position);
        }
    }

    public void Attack(Transform enemy)
    {
        ReleaseSlot();
        currentEnemy = enemy;
        if (enemy == null) return;

        float dist = Vector2.Distance(transform.position, enemy.position);
        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(enemy.position);
        }
        else
        {
            agent.isStopped = true;
            // Aquí iría la lógica de instanciar bala
            Debug.Log($"[Acción] Disparando a {enemy.name}");
        }
    }

    public void Stop()
    {
        agent.isStopped = true;
    }

    public bool ReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= stoppingDistance;
    }

    private Transform GetNearestFreeSlot(List<Transform> allSlots)
    {
        Transform bestSlot = null;
        float minDist = Mathf.Infinity;

        foreach (Transform slot in allSlots)
        {
            if (TakenSlots.Contains(slot)) continue;

            float d = Vector2.Distance(transform.position, slot.position);
            if (d < minDist)
            {
                minDist = d;
                bestSlot = slot;
            }
        }

        if (bestSlot != null) TakenSlots.Add(bestSlot);
        return bestSlot;
    }

    public void ReleaseSlot()
    {
        if (currentSlot != null)
        {
            TakenSlots.Remove(currentSlot);
            currentSlot = null;
        }
    }

    public Vector3 GetTargetPoint() => targetPoint;
    public Transform GetEnemy() => currentEnemy;
}