using Fusion;
using UnityEngine;

public class TopDownProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 18f;
    [Networked] public int OwnerId { get; set; }
    [Networked] public int DamageValue { get; set; }

    public void Initialize(int ownerId, GameObject ownerObj, int damage)
    {
        OwnerId = ownerId;
        DamageValue = damage;
    }

    public override void FixedUpdateNetwork()
    {
        transform.position += transform.forward * speed * Runner.DeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.TryGetComponent(out TopDownPlayerHealth health))
        {
            if (health.PlayerNumber != OwnerId)
            {
                Debug.Log($"<color=yellow>[Bala] Impacto P{OwnerId} -> P{health.PlayerNumber}</color>");
                health.RPC_TakeDamage(DamageValue, OwnerId);
                Runner.Despawn(Object);
            }
        }
        else if (!other.CompareTag("Player"))
        {
            Runner.Despawn(Object);
        }
    }
}