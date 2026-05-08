using UnityEngine;
using System.Collections.Generic;

public class EnemyDetector : MonoBehaviour
{
    public float detectionRadius = 6f;
    public LayerMask enemyLayer;    // Capa de los enemigos
    public LayerMask obstacleLayer; // Capa de paredes/coberturas

    public List<Transform> enemiesInRange = new List<Transform>();
    public FSMController fsm; // Referencia al FSM

    void Start()
    {
        if (fsm == null) fsm = GetComponent<FSMController>();

    }

    void Update()
    {
        LimpiarLista();
        FiltrarYEnviarAlFSM();
    }

    void LimpiarLista()
    {
        // Elimina de la lista si el enemigo fue destruido
        enemiesInRange.RemoveAll(e => e == null);
    }

    void FiltrarYEnviarAlFSM()
    {
        Transform mejorObjetivo = null;
        float distanciaCercana = Mathf.Infinity;

        foreach (Transform enemigo in enemiesInRange)
        {
            // 3D: Usamos Vector3
            Vector3 direccion = enemigo.position - transform.position;
            float distancia = direccion.magnitude;

            // 3D: Physics.Raycast y RaycastHit
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direccion.normalized, out hit, detectionRadius, enemyLayer | obstacleLayer))
            {
                // Si el Raycast golpea algo y ese algo está en la capa de enemigos...
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

        fsm.objetivo = mejorObjetivo;
    }

    // 3D: Usamos OnTriggerEnter y Collider (Asegúrate que la Sphere tenga "Is Trigger" marcado)
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            if (!enemiesInRange.Contains(other.transform))
                enemiesInRange.Add(other.transform);
        }
    }

    // 3D: Usamos OnTriggerExit y Collider
    private void OnTriggerExit(Collider other)
    {
        enemiesInRange.Remove(other.transform);
    }
}