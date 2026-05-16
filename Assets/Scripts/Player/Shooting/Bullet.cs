using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody3D))]
public class Bullet : NetworkBehaviour
{
    TickTimer _lifeTimer = TickTimer.None;

    [SerializeField] private byte _damage = 25;

    public override void Spawned()
    {
        //aplicar fuerzas al rigidbody
        

        //Crear el timer solo si se cumple una condicion
    }

    public override void FixedUpdateNetwork()
    {
        if (_lifeTimer.Expired(Runner))
        {
            DespawnObject();
        }
    }

    void DespawnObject()
    {
        _lifeTimer = TickTimer.None;
        
        Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        //retornar si el objeto no existe o no tiene la autoridad correspondiente

        //si colisiono con alguien que puede recibir daño aplico el daño
        
        
        DespawnObject();
    }
}
