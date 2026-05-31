using USP.Entities;
using USP.Core;
using USP.Services;
using UnityEngine;
using System.Collections.Generic;
using Game.Squad;

public class EnemyDetector : MonoBehaviour
{
    public float detectionRadius = 6f;
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;

    public List<Transform> enemiesInRange = new List<Transform>();
    public SoldierController controller;

    void Start()
    {
        if (controller == null) controller = GetComponent<SoldierController>();
    }

    void Update()
    {
        LimpiarLista();
        FiltrarYEnviarAlFSM();
    }

    void LimpiarLista()
    {
        enemiesInRange.RemoveAll(e => e == null);
    }

    void FiltrarYEnviarAlFSM()
    {
        if (controller == null) return;

        Transform mejorObjetivo = null;
        float distanciaCercana = Mathf.Infinity;

        foreach (Transform enemigo in enemiesInRange)
        {
            Vector3 direccion = enemigo.position - transform.position;
            float distancia = direccion.magnitude;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direccion.normalized, out hit, detectionRadius, enemyLayer | obstacleLayer))
            {
                if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
                {
                    Debug.DrawLine(transform.position, enemigo.position, Color.green);

                    if (distancia < distanciaCercana)
                    {
                        distanciaCercana = distancia;
                        mejorObjetivo = enemigo;
                    }
                }
                else
                {
                    Debug.DrawLine(transform.position, enemigo.position, Color.red);
                }
            }
        }

        controller.objetivo = mejorObjetivo;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            if (!enemiesInRange.Contains(other.transform))
                enemiesInRange.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        enemiesInRange.Remove(other.transform);
    }
}

