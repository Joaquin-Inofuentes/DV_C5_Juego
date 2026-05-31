using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IA_F_EnemyCercanos : MonoBehaviour
{
    public List<GameObject> Colisionados;

    public void OnCollisionEnter2D(Collision2D collision)
    {
        Colisionados.Add(collision.gameObject);
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        Colisionados.Remove(collision.gameObject);
    }
}
