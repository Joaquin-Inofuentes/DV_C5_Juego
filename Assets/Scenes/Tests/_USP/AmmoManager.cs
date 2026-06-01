using UnityEngine;
using Game.Squad;
using System.Linq;

public class AmmoManager : MonoBehaviour
{
    public float distanciaCompartir = 3f;

    void Update()
    {
        var todasLasUnidades = FindObjectsOfType<UnitController>().Where(u => !u.model.IsDead).ToList();

        for (int i = 0; i < todasLasUnidades.Count; i++)
        {
            for (int j = i + 1; j < todasLasUnidades.Count; j++)
            {
                var u1 = todasLasUnidades[i];
                var u2 = todasLasUnidades[j];

                // Solo comparten si son del MISMO equipo
                if (u1.model.team == u2.model.team)
                {
                    float dist = Vector3.Distance(u1.transform.position, u2.transform.position);
                    if (dist < distanciaCompartir)
                    {
                        BalancearMunicion(u1.model, u2.model);
                    }
                }
            }
        }
    }

    void BalancearMunicion(UnitModel a, UnitModel b)
    {
        if (Mathf.Abs(a.ammoActual - b.ammoActual) > 5)
        {
            if (a.ammoActual > b.ammoActual) { a.ammoActual--; b.ammoActual++; }
            else { a.ammoActual++; b.ammoActual--; }
        }
    }
}