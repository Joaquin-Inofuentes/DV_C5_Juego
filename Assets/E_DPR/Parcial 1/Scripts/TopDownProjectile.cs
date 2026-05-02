using Fusion;
using UnityEngine;

public class TopDownProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [Networked] public PlayerRef Owner { get; set; } // Cambiado a PlayerRef
    [Networked] public int DamageValue { get; set; }

    public void Initialize(PlayerRef owner, int damage)
    {
        Owner = owner;
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
            // Comparación directa de PlayerRef
            if (health.OwnerRef != Owner)
            {
                health.RPC_TakeDamage(DamageValue, Owner);
                Runner.Despawn(Object);
            }
        }
        else if (!other.CompareTag("Player"))
        {
            Runner.Despawn(Object);
        }
    }
}