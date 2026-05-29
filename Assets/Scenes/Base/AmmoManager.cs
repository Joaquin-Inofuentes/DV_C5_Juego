using UnityEngine;
using Game.Squad;

public class AmmoManager : MonoBehaviour
{
    public LeaderManager leaderManager;
    public float distanciaDistribucion = 4f; // Distancia para compartir
    public float velocidadTransferencia = 10f; // Balas por segundo

    void Update()
    {
        if (leaderManager == null) return;

        var unidades = leaderManager.unidades;

        // Comparamos todos con todos
        for (int i = 0; i < unidades.Count; i++)
        {
            for (int j = i + 1; j < unidades.Count; j++)
            {
                if (unidades[i] == null || unidades[j] == null) continue;

                float dist = Vector3.Distance(unidades[i].transform.position, unidades[j].transform.position);

                if (dist <= distanciaDistribucion)
                {
                    DistribuirBalas(unidades[i].model, unidades[j].model);
                }
            }
        }
    }

    void DistribuirBalas(SoldierModel a, SoldierModel b)
    {
        if (a == null || b == null || a.IsDead || b.IsDead) return;

        // Si uno tiene más que el otro, igualamos poco a poco
        if (Mathf.Abs(a.balasActuales - b.balasActuales) > 1)
        {
            if (a.balasActuales > b.balasActuales)
            {
                a.balasActuales--;
                b.balasActuales++;
            }
            else
            {
                a.balasActuales++;
                b.balasActuales--;
            }
        }
    }
}
